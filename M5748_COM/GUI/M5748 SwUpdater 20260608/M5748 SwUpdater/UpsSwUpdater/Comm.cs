using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Net;
//using System.Net.Sockets;
using System.IO.Ports;
using MPS_Tools;
using M1787_Monitor; //for M1787Frame

namespace M5748SwUpdater
{
    class SerialComm
    {
        SerialPort ComPort = new SerialPort("COM1", 115200, Parity.None, 8, StopBits.One);
        J1939 j1939 = null;

        protected IFrame basicFrame = new M5748Frame();
        private List<byte> recivedData = new List<byte>();
        public enum CommAddress { Host = 0x0F, Master = 0x0A, Charger = 0x0B, Batt1 = 1, Batt2 = 2, Batt3 = 3, Batt4 = 4, Batt5 = 5, Batt6 = 6, Non = 0 }
        public CommAddress sourceAddress = CommAddress.Host;
        public CommAddress destinationAddress = CommAddress.Batt3;


        public void OpenPort(string comName, int baudRate)
        {
            //this function throws SerialPort exceptions in case of error    
            if (ComPort.IsOpen)
            {
                ComPort.Close();
                System.Threading.Thread.Sleep(500);
            }

            ComPort.PortName = comName;
            ComPort.BaudRate = baudRate;
            ComPort.Parity = Parity.None;
            ComPort.Open();
        }

        private void ClosePort()
        {
            try 
            { 
                ComPort.Close(); 
            }
            catch
            {
                Console.WriteLine("Fail to close the COM");
            }
        }

        public void OpenCan()
        {
            if (j1939 == null)
                //j1939 = new J1939(0x01, 0x23); //
                j1939 = new J1939(0x01,0xE6);

            j1939.CanOpen();
        }

        private void CloseCan()
        {
            try
            {
                j1939.CanClose();
                j1939 = null;
            }
            catch
            {
                Console.WriteLine("Fail to close the CAN");
            }
        }

        public void Close()
        {
            if (IsCanOpened())
                CloseCan();
            else
                ClosePort();
        }


        public bool IsCanOpened()
        {
            return j1939 != null;
        }

        public void ClearRecived()
        {
            if (j1939 != null)
            {
                j1939.clearReceived();
            }
            else
            {
                try { ComPort.DiscardInBuffer(); }
                catch { }
                recivedData.Clear();
            }
        }

        private void ComSend(byte[] buffer)
        {
            try
            {
                ComPort.Write(buffer, 0, buffer.Length);
            }
            catch
            {
                throw new CommFailExeption("Fail to Send data over " + ComPort.PortName); 
            }
        }

        private void CanSend(byte[] buffer)
        {
            try
            {
                j1939.Send(0xEF00, buffer);
            }
            catch
            {
                throw new CommFailExeption("Fail to Send data over Can Bus");
            }
        }

        private void Send(M5748Frame outFrame)
        {
            outFrame.PrepareForTransmition();
            if (j1939 != null)
            {
                byte[] buffer = outFrame.GetFrameBuffer();
                var buffer2 = new ArraySegment<byte>(buffer, 5, buffer.Length - 6).ToArray(); //remove preamble length and crc
                CanSend(buffer2);
            }
            else
            {
                ComSend(outFrame.GetFrameBuffer());
            }
        }



        public const int DefaultTimeOut = 500;
        public int TimeOut = DefaultTimeOut;
        private IFrame ReceiveFrame()
        {
            if (j1939 != null)
            {
                return CanReceiveFrame();
            }
            else
            {
                return ComReceiveFrame();
            }
        }

