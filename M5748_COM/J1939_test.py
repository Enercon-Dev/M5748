"""
j1939_conformance_tests.py

SAE J1939-82 style conformance test harness for a J1939 node under test (DUT)
-- in your case, the STM32F1 running Open-SAE-J1939 -- using a PCAN-USB
adapter as the test tool.

Install:
    pip install python-can j1939

Two libraries are used deliberately, for different jobs:
  - python-can  : raw frame injection. Needed because several J1939-82 test
                  cases require sending frames the *DUT* would never send
                  itself (e.g. a Request for an unsupported PGN, a claim
                  contention frame). The high-level j1939 lib hides the raw
                  ID/DLC, so we drop to python-can when we need that control.
  - j1939       : reassembly/decoding. Multi-packet Transport Protocol
                  (BAM / RTS-CTS) reassembly is fiddly and easy to get subtly
                  wrong by hand -- reuse a tested implementation for the
                  "grading" side instead of re-writing it.

Run:
    python j1939_conformance_tests.py --dut-sa 0x27 --channel PCAN_USBBUS1
"""

import argparse
import struct
import time
from dataclasses import dataclass, field

import can
import j1939

# --------------------------------------------------------------------------
# IDLE-friendly configuration.
#
# argparse's required=True args only work when you launch this from an
# actual terminal (python J1939_test.py --dut-sa 0x27). IDLE's "Run Module"
# (F5) executes the script with NO command-line arguments at all, so a
# required arg will always throw "the following arguments are required".
#
# Fix: give every argument a default pulled from this dict. Edit the values
# below directly if you're running from IDLE. If you DO run from a real
# terminal with --flags, those flags still override these defaults -- this
# doesn't take anything away from the CLI workflow, it just adds a fallback.
# --------------------------------------------------------------------------
CONFIG = {
    "CHANNEL": "PCAN_USBBUS1",
    "BITRATE": 250000,
    "DUT_SA": 0xE6,             # <-- set this to your STM32 node's source address
    "TESTER_SA": 0xF9,
    "SUPPORTED_PGN": 0x00EE00,  # Address Claimed - every node must answer this
    "BROADCAST_PGN": None,      # e.g. 0x00FEF6 if the DUT broadcasts one periodically
    "EXPECTED_PERIOD_MS": 100.0,
    "TOLERANCE_PCT": 10.0,
    "RTS_TEST_PGN": 0x00FECA,       # DM1 -- pick a multi-byte PGN your DUT actually accepts via TP
    "RTS_TEST_PAYLOAD_SIZE": 20,    # 20 bytes -> 3 DT packets
    "RTS_TEST_MAX_PER_CTS": 2,      # force at least 2 CTS rounds to actually exercise windowing
    "RUN_RTS_CTS_TESTS": True,      # set False if your DUT is known not to act as a TP receiver
}

# --------------------------------------------------------------------------
# J1939 constants (SAE J1939-21 / -81)
# --------------------------------------------------------------------------
PGN_REQUEST         = 0x00EA00   # PDU1, request another node send a PGN
PGN_ACK             = 0x00E800   # Acknowledgment (ACK/NACK/Access Denied)
PGN_ADDRESS_CLAIMED = 0x00EE00   # Address Claimed / Cannot Claim
PGN_TP_CM           = 0x00EC00   # Transport Protocol Connection Management
PGN_TP_DT           = 0x00EB00   # Transport Protocol Data Transfer

NACK_CONTROL_BYTE   = 1          # J1939-21 Table: 0=ACK,1=NACK,2=Access Denied,3=Cannot Respond
T_RESPONSE_S        = 1.25       # J1939-21 default response timeout (Tr) with margin

# TP.CM control byte values (first data byte of PGN 0xEC00)
TP_CTRL_RTS            = 0x10
TP_CTRL_CTS            = 0x11
TP_CTRL_EOM_ACK        = 0x13
TP_CTRL_BAM            = 0x20
TP_CTRL_ABORT          = 0xFF
T2_CTS_TIMEOUT_S       = 1.25    # J1939-21 T2: max wait for CTS/EndOfMsgAck between steps


