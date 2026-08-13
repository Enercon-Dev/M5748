using Peak.Can.Basic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace M5748SwUpdater
{
    public class J1939Message
    {
        public uint Pgn;
        public byte Source;
        public byte Destination;
        public byte[] Data;
    }
    public class TpRxState
    {
        public bool ProtocolBAM;
        public byte MaxPackets;
        public byte RemainingPackets;
        public uint Pgn;
        public byte Source;
        public int ExpectedLength;
        public int Received;
        public byte[] Buffer = new byte[2048];
    }


    public class J1939
    {
        const uint TP_CM = 0xEC00; // Connection Management
        const uint TP_DT = 0xEB00; // Data Transfer

        private byte SourceAddress;
        private byte DestinationAddress;
        private ushort Channel = 81;
        private byte ctsPacketsToSend;
        private byte ctsNextSeq;
        private bool ctsLive = false;


        private byte[] currentTransmitData;
        private int currentTransmitLength;
        private int currentTransmitTotalPackets;
        private int txNextSequence = 1;
        private readonly object txLock = new object();

        private bool tpTxActive = false;
        private bool lastCtsWasHold = false;
        const uint TP_T1_MS = 750;
        const int TP_T2_MS = 1250;   // sender waits for next CTS
        const int TP_T3_MS = 1250;   // sender waits for EndOfMsgAck after final DT
        const int TP_T4_MS = 1050;   // sender waits after CTS(hold, packets=0)
        private System.Timers.Timer rxT1Timer;
        bool debug = true;
        private void StartT1()
        {
            if (debug)
                return;
            if (rxT1Timer == null)
            {
                rxT1Timer = new System.Timers.Timer { AutoReset = false };
                rxT1Timer.Elapsed += (s, e) => OnT1Timeout();
            }
            rxT1Timer.Stop();
            rxT1Timer.Interval = TP_T1_MS;
            rxT1Timer.Start();
        }

        private void OnT1Timeout()
        {
            Console.WriteLine("T1 timeout waiting for TP.DT -- sending Abort");
            SendAbort(RxState.Source, RxState.Pgn, 3 /* timeout */);
            RxState.ExpectedLength = 0;
            RxState.Received = 0;
        }

        private void SendAbort(byte da, uint pgn, byte reason)
        {
            byte savedDa = DestinationAddress;
            DestinationAddress = da;
            byte[] d = new byte[8] {
        0xFF, reason, 0xFF, 0xFF, 0xFF,
        (byte)pgn, (byte)(pgn >> 8), (byte)(pgn >> 16)
    };
            SendCan(TP_CM, d);
            DestinationAddress = savedDa;
        }


        public J1939(byte sa, byte da)
        {
            SourceAddress = sa;
            DestinationAddress = da;
        }

        private Queue<J1939Message> completedMessages = new Queue<J1939Message>();
        private AutoResetEvent messageEvent = new AutoResetEvent(false);
        private AutoResetEvent ctsEvent = new AutoResetEvent(false);
        private AutoResetEvent eomEvent = new AutoResetEvent(false);
        private TpRxState RxState = new TpRxState();


        public delegate void NewPduRecivedHandler(object sender, J1939Message msg);
        public event NewPduRecivedHandler NewPduRecived;

        private void RaisePdu(J1939Message msg)
        {
            if (NewPduRecived != null)
                NewPduRecived(this, msg);
            else
            {
                lock (completedMessages)
                    completedMessages.Enqueue(msg);

                messageEvent.Set();
            }
        }
        private Thread rxThread;
        private bool running;

        public void CanOpen()
        {
            var sts = PCANBasic.Initialize(Channel, TPCANBaudrate.PCAN_BAUD_250K);

            if (sts != TPCANStatus.PCAN_ERROR_OK)
                throw new Exception("CAN init failed");

            running = true;
            rxThread = new Thread(RxLoop);
            rxThread.Start();
        }

        public void CanClose()
        {
            running = false;
            rxThread?.Join();
            PCANBasic.Uninitialize(Channel);
        }

        private void RxLoop()
        {
            while (running)
            {
                TPCANMsg msg;
                TPCANTimestamp ts;

                var sts = PCANBasic.Read(Channel, out msg, out ts);

                if (sts == TPCANStatus.PCAN_ERROR_OK)
                {
                    ProcessFrame(msg.ID, msg.DATA);
                }
                else if (sts == TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    PCANBasic.Reset(Channel);
                }
            }
        }
        public void OnFullMessageReceived(J1939Message msg)
        {
            lock (completedMessages)
            {
                completedMessages.Enqueue(msg);
            }
            messageEvent.Set(); //  SIGNAL

        }

        public void Send(uint pgn, byte[] data)
        {
            if (data.Length <= 8)
                SendCan(pgn, data);
            else
                SendTpRtsCts(pgn, data);
            //SendBamMessage(pgn, data);
        }

        private void SendCan(uint pgn, byte[] data)
        {
            uint id = BuildCanId(pgn);

            TPCANMsg msg = new TPCANMsg
            {
                ID = id,
                LEN = 8,
                MSGTYPE = TPCANMessageType.PCAN_MESSAGE_EXTENDED,
                DATA = new byte[8]
            };

            Array.Copy(data, msg.DATA, data.Length);

            // no padding needed (as requested)

            var sts = PCANBasic.Write(Channel, ref msg);

            if (sts != TPCANStatus.PCAN_ERROR_OK)
                throw new Exception("CAN send failed");
        }

        // =============================
        // RTS/CTS 
        // =============================
        private void SendTpRtsCts(uint pgn, byte[] data)
        {
            int length = data.Length;
            int totalPackets = (length + 6) / 7;

            lock (txLock)
            {
                currentTransmitData = data.ToArray();
                currentTransmitLength = length;
                currentTransmitTotalPackets = totalPackets;
                tpTxActive = true;
            }

            try
            {
                //ctsPacketsToSend = 0;

                sendRTS(length, pgn);

                int sentPackets = 0;

                while (sentPackets < totalPackets)
                {
                    /*
                     * Wait for CTS.
                     *
                     * The first CTS opens the first transmission window.
                     * A subsequent CTS can either:
                     *
                     * 1. Continue with a new window
                     * 2. Request retransmission of an earlier packet
                     */

                    // Determine which timeout is active right now
                    int timeoutMs;
                    if (sentPackets >= totalPackets)
                        timeoutMs = TP_T3_MS;                    // waiting for EndOfMsgAck
                    else if (lastCtsWasHold)
                        timeoutMs = TP_T4_MS;                    // waiting after CTS(0,...)
                    else
                        timeoutMs = TP_T2_MS;                    // waiting for next CTS

                    if (!ctsEvent.WaitOne(timeoutMs))
                    {
                        string phase = (sentPackets >= totalPackets) ? "T3 (EOM_ACK)" :
                                        lastCtsWasHold ? "T4 (hold)" : "T2 (CTS)";
                        Console.WriteLine($"{phase} timeout -- sending Abort");
                        SendAbort(DestinationAddress, pgn, 3 /* timeout */);
                        lock (txLock) { tpTxActive = false; }
                        throw new Exception($"{phase} timeout");
                    }

                    byte requestedSeq;
                    byte packetsToSend;

                    lock (txLock)
                    {
                        requestedSeq = ctsNextSeq;
                        packetsToSend = ctsPacketsToSend;
                        if (packetsToSend == 0)
                            continue;   // CTS(0,...) means "hold" -- loop back to wait for next CTS under T4
                    }

                    if (requestedSeq == 0 || requestedSeq > totalPackets)
                        throw new Exception($"Invalid CTS sequence {requestedSeq}");

                    /*
                     * CTS may request a sequence number earlier than the
                     * packet we currently consider sent.
                     *
                     * Example:
                     *
                     * sentPackets = 4
                     * CTS nextSeq = 3
                     *
                     * => retransmit packet 3
                     */

                    if (requestedSeq - 1 < sentPackets)
                    {
                        Console.WriteLine(
                            $"CTS retransmission request: seq={requestedSeq}");
                    }

                    /*
                     * Normal forward transmission.
                     */

                    sentPackets = requestedSeq - 1;

                    for (int i = 0;
                         i < packetsToSend && sentPackets < totalPackets;
                         i++)
                    {
                        byte seq = (byte)(sentPackets + 1);

                        SendTpDataPacket(
                            seq,
                            currentTransmitData,
                            currentTransmitLength);
                        txNextSequence = seq + 1;
                        sentPackets++;

                        Thread.Sleep(2);
                    }
                }

                /*
                 * IMPORTANT:
                 *
                 * Do NOT finish the transmission here.
                 *
                 * We still need to receive the EndOfMsgACK.
                 */

                Console.WriteLine("All TP.DT packets transmitted. Waiting for EOM ACK.");

                if (!eomEvent.WaitOne(TP_T3_MS))
                {
                    SendAbort(DestinationAddress, pgn, 3);
                    lock (txLock) { tpTxActive = false; }
                    throw new Exception("T3 (EOM_ACK) timeout");
                }
                // Wait for EOM ACK here if you have a TX completion event.
            }
            finally
            {
                lock (txLock)
                {
                    /*
                     * Don't clear this immediately if you still need
                     * retransmission requests after the final packet.
                     *
                     * Prefer clearing it when EOM ACK is received.
                     */
                }
            }
        }
        public byte DebugSkipSeq = 0;        // 0 = disabled, else the seq to skip once
        private bool debugSkipConsumed = false;
        private void SendTpDataPacket(byte sequence, byte[] data, int length)
        {

            // Debug: simulate a lost packet on the first transmission of this seq
            if (DebugSkipSeq != 0 && sequence == DebugSkipSeq && !debugSkipConsumed)
            {
                debugSkipConsumed = true;
                Console.WriteLine($"[DEBUG] Skipping DT seq={sequence} to simulate packet loss");
                return;   // deliberately do not transmit
            }
            byte[] dt = new byte[8];

            dt[0] = sequence;

            int offset = (sequence - 1) * 7;

            for (int i = 0; i < 7; i++)
            {
                int index = offset + i;

                if (index < length)
                    dt[i + 1] = data[index];
                else
                    dt[i + 1] = 0xFF;
            }

            SendCan(TP_DT, dt);
            Console.WriteLine(
                $"TX DT seq={sequence}");
        }

        public void SendBamMessage(uint pgn, byte[] data)
        {
            int length = data.Length;
            int numPackets = (length + 6) / 7;

            // -----------------------
            // 1. BAM (announce)
            // -----------------------
            byte[] bam = new byte[8];

            bam[0] = 0x20; // BAM
            bam[1] = (byte)(length & 0xFF);
            bam[2] = (byte)((length >> 8) & 0xFF);
            bam[3] = (byte)numPackets;
            bam[4] = 0xFF;

            bam[5] = (byte)(pgn & 0xFF);
            bam[6] = (byte)((pgn >> 8) & 0xFF);
            bam[7] = (byte)((pgn >> 16) & 0xFF);

            SendCan(TP_CM, bam);

            System.Threading.Thread.Sleep(2);

            // -----------------------
            // 2. DATA packets
            // -----------------------
            int index = 0;

            for (int seq = 1; seq <= numPackets; seq++)
            {
                byte[] dt = new byte[8];
                dt[0] = (byte)seq;

                for (int i = 1; i < 8; i++)
                {
                    if (index < length)
                        dt[i] = data[index++];
                    else
                        dt[i] = 0xFF;
                }

                SendCan(TP_DT, dt);

                System.Threading.Thread.Sleep(2);
            }
        }

        public void clearReceived()
        {
            lock (completedMessages)
            {
                completedMessages.Clear();
            }
        }

        public J1939Message getMessage(int timeOut)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeOut)
            {
                messageEvent.WaitOne(timeOut);

                lock (completedMessages)
                {
                    while (completedMessages.Count > 0)
                    {
                        var msg = completedMessages.Dequeue();
                        return msg;
                    }
                }
            }
            return null;
        }


        private uint BuildCanId(uint pgn)
        {
            byte pf = (byte)(pgn >> 8);
            byte ps = (byte)(pgn & 0xFF);

            byte priority;

            // ----------------------------
            // SELECT PRIORITY (id0)
            // ----------------------------
            if (pgn == 0xEC00 || pgn == 0xEB00)
            {
                // TP.CM / TP.DT
                priority = 7; // -> id0 = 0x1C
            }
            else if (pf == 0xEF)
            {
                // Proprietary A
                priority = 5; // -> id0 = 0x14
            }
            else
            {
                // Normal messages
                priority = 6; // -> id0 = 0x18
            }

            uint id = 0;

            if (pf < 240)
            {
                // ----------------------------
                // PDU1 (destination specific)
                // ----------------------------
                id = (uint)(
                    (priority << 26) |
                    (pf << 16) |
                    (DestinationAddress << 8) |
                    SourceAddress
                );
            }
            else
            {
                // ----------------------------
                // PDU2 (broadcast)
                // ----------------------------
                id = (uint)(
                    (priority << 26) |
                    (pgn << 8) |
                    SourceAddress
                );
            }

            return id;
        }


        private void ProcessFrame(uint id, byte[] data)
        {
            byte pf = (byte)(id >> 16);
            byte ps = (byte)(id >> 8);
            byte sa = (byte)(id);

            uint pgn;
            byte da = 0xFF;

            if (pf < 240)
            {
                pgn = (uint)(pf << 8);   // remove DA
                da = ps;
            }
            else
            {
                pgn = (uint)((pf << 8) | ps);
            }

            //if (da != 0xFF && da != SourceAddress)
            //    return; //this message is not for us

            if (pgn == TP_CM) // TP_CM (0xEC00)
            {

                if (data[0] == 0x20)  // check BAM = 0x20 
                {
                    RxState.ProtocolBAM = true;
                    RxState.ExpectedLength = data[1] | (data[2] << 8);
                    int rxPgn = data[5] | (data[6] << 8) | (data[7] << 16);
                    RxState.Pgn = (uint)rxPgn;
                    RxState.Source = sa;
                    RxState.Received = 0;

                    //Console.WriteLine($"BAM: size={state.ExpectedLength}");
                }
      

                else if (data[0] == 0x10) //RTS
                {
                    RxState.ProtocolBAM = false;
                    RxState.ExpectedLength = data[1] | (data[2] << 8);
                    //data[3] - Total number of packets
                    RxState.MaxPackets = data[4]; //Maximum number of packets that can be sent in response to one CTS
                    int rxPgn = data[5] | (data[6] << 8) | (data[7] << 16); //PGN
                    RxState.Pgn = (uint)rxPgn;
                    RxState.Source = sa;
                    RxState.Received = 0;
                    sendCTS();
                    //StartT1();
                }

                else if (data[0] == 0x11) // CTS
                {
                    byte requestedPackets = data[1];
                    byte requestedSequence = data[2];

                    if (requestedPackets == 0)
                    {
                        lastCtsWasHold = true;   // add this field to the class
                        ctsEvent.Set();          // still wake up the sender so it can re-wait with T4
                        return;
                    }
                    lastCtsWasHold = false;

                    Console.WriteLine($"CTS: allow={requestedPackets} next={requestedSequence}");


                    bool retransmission = false;

                    lock (txLock)
                    {
                        /*
                         * If the requested sequence is behind the sequence
                         * we have already transmitted, this is a retransmission.
                         */
                        if (tpTxActive &&
                            requestedSequence > 0 &&
                            requestedSequence < txNextSequence)
                        {
                            retransmission = true;
                        }

                        ctsPacketsToSend = requestedPackets;
                        ctsNextSeq = requestedSequence;
                    }

                    if (retransmission)
                    {
                        Console.WriteLine(
                            $"CTS indicates missing packet -> retransmit seq={requestedSequence}");
       
                        retransmission = false;
                        ctsEvent.Set();

                        return;
                    }
                    else
                    {
                        /*
                         * Normal CTS.
                         */
                        ctsEvent.Set();
                    }
                }
                else if (data[0] == 0x13) // EndOfMessage
                    eomEvent.Set();
            }
            else if (pgn == 0xEB00) // TP_DT
            {
                if (RxState.ExpectedLength <= 0)
                    return; //conection not initialized
                if (RxState.Source != sa)
                    return; //packet received not from the expected source

                int SequenceNumber = data[0];
                int DataOffset = (SequenceNumber - 1) * 7;

                if (DataOffset != RxState.Received)
                {
                    //packet received out of order - drop it
                    //if the protocol is RTS/CTS send CTS with the requiered Sequence Number
                    //if the protocol is BAM the conection should probably be dropd - not implemented yet
                    if (!RxState.ProtocolBAM)
                    {
                        sendCTS();
                        //StartT1();
                    }
                    return;
                }

                for (int i = 1; i < 8; i++)
                {
                    if (RxState.Received < RxState.ExpectedLength)
                        RxState.Buffer[RxState.Received++] = data[i];
                }

                if (RxState.Received >= RxState.ExpectedLength && RxState.ExpectedLength != 0)
                {
                    //reception completed
                    byte[] result = new byte[RxState.ExpectedLength];
                    Array.Copy(RxState.Buffer, result, RxState.ExpectedLength);
                    if (!RxState.ProtocolBAM)
                        sendEOM_ACK();
                    RxState.ExpectedLength = 0;

                    rxT1Timer?.Stop(); // STOP T1 timer

                    lock (txLock)
                    {
                        tpTxActive = false;
                        currentTransmitData = null;
                        currentTransmitLength = 0;
                        currentTransmitTotalPackets = 0;
                        txNextSequence = 1;
                    }

                    RaisePdu(new J1939Message
                    {
                        Pgn = RxState.Pgn,
                        Source = sa,
                        Destination = ps,
                        Data = result
                    });
                    return;
                }
                else
                {
                    //reception not completed
                    //a CTS message is sent when the last data packet requested in the previous CTS message has been received.
                    if (RxState.RemainingPackets > 1)
                        RxState.RemainingPackets--;
                    else
                    {
                        sendCTS();
                        // StartT1();
                    }
                }
            }
         

            else if (pgn == 0xFF)
            {
                tpTxActive = false;
                RxState = new TpRxState();
            }
            
            else
            {
                RaisePdu(new J1939Message
                {
                    Pgn = pgn,
                    Source = sa,
                    Destination = ps,
                    Data = data
                });
            }
        }

        private void sendRTS(int length, uint pgn)
        {
            int totalPackets = (length + 6) / 7;

            byte[] rts = new byte[8];

            rts[0] = 0x10; // RTS
            rts[1] = (byte)(length & 0xFF);
            rts[2] = (byte)((length >> 8) & 0xFF);
            rts[3] = (byte)totalPackets;
            rts[4] = 4; // max packets per CTS

            rts[5] = (byte)(pgn & 0xFF);
            rts[6] = (byte)((pgn >> 8) & 0xFF);
            rts[7] = (byte)((pgn >> 16) & 0xFF);

            SendCan(0xEC00, rts);
        }

        //private void sendCTS()
        //{
        //    byte[] data = new byte[8];

        //    data[0] = 0x11; // CTS
        //    data[1] = state.MaxPackets; //Number of packets that can be sent.This value shall be no larger than the value in byte 5 of the RTS message.
        //    data[2] = (byte)(state.Received / 7 + 1); //Next packet number to be sent
        //    data[3] = 0xFF;
        //    data[4] = 0xFF;
        //    data[5] = (byte)(state.Pgn & 0xFF);
        //    data[6] = (byte)((state.Pgn >> 8) & 0xFF);
        //    data[7] = (byte)((state.Pgn >> 16) & 0xFF);

        //    state.RemainingPackets = state.MaxPackets;
        //    SendCan(TP_CM, data);
        //}
        private void sendCTS()
        {
            byte[] data = new byte[8];

            byte nextSequence =
                (byte)(RxState.Received / 7 + 1);

            data[0] = 0x11;

            /*
             * Normal window.
             */
            data[1] = RxState.MaxPackets;

            /*
             * First packet expected.
             */
            data[2] = nextSequence;

            data[3] = 0xFF;
            data[4] = 0xFF;

            data[5] = (byte)(RxState.Pgn & 0xFF);
            data[6] = (byte)((RxState.Pgn >> 8) & 0xFF);
            data[7] = (byte)((RxState.Pgn >> 16) & 0xFF);

            RxState.RemainingPackets = RxState.MaxPackets;

            Console.WriteLine(
                $"TX CTS: allow={data[1]} next={data[2]}");

            SendCan(TP_CM, data);
            StartT1();
        }

        private void sendEOM_ACK()
        {
            byte[] data = new byte[8];

            data[0] = 0x13; // End_of_Message Acknowledge
            data[1] = (byte)(RxState.Received & 0xFF); //Total message size, number of bytes
            data[2] = (byte)((RxState.Received >> 8) & 0xFF);
            data[3] = (byte)((RxState.Received + 6) / 7); //Total number of packets
            data[4] = 0xFF;
            data[5] = (byte)(RxState.Pgn & 0xFF);
            data[6] = (byte)((RxState.Pgn >> 8) & 0xFF);
            data[7] = (byte)((RxState.Pgn >> 16) & 0xFF);

            SendCan(TP_CM, data);
        }

        private void sendRemainingData(byte NextSeq)
        {
            byte[] dt = new byte[8];
            SendCan(0xEB00, dt);
        }

    }

}
