using Peak.Can.Basic;
using System;
using System.Collections.Generic;
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
        public byte[] Data;
    }
    public class TpRxState
    {
        public int ExpectedLength;
        public int Received;
        public byte[] Buffer = new byte[2048];
    }
   public class CanReceiver
    {
        public event Action<uint, byte[]> OnMessage;
        int count = 0;
        public void StartCanReceive()
        {
            new Thread(() =>
            {
                while (true)
                {
                    TPCANMsg msg;
                    TPCANTimestamp ts;

                    var sts = PCANBasic.Read(81, out msg, out ts);

                    if (sts == TPCANStatus.PCAN_ERROR_OK)
                    {
                        count++;
                        OnMessage?.Invoke(msg.ID, msg.DATA);
                    }
                    else if (sts != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                    {
                        Console.WriteLine("CAN Read Error: " + sts);
                        PCANBasic.Reset(81);

                    }
                    else if (sts == TPCANStatus.PCAN_ERROR_BUSHEAVY)
                    {
                        PCANBasic.Reset(81);
                    }
                    //Thread.Sleep(50);

                }
            }).Start();
        }
    }


    public class J1939
    {
        const uint TP_CM = 0x1CECFF00; // Connection Management
        const uint TP_DT = 0x1CEBFF00; // Data Transfer

        private Queue<J1939Message> completedMessages = new Queue<J1939Message>();
        private AutoResetEvent messageEvent = new AutoResetEvent(false);
         public CanReceiver CanReceive = new CanReceiver();
        public void InitCanBus()
        {
            TPCANStatus stsResult;
            stsResult = PCANBasic.Initialize(
                           81,
                           TPCANBaudrate.PCAN_BAUD_250K,
                           TPCANType.PCAN_TYPE_ISA,
                           0x0100,
                           0x3);
            if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                Console.WriteLine("CANBUS connected!");
            else
            {
                Console.WriteLine("CANBUS Connection Error");
                return;
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

        public void SendLongMessage(uint pgn, byte[] data)
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

        public void SendCan(uint id, byte[] data) // raw, no J1939
        {
            TPCANMsg msg = new TPCANMsg();

            msg.ID = id;
            msg.LEN = 8;
            msg.MSGTYPE = TPCANMessageType.PCAN_MESSAGE_EXTENDED;

            msg.DATA = new byte[8];
            Array.Copy(data, msg.DATA, 8);

            var sts = PCANBasic.Write(81, ref msg);

            if (sts != TPCANStatus.PCAN_ERROR_OK)
                throw new Exception("CAN send failed");

           // Console.WriteLine($"TX {id:X} : {BitConverter.ToString(msg.DATA)}");
        }
       

    public byte[] SendAndWaitJ1939(
    uint pgn,
    byte[] payload,
    byte expectedOpcode,
    int timeoutMs = 1000,
    int retries = 10)
        {

            // ----------------------------
            // SEND
            // ----------------------------
            if (payload.Length <= 8)
            {
                uint canId = BuildCanId(pgn);
                SendCan(canId, payload);

            }
            else
            {
                SendLongMessage(pgn, payload);
            }
            for (int attempt = 0; attempt < retries; attempt++)
            {
              //  Console.WriteLine($"[J1939] Attempt {attempt + 1}");
                // while (completedMessages.Count == 0) ;
                //Thread.CurrentThread.Join();
                // ----------------------------
                // WAIT + FILTER
                // ----------------------------
                var start = DateTime.Now;

                var sw = System.Diagnostics.Stopwatch.StartNew();

                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    //  WAIT for signal instead of polling
                    int waitTime = timeoutMs - (int)sw.ElapsedMilliseconds;
                    if (waitTime <= 0)
                        break;

                    messageEvent.WaitOne(); // wait for receive

                    //lock (completedMessages)
                    //{
                        while (completedMessages.Count > 0)
                        {
                            var msg = completedMessages.Dequeue();

                            // ----------------------------
                            // FILTER  OPCODE (Application layer)
                            // ----------------------------
                            if (msg.Data != null && msg.Data.Length > 0)
                            {
                                if (msg.Data[1] == expectedOpcode)
                                {
                                  //  Console.WriteLine("[J1939] RX MATCH: " +
                                  //      BitConverter.ToString(msg.Data));

                                    return msg.Data;
                                }
                            }
                        }
                    //}

                }
                //Thread.Sleep(1);

                Console.WriteLine("[J1939] Timeout, retry...");
            }
            return null;
           // throw new Exception("J1939: No valid response");
        }

        public uint BuildCanId(uint pgn, byte da = 0x23, byte sa = 0x00)
        {
            // 0x14 = for Proprietary_A messages
            return (0x14 << 24) | (pgn << 8) | sa;
        }
        //public byte[] ReceiveLongMessage(int timeoutMs = 1000)
        //{
        //    var start = DateTime.Now;

        //    while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
        //    {
        //        lock (completedMessages)
        //        {
        //            if (completedMessages.Count > 0)
        //                return completedMessages.Dequeue().Data;
        //        }

        //        //Thread.Sleep(1);
        //    }

        //    return null;
        //}


        //public byte[] ReceiveByOpcode(byte opcode, int timeoutMs)
        //{
        //    var start = DateTime.Now;

        //    while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
        //    {
        //        lock (completedMessages)
        //        {
        //            while (completedMessages.Count > 0)
        //            {
        //                var msg = completedMessages.Dequeue();

        //                if (msg.Data[0] == opcode)
        //                    return msg.Data;
        //            }
        //        }

        //        Thread.Sleep(1);
        //    }

        //    return null;
        //}
        //public byte[] ReceiveFilteredMessage(uint expectedPgn, int timeoutMs)
        //{
        //    var start = DateTime.Now;

        //    while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
        //    {
        //        lock (completedMessages)
        //        {
        //            while (completedMessages.Count > 0)
        //            {
        //                var msg = completedMessages.Dequeue();

        //                if (msg.Pgn == expectedPgn)
        //                {
        //                    return msg.Data;
        //                }
        //            }
        //        }

        //        Thread.Sleep(1);
        //    }

        //    return null;
        //}
     
        TpRxState state = new TpRxState();

        public void ProcessFrame(uint id, byte[] data)
        {
            uint pgn = (id >> 8) & 0xFFFF;
            byte sa = (byte)(id & 0xFF);

            if (pgn == 0xEC00) // TP_CM
            {
                if (data[0] == 0x20) // check BAM
                {
                    state.ExpectedLength = data[1] | (data[2] << 8);
                    state.Received = 0;

                    //Console.WriteLine($"BAM: size={state.ExpectedLength}");
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
                    OnFullMessageReceived(new J1939Message
                    {
                        Pgn = pgn,
                        Source = sa,
                        Data = result
                    });// Console.WriteLine("FULL MESSAGE RECEIVED");
                    return;
                }
            }
            else
            {
                //ProcessSingleFrame(pgn, data);
            }
           // System.Threading.Thread.Sleep(10);

        }
       

    }
 
}