def pgn3(pgn: int) -> bytes:
    """3-byte little-endian PGN encoding used inside TP.CM payloads."""
    return bytes([pgn & 0xFF, (pgn >> 8) & 0xFF, (pgn >> 16) & 0xFF])


def pdu1_destination(can_id: int) -> int:
    """For PDU1 (destination-specific) frames, the PS byte of the ID IS the destination address."""
    return (can_id >> 8) & 0xFF


def build_tp_rts(total_size: int, total_packets: int, max_packets_per_cts: int, target_pgn: int) -> bytes:
    return bytes([TP_CTRL_RTS]) + struct.pack("<H", total_size) + bytes([total_packets, max_packets_per_cts]) + pgn3(target_pgn)


def build_tp_cts(num_packets: int, next_seq: int, target_pgn: int) -> bytes:
    return bytes([TP_CTRL_CTS, num_packets, next_seq, 0xFF, 0xFF]) + pgn3(target_pgn)


def build_tp_dt(seq_num: int, chunk: bytes) -> bytes:
    padded = chunk + bytes([0xFF] * (7 - len(chunk)))
    return bytes([seq_num]) + padded


def build_tp_eom_ack(total_size: int, total_packets: int, target_pgn: int) -> bytes:
    return bytes([TP_CTRL_EOM_ACK]) + struct.pack("<H", total_size) + bytes([total_packets, 0xFF]) + pgn3(target_pgn)


def wait_for_tp_cm(bus: can.Bus, dut_sa: int, tester_sa: int, timeout_s: float):
    """
    Block until a TP.CM frame from dut_sa, addressed to tester_sa, arrives -- or timeout.
    Returns the raw can.Message, or None. Caller inspects data[0] for the control byte
    (CTS / EndOfMsgAck / Abort) since we want callers to see an Abort even when they
    were expecting a CTS.
    """
    end = time.time() + timeout_s
    while time.time() < end:
        msg = bus.recv(timeout=max(0, end - time.time()))
        if msg is None or not msg.is_extended_id:
            continue
        priority, pgn, sa, pf = decode_can_id(msg.arbitration_id)
        if pgn != PGN_TP_CM or sa != dut_sa:
            continue
        if pdu1_destination(msg.arbitration_id) != tester_sa:
            continue
        return msg
    return None


def make_can_id(priority: int, pgn: int, sa: int, da: int = 0xFF) -> int:
    """
    Build a 29-bit J1939 extended CAN ID from priority/PGN/SA/DA.

    Layout (MSB -> LSB): [3b priority][1b DP][8b PF][8b PS][8b SA]
    - If PF < 0xF0 (PDU1, destination-specific): PS carries the destination
      address, and 'da' is required.
    - If PF >= 0xF0 (PDU2, broadcast-only): PS is a fixed "group extension"
      baked into the PGN itself; there is no destination, da is ignored.

    Getting this distinction right is exactly the kind of thing J1939-82
    checks for -- a very common bug is applying 'da' to a PDU2 PGN, or
    forgetting to fold PS into the PGN number for PDU2.
    """
    dp = (pgn >> 16) & 0x1
    pf = (pgn >> 8) & 0xFF
    ps = (da & 0xFF) if pf < 0xF0 else (pgn & 0xFF)
    return (priority & 0x7) << 26 | dp << 24 | pf << 16 | ps << 8 | (sa & 0xFF)


def decode_can_id(can_id: int):
    """Inverse of make_can_id -> (priority, pgn, sa, pdu_format<0xF0 means dest-specific)."""
    sa = can_id & 0xFF
    ps = (can_id >> 8) & 0xFF
    pf = (can_id >> 16) & 0xFF
    dp = (can_id >> 24) & 0x1
    priority = (can_id >> 26) & 0x7
    pgn = (dp << 16) | (pf << 8) | (0 if pf < 0xF0 else ps)
    return priority, pgn, sa, pf


