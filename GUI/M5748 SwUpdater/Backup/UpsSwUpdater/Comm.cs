using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Net;
//using System.Net.Sockets;
using System.IO.Ports;
using MPS_Tools;
using M339_Monitor; //for M339IntFrame

namespace M339SWUpdater
{
    class SerialComm
    {
        SerialPort ComPort = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);
        protected IFrame basicFrame = new M339IntFrame();
        private List<byte> recivedData = new List<byte>();
        
         
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
            ComPort.Parity = Parity.Even;
            ComPort.Open();

        }

        public void ClosePort()
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

        private void ClearRecived()
        {
            try
            {
                ComPort.DiscardInBuffer();
            }
            catch { }
            recivedData.Clear();
        }

        private void Send(byte[] buffer)
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



        public const int DefaultTimeOut = 500;
        public int TimeOut = DefaultTimeOut;
        private IFrame ReceiveFrame()
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
                        recivedData.RemoveRange(result[0], result[1]);
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
            M339IntFrame outFrame = ConstructFrame(buffer, description, ID);

            M339IntFrame recivedFrame = SendAndReceive(outFrame);

            DataBuffer receivedBuffer = new DataBuffer(recivedFrame.GetDataBuffer());
            System.Threading.Thread.Sleep(50);
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

        private M339IntFrame SendAndReceive(M339IntFrame outFrame)
        {
            try
            {
                for (int retries = 0; retries < MaxRetries; retries++)
                {
                    ClearRecived(); //clear recive buffer before sending
                    outFrame.PrepareForTransmition();
                    Send(outFrame.GetFrameBuffer());
                    
                    
                    
                    //TimeSpan timeOut = new TimeSpan();
                    M339IntFrame recivedFrame = (M339IntFrame)ReceiveFrame();
                    if (recivedFrame == null ||
                        recivedFrame.Counter != outFrame.Counter)
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

        public void Send(DataBuffer buffer, string description)
        {
            Send(buffer, description, true);
        }

        public void Send(DataBuffer buffer, string description, bool ID)
        {
            DataBuffer recived = SendAndReceive(buffer, description, ID);
            VerifyAck(recived);
        }


        private M339IntFrame ConstructFrame(DataBuffer buffer, string description, bool ID)
        {
            DataBuffer newBuffer = new DataBuffer();

            /*
            if (ID)
            {
                newBuffer.fillByte(userID); //Admin ID
                newBuffer.fillByte(0x80); //Command with password
                newBuffer.fillString(Password, 16);
            }
            else
            {
                newBuffer.fillByte(0); //no ID
                newBuffer.fillByte(0x00);//Command without password
            }*/

            newBuffer.fillArray(buffer.GetDataBuffer());
            M339IntFrame frame = new M339IntFrame(newBuffer.GetDataBuffer());
#if DEV_MASTER
            frame.Destination = 1;
#elif DEV_SLAVE1
            frame.Destination = 2;
#else
            frame.Destination = 3;
#endif
            frame.Description = description;
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

        private const int ACK_TEL_LENGTH = 4;
        private void VerifyAck(DataBuffer buffer)
        {
            if (buffer.Length < ACK_TEL_LENGTH)
                throw new WrongResponceExeption();
            
            if (buffer.getShort() != 0x8000) //ACK OpCode
                throw new WrongResponceExeption(); //return false;

            //uint errorOpCode = buffer.getShort();
            uint errorCode = buffer.getShort();
            //string errorDescription = buffer.getString(64);

            if (/*errorCode != 0 &&*/ errorCode != 1)
            {
                //throw new NackResponceExeption(errorOpCode, errorCode, errorDescription);
                throw new NackResponceExeption(0, errorCode, ""); //TODO: add error description and may be opcode
            }

            return;
        }
        
        private void CheckForNack(DataBuffer buffer)
        {
            if (buffer.Length < ACK_TEL_LENGTH)
                return;
            
            DataBuffer bufferCopy = new DataBuffer(buffer);

            if (bufferCopy.getShort() != 0x8000) //ACK OpCode
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

        public NackResponceExeption(uint opCode, uint errorCode, string nackMessage)
        {
            OpCode = opCode;
            ErrorCode = errorCode;
            NackMessage = nackMessage;
        }

        public uint ErrorCode = 0;
        public uint OpCode = 0;
        public string NackMessage;

        public override string Message
        {
            get 
            {
                string messageText = "NACK Error #" + ErrorCode.ToString("X4");
                if (ErrorCode != 0)
                    messageText += " in command " + OpCode.ToString("X4");
                messageText += "\n" + NackMessage;
                return messageText;
            }
        }
    }
}
