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

            ctsPacketsToSend = 0; //close the window so we will wait for the first CTS before starting the transmition
            sendRTS(length, pgn);

            int sentPackets = 0; //next packet to be sent (zero based index)

            while (sentPackets < totalPackets)
            {
                // WAIT FOR CTS
                int ctsTimeout = 2000;
                if (ctsPacketsToSend > 0)
                    ctsTimeout = 1; //the transmit window is still open, just check if there is a CTS

                if (ctsEvent.WaitOne())
                {
                    //CTS received - update the next packet accordig to CTS request
                    //TODO: handel concuret access to ctsNextSeq and ctsPacketsToSend
                    if (ctsNextSeq > 0) //ctsNextSeq is a one based index
                        sentPackets = ctsNextSeq - 1;

                    if (sentPackets >= totalPackets)
                        break;
                }
                else if (ctsTimeout > 1)
                {
                    throw new Exception("CTS timeout");
                }

                for (int i = 0; i < ctsPacketsToSend && sentPackets < totalPackets; i++)
                {
                    byte[] dt = new byte[8];
                    dt[0] = (byte)(sentPackets + 1); //sentPackets transmitted as one based index

                    for (int j = 0; j < 7; j++) //fill data
                    {
                        int index = sentPackets * 7 + j;
                        if (index < length)
                            dt[1 + j] = data[index];
                        else
                            dt[1 + j] = 0xFF;
                    }

                    SendCan(0xEB00, dt);
                    Console.WriteLine($"TX DT seq={dt[0]}");
                    sentPackets++;
                    //ctsPacketsToSend--;
                    System.Threading.Thread.Sleep(2);

                }

                //note: we close the connection before receining an EndOfMsgACK which is not correct and may lead to lost of data
                // in case some last packets are lost and the other side did not had the time to send CTS with a retransmition request
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
                    state.ProtocolBAM = true;
                    state.ExpectedLength = data[1] | (data[2] << 8);
                    int rxPgn = data[5] | (data[6]<<8) | (data[7] << 16);
                    state.Pgn = (uint)rxPgn;
                    state.Source = sa;
                    state.Received = 0;

                    //Console.WriteLine($"BAM: size={state.ExpectedLength}");
                }
                else if (data[0] == 0x10) //RTS
                {
                    state.ProtocolBAM = false;
                    state.ExpectedLength = data[1] | (data[2] << 8);
                    //data[3] - Total number of packets
                    state.MaxPackets = data[4]; //Maximum number of packets that can be sent in response to one CTS
                    int rxPgn = data[5] | (data[6] << 8) | (data[7] << 16); //PGN
                    state.Pgn = (uint)rxPgn;
                    state.Source = sa;
                    state.Received = 0;
                    sendCTS();
                }

                else if (data[0] == 0x11) // or cts = 0x11
                {
                    ctsPacketsToSend = data[1];
                    ctsNextSeq = data[2];
                    Console.WriteLine($"CTS: allow={ctsPacketsToSend} next={ctsNextSeq}");
                    ctsEvent.Set(); // SIGNAL sender
                }
            }
            else if (pgn == 0xEB00) // TP_DT
            {
                if (state.ExpectedLength <= 0)
                    return; //conection not initialized
                if (state.Source != sa)
                    return; //packet received not from the expected source

                int SequenceNumber = data[0];
                int DataOffset = (SequenceNumber-1) * 7;

                if (DataOffset != state.Received)
                {
                    //packet received out of order - drop it
                    //if the protocol is RTS/CTS send CTS with the requiered Sequence Number
                    //if the protocol is BAM the conection should probably be dropd - not implemented yet
                    if (!state.ProtocolBAM)
                        sendCTS();
                    return;
                }

                for (int i = 1; i < 8; i++)
                {
                    if (state.Received < state.ExpectedLength)
                        state.Buffer[state.Received++] = data[i];
                }

                if (state.Received >= state.ExpectedLength && state.ExpectedLength != 0)
                {
                    //reception completed
                    byte[] result = new byte[state.ExpectedLength];
                    Array.Copy(state.Buffer, result, state.ExpectedLength);
                    if (!state.ProtocolBAM)
                        sendEOM_ACK();
                    state.ExpectedLength = 0;

                    RaisePdu(new J1939Message
                    {
                        Pgn = state.Pgn,
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
                    if (state.RemainingPackets > 1)
                        state.RemainingPackets--;
                    else
                        sendCTS();
                }
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
            rts[4] = 16; // max packets per CTS

            rts[5] = (byte)(pgn & 0xFF);
            rts[6] = (byte)((pgn >> 8) & 0xFF);
            rts[7] = (byte)((pgn >> 16) & 0xFF);

            SendCan(0xEC00, rts);
        }

        private void sendCTS()
        {
            byte[] data = new byte[8];

            data[0] = 0x11; // CTS
            data[1] = state.MaxPackets; //Number of packets that can be sent.This value shall be no larger than the value in byte 5 of the RTS message.
            data[2] = (byte)(state.Received / 7 + 1); //Next packet number to be sent
            data[3] = 0xFF;
            data[4] = 0xFF;
            data[5] = (byte)(state.Pgn & 0xFF);
            data[6] = (byte)((state.Pgn >> 8) & 0xFF);
            data[7] = (byte)((state.Pgn >> 16) & 0xFF);

            state.RemainingPackets = state.MaxPackets;
            SendCan(TP_CM, data);
        }


        private void sendEOM_ACK()
        {
            byte[] data = new byte[8];

            data[0] = 0x13; // End_of_Message Acknowledge
            data[1] = (byte)(state.Received & 0xFF); //Total message size, number of bytes
            data[2] = (byte)((state.Received >> 8) & 0xFF);
            data[3] = (byte)((state.Received + 6) / 7); //Total number of packets
            data[4] = 0xFF;
            data[5] = (byte)(state.Pgn & 0xFF);
            data[6] = (byte)((state.Pgn >> 8) & 0xFF);
            data[7] = (byte)((state.Pgn >> 16) & 0xFF);

            SendCan(TP_CM, data);
        }

    }
 
}