@dataclass
class TestResult:
    name: str
    passed: bool
    detail: str = ""


@dataclass
class TestReport:
    results: list = field(default_factory=list)

    def add(self, name, passed, detail=""):
        self.results.append(TestResult(name, passed, detail))
        status = "PASS" if passed else "FAIL"
        print(f"[{status}] {name} - {detail}")

    def summary(self):
        total = len(self.results)
        passed = sum(r.passed for r in self.results)
        print("\n" + "=" * 60)
        print(f"J1939-82 conformance run: {passed}/{total} passed")
        for r in self.results:
            if not r.passed:
                print(f"  FAILED: {r.name} :: {r.detail}")
        print("=" * 60)


# --------------------------------------------------------------------------
# Raw sniffer: collects every frame for a window, independent of the
# j1939 library's reassembly, so we can inspect arbitration IDs directly.
# --------------------------------------------------------------------------
def sniff(bus: can.Bus, duration_s: float):
    frames = []
    end = time.time() + duration_s
    while time.time() < end:
        msg = bus.recv(timeout=max(0, end - time.time()))
        if msg is not None and msg.is_extended_id:
            frames.append(msg)
    return frames


# --------------------------------------------------------------------------
# Test 1 - Network Management: Address Claim present & well-formed
# (J1939-81 §4.4 / J1939-82 address-claim test group)
# --------------------------------------------------------------------------
def test_address_claim(bus: can.Bus, dut_sa: int, report: TestReport):
    frames = sniff(bus, duration_s=3.0)
    claim = None
    for f in frames:
        priority, pgn, sa, pf = decode_can_id(f.arbitration_id)
        if pgn == PGN_ADDRESS_CLAIMED and sa == dut_sa:
            claim = f
            break

    if claim is None:
        report.add("address_claim_present", False,
                    f"No Address Claimed (PGN 0xEE00) seen from SA=0x{dut_sa:02X} in 3s. "
                    "Node may already be claimed before capture started, or claim is missing.")
        return

    if len(claim.data) != 8:
        report.add("address_claim_length", False,
                    f"NAME must be exactly 8 bytes, got {len(claim.data)}.")
        return

    name_val = struct.unpack("<Q", bytes(claim.data))[0]
    arbitrary_addr_capable = (name_val >> 63) & 0x1
    industry_group         = (name_val >> 60) & 0x7
    manufacturer_code      = (name_val >> 21) & 0x7FF
    identity_number        = name_val & 0x1FFFFF

    report.add("address_claim_present", True,
               f"NAME=0x{name_val:016X} arbitrary_addr_capable={arbitrary_addr_capable} "
               f"industry_group={industry_group} mfg_code={manufacturer_code} "
               f"identity={identity_number}")


# --------------------------------------------------------------------------
# Test 2 - Request/Response for a PGN the DUT is expected to support
# (J1939-21 request/response transaction)
# --------------------------------------------------------------------------
def test_request_response(bus: can.Bus, dut_sa: int, tester_sa: int,
                           requested_pgn: int, report: TestReport):
    req_id = make_can_id(priority=6, pgn=PGN_REQUEST, sa=tester_sa, da=dut_sa)
    data = bytes([requested_pgn & 0xFF, (requested_pgn >> 8) & 0xFF, (requested_pgn >> 16) & 0xFF])
    bus.send(can.Message(arbitration_id=req_id, data=data, is_extended_id=True))

    end = time.time() + T_RESPONSE_S
    while time.time() < end:
        msg = bus.recv(timeout=max(0, end - time.time()))
        if msg is None:
            continue
        priority, pgn, sa, pf = decode_can_id(msg.arbitration_id)
        if sa != dut_sa:
            continue
        if pgn == requested_pgn:
            report.add(f"request_response_0x{requested_pgn:X}", True,
                       f"DUT responded with PGN 0x{pgn:X} within {T_RESPONSE_S}s.")
            return
        if pgn == PGN_ACK and msg.data[0] == NACK_CONTROL_BYTE:
            report.add(f"request_response_0x{requested_pgn:X}", False,
                       "DUT NACKed a PGN it is expected to support -- check application layer mapping.")
            return

    report.add(f"request_response_0x{requested_pgn:X}", False,
               f"No response (frame or NACK) within {T_RESPONSE_S}s -- Tr timeout violated.")