        private IFrame CanReceiveFrame()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < TimeOut)
            {
                J1939Message msg = j1939.getMessage(TimeOut - (int)sw.ElapsedMilliseconds);

                // FILTER  OPCODE (Application layer)
                if(msg != null && msg.Pgn == 0xEF00 && msg.Data != null && msg.Data.Length > 0)
                {
                    M5748Frame newFrame = new M5748Frame();
                    newFrame.ReadFromBuffer(msg.Data);
                    return newFrame;
                }
            }
            return null;
        }



        private IFrame ComReceiveFrame()
        {
            TimeSpan timeOut = new TimeSpan(0, 0, 0, 0, TimeOut);

            DateTime startTime = DateTime.Now;
            while (true)
            {
                if (DateTime.Now - startTime /*+ passedTime*/ >= timeOut)
                {
                    //passedTime = DateTime.Now - startTime + passedTime;
                    recivedData.Clear();
                    return null;
                }

                if (ComPort.BytesToRead > 0)
                {
                    byte[] buffer = new byte[ComPort.BytesToRead];
                    ComPort.Read(buffer, 0, buffer.Length);
                    recivedData.AddRange(buffer);

                    IFrame newFrame = basicFrame.GetFrameCopy();
                    int[] result = newFrame.ReadFromStream(recivedData.ToArray());
                    if (result[0] >= 0)
                    {
                        recivedData.RemoveRange(0, result[0] + result[1]);
                        return newFrame;
                    }
                }
            }

        }
        
        /*
        private byte[] Receive(ref TimeSpan passedTime)
        {
#warning remove this function
            TimeSpan timeOut = new TimeSpan(0, 0, 0, 1, 0);
#warning is 1Sec enaugh
            DateTime startTime = DateTime.Now;
            while (true)
            {
                if (DateTime.Now - startTime + passedTime >= timeOut)
                {
                    passedTime = DateTime.Now - startTime + passedTime;
                    return null;
                }

                if (ComPort.BytesToRead > 0)
                {
                    byte[] buffer = new byte[ComPort.BytesToRead];
                    ComPort.Read(buffer, 0, buffer.Length);
                    passedTime = DateTime.Now - startTime + passedTime;
                    return buffer;
                }
            }
        }*/

        //private string userName = "admin";
        private int userID = 1;
        public string Password = "";// = "weBPass";
        public int MaxRetries = 3;

        public DataBuffer SendAndReceive(DataBuffer buffer, string description)
        {
            return SendAndReceive(buffer, description, true);
        }

        public DataBuffer SendAndReceive(DataBuffer buffer, string description, bool ID)
        {
            M5748Frame outFrame = ConstructFrame(buffer, description);

            M5748Frame recivedFrame = SendAndReceive(outFrame);

            DataBuffer receivedBuffer = new DataBuffer(recivedFrame.GetDataBuffer());
            System.Threading.Thread.Sleep(20);
            //get security header:
            //receivedBuffer.getByte(); //user id
            //receivedBuffer.getByte(); //security flags

            try { CheckForNack(receivedBuffer); }
            catch (CommFailExeption ex)
            {
                ex.FailedCommandDescription = outFrame.Description;
                throw;
            }
            return receivedBuffer;
        }

        private M5748Frame SendAndReceive(M5748Frame outFrame)
        {
            try
            {
                for (int retries = 0; retries < MaxRetries; retries++)
                {
                    ClearRecived(); //clear recive buffer before sending
                    Send(outFrame);



                    //TimeSpan timeOut = new TimeSpan();
                    M5748Frame recivedFrame = (M5748Frame)ReceiveFrame();
                    if (recivedFrame != null && buffersEqual(outFrame.GetFrameBuffer(), recivedFrame.GetFrameBuffer()))
                    {
                        //the transmitted frame is received back - keep waiting for the responce
                        recivedFrame = (M5748Frame)ReceiveFrame();
                    }

                    //if (recivedFrame != null) && recivedFrame.GetDataBuffer()[0] == 0x81)
                    //{
                    //    // sometimes we receive back the status frame (in case the unit detects a failure and informes the user.
                    //    //  ignore this frame and keep waiting
                    //    recivedFrame = (M1787Frame)ReceiveFrame();
                    //}

                    if (recivedFrame == null)
                    {
                        //nothing recived - try sending the command again
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        return recivedFrame;
                    }
                }
                throw new NoResponceExeption();
            }
            catch (CommFailExeption ex)
            {
                ex.FailedCommandDescription = outFrame.Description;
                throw;
            }
        }


        private bool buffersEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null)
                return false;
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }


        public void Send(DataBuffer buffer, string description)
        {
            M5748Frame outFrame = ConstructFrame(buffer, description);
            outFrame.PrepareForTransmition();
            Send(outFrame);


            //DataBuffer recived = SendAndReceive(buffer, description, ID);
            //VerifyAck(recived);
        }


        private M5748Frame ConstructFrame(DataBuffer buffer, string description)
        {
            //DataBuffer newBuffer = new DataBuffer();
            //newBuffer.fillArray(buffer.GetDataBuffer());
            //M5748Frame frame = new M5748Frame(newBuffer.GetDataBuffer());
            M5748Frame frame = new M5748Frame(buffer.GetDataBuffer());
            frame.Description = description;
            frame.Destination = (int)destinationAddress;
            frame.Sourse = (int)sourceAddress;
            return frame;
        }

        /*
        private bool IsAckTelem(DataBuffer buffer)
        {
            //buffer.getByte(); //userId
            //buffer.getByte(); //flags
            if (buffer.getShort() != 0x8000) //ACK OpCode
                throw new WrongResponceExeption(); //return false;
                
            
            uint errorOpCode = buffer.getShort();
            uint errorCode = buffer.getShort();
            string errorDescription = buffer.getString(64);

            if (errorCode != 0 && errorCode != 1)
            {
                throw new NackResponceExeption(errorOpCode, errorCode, errorDescription);
            }

            return true;
        }*/

        private const int ACK_TEL_LENGTH = 2;
        private void VerifyAck(DataBuffer buffer)
        {
            if (buffer.Length < ACK_TEL_LENGTH)
                throw new WrongResponceExeption();
            
            if (buffer.getByte() != 0x80) //ACK OpCode
                throw new WrongResponceExeption(); //return false;

            int errorCode = buffer.getByte();


            if (errorCode != 1)
            {
                //throw new NackResponceExeption(errorOpCode, errorCode, errorDescription);
                throw new NackResponceExeption((uint)errorCode, ""); //TODO: add error description and may be opcode
            }

            return;
        }
        
        private void CheckForNack(DataBuffer buffer)
        {
            if (buffer.Length < ACK_TEL_LENGTH)
                return;
            
            DataBuffer bufferCopy = new DataBuffer(buffer);

            if (bufferCopy.getByte() != 0x80) //ACK OpCode
                return;
            else
            {
                bufferCopy = new DataBuffer(buffer);
                VerifyAck(bufferCopy);
            }
        }
    }

    public class CommFailExeption : ApplicationException
    {
        public CommFailExeption() : base(""){}
        public CommFailExeption(string message) : base(message){}
        public CommFailExeption(string message, Exception inner) : base(message, inner) { }

        public string FailedCommandDescription;
    }


    public class NoResponceExeption : CommFailExeption
    {
        public NoResponceExeption(){}
        public NoResponceExeption(string message) : base(message){}
        public NoResponceExeption(string message, Exception inner) : base(message, inner){}

        public override string Message
        {
            get { return "No Response " + base.Message; }
        }
    }

    public class WrongResponceExeption : CommFailExeption
    {
        public WrongResponceExeption() { }
        public WrongResponceExeption(string message) : base(message) { }
        public WrongResponceExeption(string message, Exception inner) : base(message, inner) { }
        public override string Message
        {
            get { return "Wrong Response " + base.Message; }
        }
    }

    public class NackResponceExeption : CommFailExeption
    {
        public NackResponceExeption() { }
        public NackResponceExeption(string message) : base(message) { }
        public NackResponceExeption(string message, Exception inner) : base(message, inner) { }

        public NackResponceExeption(uint errorCode, string nackMessage)
        {
            ErrorCode = errorCode;
            NackMessage = nackMessage;
        }

        public uint ErrorCode = 0;
        public string NackMessage;

        public override string Message
        {
            get 
            {
                string messageText = "NACK Error #" + ErrorCode.ToString("X2");
                messageText += "\n" + NackMessage;
                return messageText;
            }
        }
    }
}
