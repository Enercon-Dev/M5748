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
        public J1939(byte sa, byte da)
        {
            SourceAddress = sa;
            DestinationAddress = da;
        }

        private Queue<J1939Message> completedMessages = new Queue<J1939Message>();
        private AutoResetEvent messageEvent = new AutoResetEvent(false);
        private AutoResetEvent ctsEvent = new AutoResetEvent(false);
        private TpRxState state = new TpRxState();

   
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
               // SendTpRtsCts(pgn, data);
               SendBamMessage(pgn, data);
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

            byte[] rts = new byte[8];

            rts[0] = 0x10; // RTS
            rts[1] = (byte)(length & 0xFF);
            rts[2] = (byte)((length >> 8) & 0xFF);
            rts[3] = (byte)totalPackets;
            rts[4] = 0xFF; // max packets per CTS

            rts[5] = (byte)(pgn & 0xFF);
            rts[6] = (byte)((pgn >> 8) & 0xFF);
            rts[7] = (byte)((pgn >> 16) & 0xFF);

            SendCan(0xEC00, rts);

            int sentPackets = 0;
            int index = 0;

            Stopwatch sw = Stopwatch.StartNew();

            while (sentPackets < totalPackets)
            {
                // WAIT FOR CTS
                if (!ctsEvent.WaitOne(2000))
                    throw new Exception("CTS timeout");

                for (int i = 0; i < ctsPacketsToSend; i++)
                {
                    if (sentPackets >= totalPackets)
                        break;

                    byte[] dt = new byte[8];
                    dt[0] = (byte)(sentPackets + 1);

                    for (int j = 1; j < 8; j++)
                    {
                        if (index < length)
                            dt[j] = data[index++];
                        else
                            dt[j] = 0xFF;
                    }

                    SendCan(0xEB00, dt);

                    sentPackets++;
                }
            }
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

       

        public byte[] SendAndWaitJ1939(
            uint pgn,
            byte[] payload,
            byte expectedOpcode,
            int timeoutMs = 2000,
            int retries = 3)
        {
            for (int i = 0; i < retries; i++)
            {
                Send(pgn, payload);

                var sw = System.Diagnostics.Stopwatch.StartNew();

                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    messageEvent.WaitOne();

                    lock (completedMessages)
                    {
                        while (completedMessages.Count > 0)
                        {
                            var msg = completedMessages.Dequeue();

                            if (msg.Data != null &&
                                msg.Data.Length > 0 &&
                                msg.Data[6] == expectedOpcode) 
                            {
                                return msg.Data;
                            }
                        }
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

            if (pf < 240)
                pgn = (uint)(pf << 8);   // remove DA
            else
                pgn = (uint)((pf << 8) | ps);

            if (pgn == 0xEC00) // TP_CM
            {
                if (data[0] == 0x20)  // check BAM = 0x20 
                {
                    state.ExpectedLength = data[1] | (data[2] << 8);
                    state.Received = 0;

                    //Console.WriteLine($"BAM: size={state.ExpectedLength}");
                }
                else if (data[0] == 0x11) // or cts = 0x11
                {
                    ctsPacketsToSend = data[1];
                    ctsNextSeq = data[2];

                    ctsEvent.Set(); // SIGNAL sender
                }
            }
            else if (pgn == 0xEB00) // TP_DT
            {
                for (int i = 1; i < 8; i++)
                {
                    if (state.Received < state.ExpectedLength)
                        state.Buffer[state.Received++] = data[i];
                }

                if (state.Received >= state.ExpectedLength && state.ExpectedLength != 0)
                {
                    byte[] result = new byte[state.ExpectedLength];
                    Array.Copy(state.Buffer, result, state.ExpectedLength);

                    RaisePdu(new J1939Message
                    {
                        Pgn = pgn,
                        Source = sa,
                        Destination = ps,
                        Data = result
                    });
                    return;
                }
            }

            
            
        }


    }
 
}