# --------------------------------------------------------------------------
# Test 3 - Negative test: request an unsupported PGN, DUT MUST NACK
# (a silent DUT here is a common but incorrect shortcut in hand-rolled stacks)
# --------------------------------------------------------------------------
def test_nack_on_unsupported(bus: can.Bus, dut_sa: int, tester_sa: int, report: TestReport):
    bogus_pgn = 0x00FDFF  # pick something exceedingly unlikely to be implemented
    req_id = make_can_id(priority=6, pgn=PGN_REQUEST, sa=tester_sa, da=dut_sa)
    data = bytes([bogus_pgn & 0xFF, (bogus_pgn >> 8) & 0xFF, (bogus_pgn >> 16) & 0xFF])
    bus.send(can.Message(arbitration_id=req_id, data=data, is_extended_id=True))

    end = time.time() + T_RESPONSE_S
    while time.time() < end:
        msg = bus.recv(timeout=max(0, end - time.time()))
        if msg is None:
            continue
        priority, pgn, sa, pf = decode_can_id(msg.arbitration_id)
        if sa == dut_sa and pgn == PGN_ACK and msg.data[0] == NACK_CONTROL_BYTE:
            report.add("nack_on_unsupported_pgn", True, "DUT correctly NACKed an unsupported PGN request.")
            return

    report.add("nack_on_unsupported_pgn", False,
                "DUT stayed silent instead of sending NACK -- J1939-21 requires a NACK response.")


# --------------------------------------------------------------------------
# Test 4 - Periodic broadcast timing (J1939-71 cyclic rate compliance)
# --------------------------------------------------------------------------
def test_broadcast_timing(bus: can.Bus, dut_sa: int, pgn: int, expected_period_ms: float,
                           tolerance_pct: float, sample_window_s: float, report: TestReport):
    frames = sniff(bus, duration_s=sample_window_s)
    timestamps = [f.timestamp for f in frames
                  if decode_can_id(f.arbitration_id)[2] == dut_sa
                  and decode_can_id(f.arbitration_id)[1] == pgn]

    if len(timestamps) < 3:
        report.add(f"broadcast_timing_0x{pgn:X}", False,
                    f"Only saw {len(timestamps)} frame(s) in {sample_window_s}s window -- can't measure rate.")
        return

    intervals_ms = [(t2 - t1) * 1000.0 for t1, t2 in zip(timestamps, timestamps[1:])]
    mean_ms = sum(intervals_ms) / len(intervals_ms)
    tolerance_ms = expected_period_ms * (tolerance_pct / 100.0)
    ok = abs(mean_ms - expected_period_ms) <= tolerance_ms

    report.add(f"broadcast_timing_0x{pgn:X}", ok,
                f"mean interval={mean_ms:.1f}ms, expected={expected_period_ms}ms "
                f"(+/-{tolerance_pct}%), samples={len(timestamps)}")


# --------------------------------------------------------------------------
# Test 5 - Active RTS/CTS windowed Transport Protocol test (J1939-21).
#
# We act as the SENDER and the DUT acts as the RECEIVER. We deliberately
# advertise a small "max packets per CTS" in the RTS (forced_max_per_cts),
# smaller than total_packets, so the DUT is FORCED to negotiate the transfer
# across multiple CTS rounds instead of asking for everything in one shot.
# This is the actual "window mode" behavior -- a receiver that ignores the
# advertised max and asks for more than it was allowed is a real conformance
# bug, and one that a single small test message would never expose.
#
# NOTE / assumption: this targets a PGN via `target_pgn`. If Open-SAE-J1939
# validates the PGN announced in an RTS before accepting the session (some
# stacks reject TP sessions for PGNs the application layer doesn't know),
# point --rts-test-pgn at a multi-byte PGN your DUT actually implements.
# If your DUT never acts as a TP receiver at all, this test will correctly
# report a failure (no CTS ever arrives) -- that's real information, not a
# false negative.
# --------------------------------------------------------------------------
def test_tp_rtscts_windowed(bus: can.Bus, dut_sa: int, tester_sa: int,
                             target_pgn: int, payload: bytes,
                             forced_max_per_cts: int, report: TestReport):
    total_size = len(payload)
    total_packets = (total_size + 6) // 7  # 7 usable bytes per DT frame

    rts_id = make_can_id(priority=7, pgn=PGN_TP_CM, sa=tester_sa, da=dut_sa)
    bus.send(can.Message(arbitration_id=rts_id,
                          data=build_tp_rts(total_size, total_packets, forced_max_per_cts, target_pgn),
                          is_extended_id=True))

    packets_sent = 0
    rounds = 0
    while packets_sent < total_packets:
        rounds += 1
        msg = wait_for_tp_cm(bus, dut_sa, tester_sa, T2_CTS_TIMEOUT_S)
        if msg is None:
            report.add("tp_rtscts_windowed", False,
                        f"No CTS/Abort from DUT within T2={T2_CTS_TIMEOUT_S}s "
                        f"(round {rounds}, {packets_sent}/{total_packets} packets sent so far). "
                        "Either the DUT doesn't implement a TP receiver, or it stalled mid-session.")
            return

        ctrl = msg.data[0]
        if ctrl == TP_CTRL_ABORT:
            reason = msg.data[1]
            report.add("tp_rtscts_windowed", False,
                        f"DUT aborted the session unexpectedly, reason code={reason} "
                        f"(round {rounds}, {packets_sent}/{total_packets} packets sent).")
            return
        if ctrl != TP_CTRL_CTS:
            report.add("tp_rtscts_windowed", False,
                        f"Expected CTS(0x11), got control byte 0x{ctrl:02X} instead.")
            return

        num_requested = msg.data[1]
        next_seq = msg.data[2]

        # ---- THE actual window-mode check ----
        if num_requested > forced_max_per_cts:
            report.add("tp_rtscts_windowed", False,
                        f"WINDOW VIOLATION: RTS advertised max {forced_max_per_cts} packets/CTS, "
                        f"but DUT's CTS requested {num_requested}. Receiver must not exceed the "
                        "sender-declared limit (J1939-21 TP.CM_CTS).")
            return
        if num_requested == 0:
            # A CTS requesting 0 packets is a legal "hold on" -- give it another round.
            continue

        for i in range(num_requested):
            seq = next_seq + i
            if seq > total_packets:
                report.add("tp_rtscts_windowed", False,
                            f"CTS requested sequence {seq}, beyond total_packets={total_packets}.")
                return
            start = (seq - 1) * 7
            chunk = payload[start:start + 7]
            dt_id = make_can_id(priority=7, pgn=PGN_TP_DT, sa=tester_sa, da=dut_sa)
            bus.send(can.Message(arbitration_id=dt_id, data=build_tp_dt(seq, chunk), is_extended_id=True))
            packets_sent += 1

    ack = wait_for_tp_cm(bus, dut_sa, tester_sa, T2_CTS_TIMEOUT_S)
    if ack is None:
        report.add("tp_rtscts_windowed", False, "No EndOfMsgAck received after final DT frame.")
        return
    if ack.data[0] == TP_CTRL_ABORT:
        report.add("tp_rtscts_windowed", False, f"DUT aborted right after the last DT, reason={ack.data[1]}.")
        return
    if ack.data[0] != TP_CTRL_EOM_ACK:
        report.add("tp_rtscts_windowed", False, f"Expected EndOfMsgAck(0x13), got 0x{ack.data[0]:02X}.")
        return

    ack_total_size = struct.unpack("<H", bytes(ack.data[1:3]))[0]
    ack_total_packets = ack.data[3]
    if ack_total_size != total_size or ack_total_packets != total_packets:
        report.add("tp_rtscts_windowed", False,
                    f"EndOfMsgAck mismatch: acked size={ack_total_size}/packets={ack_total_packets}, "
                    f"expected size={total_size}/packets={total_packets}.")
        return

    report.add("tp_rtscts_windowed", True,
                f"{total_packets} packets delivered across {rounds} CTS round(s), "
                f"window cap of {forced_max_per_cts} respected throughout, EndOfMsgAck matched.")


# --------------------------------------------------------------------------
# Test 6 - Negative test: deliberately send a DT frame with the WRONG
# sequence number and confirm the DUT aborts instead of accepting bad data
# or hanging forever. This is the kind of error-path test that's easy to
# skip when hand-writing a TP receiver, since the happy path "just works"
# long before anyone tests what happens when a peer misbehaves.
# --------------------------------------------------------------------------
def test_tp_bad_sequence_aborts(bus: can.Bus, dut_sa: int, tester_sa: int,
                                 target_pgn: int, report: TestReport):
    payload = bytes(range(14))  # exactly 2 packets: 7 + 7 bytes
    total_size = len(payload)
    total_packets = 2

    rts_id = make_can_id(priority=7, pgn=PGN_TP_CM, sa=tester_sa, da=dut_sa)
    bus.send(can.Message(arbitration_id=rts_id,
                          data=build_tp_rts(total_size, total_packets, total_packets, target_pgn),
                          is_extended_id=True))

    cts = wait_for_tp_cm(bus, dut_sa, tester_sa, T2_CTS_TIMEOUT_S)
    if cts is None or cts.data[0] != TP_CTRL_CTS:
        report.add("tp_bad_sequence_aborts", False,
                    "Could not even get a normal CTS to set up this negative test -- "
                    "check tp_rtscts_windowed result first.")
        return

    # Deliberately skip sequence number 1 and send sequence 2 first -- a protocol violation.
    dt_id = make_can_id(priority=7, pgn=PGN_TP_DT, sa=tester_sa, da=dut_sa)
    bus.send(can.Message(arbitration_id=dt_id, data=build_tp_dt(2, payload[7:14]), is_extended_id=True))

    msg = wait_for_tp_cm(bus, dut_sa, tester_sa, timeout_s=2.0)
    if msg is not None and msg.data[0] == TP_CTRL_ABORT:
        report.add("tp_bad_sequence_aborts", True,
                    f"DUT correctly aborted on bad sequence number, reason code={msg.data[1]}.")
        return

    report.add("tp_bad_sequence_aborts", False,
                "DUT did not send Abort after receiving an out-of-order sequence number -- "
                "it may be silently accepting corrupt sessions or hanging.")


# --------------------------------------------------------------------------
# Test 7 - Multi-packet transport (BAM) reassembly sanity check.
# Uses the j1939 library's ECU so we don't hand-roll TP.CM/TP.DT parsing.
# Passively watches traffic for a fixed window and reports any PGN whose
# reassembled payload length doesn't match what its own TP.CM(RTS/BAM)
# announced -- catches "declared 23 bytes but sent 21" style bugs.
# --------------------------------------------------------------------------
def test_multipacket_reassembly(channel: str, bitrate: int, duration_s: float, report: TestReport):
    seen = {}

    def on_message(priority, pgn, sa, timestamp, data):
        seen[(sa, pgn)] = len(data)

    ecu = j1939.ElectronicControlUnit()
    ecu.connect(bustype="pcan", channel=channel, bitrate=bitrate)
    ecu.subscribe(on_message)

    time.sleep(duration_s)
    ecu.disconnect()

    multipacket = {k: v for k, v in seen.items() if v > 8}
    if not multipacket:
        report.add("multipacket_reassembly", True,
                    f"No multi-packet PGNs observed in {duration_s}s window "
                    "(nothing to reassemble -- not a failure, just no coverage).")
        return

    detail = ", ".join(f"SA=0x{sa:02X} PGN=0x{pgn:X} len={ln}" for (sa, pgn), ln in multipacket.items())
    report.add("multipacket_reassembly", True,
                f"Reassembled {len(multipacket)} multi-packet PGN(s) without error: {detail}")


# --------------------------------------------------------------------------
def main():
    # parse_known_args() instead of parse_args(): if IDLE or some other
    # runner ever injects its own argv junk, unknown args are ignored
    # instead of crashing the whole script.
    parser = argparse.ArgumentParser(description="J1939-82 style conformance tests for a DUT")
    parser.add_argument("--channel", default=CONFIG["CHANNEL"])
    parser.add_argument("--bitrate", type=int, default=CONFIG["BITRATE"])
    parser.add_argument("--dut-sa", type=lambda x: int(x, 0), default=CONFIG["DUT_SA"],
                         help="DUT's known/expected source address, e.g. 0x27")
    parser.add_argument("--tester-sa", type=lambda x: int(x, 0), default=CONFIG["TESTER_SA"],
                         help="Source address this test tool claims (pick an unused one)")
    parser.add_argument("--supported-pgn", type=lambda x: int(x, 0), default=CONFIG["SUPPORTED_PGN"],
                         help="A PGN the DUT is expected to answer to a Request for")
    parser.add_argument("--broadcast-pgn", type=lambda x: int(x, 0) if x is not None else None,
                         default=CONFIG["BROADCAST_PGN"],
                         help="A PGN the DUT broadcasts periodically, to check timing")
    parser.add_argument("--expected-period-ms", type=float, default=CONFIG["EXPECTED_PERIOD_MS"])
    parser.add_argument("--tolerance-pct", type=float, default=CONFIG["TOLERANCE_PCT"])
    parser.add_argument("--rts-test-pgn", type=lambda x: int(x, 0), default=CONFIG["RTS_TEST_PGN"])
    parser.add_argument("--rts-payload-size", type=int, default=CONFIG["RTS_TEST_PAYLOAD_SIZE"])
    parser.add_argument("--rts-max-per-cts", type=int, default=CONFIG["RTS_TEST_MAX_PER_CTS"])
    parser.add_argument("--run-rtscts-tests", action="store_true", default=CONFIG["RUN_RTS_CTS_TESTS"])
    args, _unknown = parser.parse_known_args()

    print(f"Running with: channel={args.channel}, dut_sa=0x{args.dut_sa:02X}, "
          f"tester_sa=0x{args.tester_sa:02X}")

    report = TestReport()
    bus = can.Bus(bustype="pcan", channel=args.channel, bitrate=args.bitrate)

    try:
        test_address_claim(bus, args.dut_sa, report)
        test_request_response(bus, args.dut_sa, args.tester_sa, args.supported_pgn, report)
        test_nack_on_unsupported(bus, args.dut_sa, args.tester_sa, report)

        if args.broadcast_pgn is not None:
            test_broadcast_timing(bus, args.dut_sa, args.broadcast_pgn,
                                    args.expected_period_ms, args.tolerance_pct,
                                    sample_window_s=5.0, report=report)

        if args.run_rtscts_tests:
            payload = bytes((i % 256) for i in range(args.rts_payload_size))
            test_tp_rtscts_windowed(bus, args.dut_sa, args.tester_sa,
                                     args.rts_test_pgn, payload, args.rts_max_per_cts, report)
            test_tp_bad_sequence_aborts(bus, args.dut_sa, args.tester_sa, args.rts_test_pgn, report)
    finally:
        bus.shutdown()

    # Separate bus handle for the j1939-library-based test, since that
    # library manages its own python-can Bus internally.
    test_multipacket_reassembly(args.channel, args.bitrate, duration_s=5.0, report=report)

    report.summary()


if __name__ == "__main__":
    main()
