using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS_Tools;

namespace M339_Monitor
{
    public class M339IntFrame : BasicFrame
    {
        protected override int HeaderLength { get { return 9; } }
        protected override int TrailerLength { get { return 2; } }
        protected override int MaxDataLength { get { return 255; } }
        protected static int commandsCounter = 0;

        public int Destination = 0x01;
        public const int Sourse = 0x0f;

        //default constructor
        public M339IntFrame()
        {
            init(new byte[0], MPS_FrameType.Unknown);
        }

        //copy constructor
        public M339IntFrame(M339IntFrame frame)
        {
            buffer = new List<byte>(frame.buffer);
            type = frame.type;
            description = frame.description;
        }

        public M339IntFrame(byte[] buffer)
        {
            init(buffer, MPS_FrameType.Command);
        }

        public override IFrame GetFrameCopy()
        {
            return new M339IntFrame(this);
        }

        public int Counter
        {
            get
            {
                //if (type == MPS_FrameType.Telemetry)
                    return buffer[7];
                //else
                //    return -1;
            }
        }
        
        private static byte[] commandMagicNumber = { 0xD7, 0xEE, 0xA6, 0xC8 };
        private static byte[] telemMagicNumber = { 0xD7, 0xEE, 0xA6, 0xC8 };

        protected override void RecalcHeaderTrailer()
        {
            if (type == MPS_FrameType.Command)
            {
                if (buffer.Count < MinFrameLength)
                    throw new Exception("Command too short");

                //fill magic number
                for (int i = 0; i < commandMagicNumber.Length; i++)
                    buffer[i] = commandMagicNumber[i];

                //fill data length
                buffer[4] = (byte)(((DataLength + 3) >> 8) & 0xFF);
                buffer[5] = (byte)(((DataLength + 3) & 0xFF));
                buffer[6] = (byte)((Sourse << 4) | (Destination & 0x0F));
                //fill counter
                //buffer[7] = (byte)commandsCounter;
                //commandsCounter++;
                //if (commandsCounter > 255)
                //    commandsCounter = 1;
                buffer[8] = 0;

                //fill crc
                int crc = CalcCRC(buffer.ToArray(), buffer.Count - TrailerLength);
                buffer[buffer.Count - 2] = (byte)((crc >> 8) & 0xFF);
                buffer[buffer.Count - 1] = (byte)(crc & 0xFF);
            }
            else if (type == MPS_FrameType.Telemetry)
            {
                //no nead to change telem. frame
            }
            else
            {
                //no nead to change unknown frame
            }
        }

        public override void PrepareForTransmition()
        {
            commandsCounter++;
            if (commandsCounter > 255)
                commandsCounter = 1;
            buffer[7] = (byte)commandsCounter;
            RecalcHeaderTrailer();
        }

        public static int CommandCounter
        {
            get { return commandsCounter; }
        }


        public override int[] ReadFromStream(byte[] stream)
        {
            int[] result = new int[2] { -1, -1 };

            int start = findSubArray(stream, telemMagicNumber);
            if (start < 0)
                return result; //magic number not found
            if (start + MinFrameLength > stream.Length)
                return result; //not enaugh data in stream

            int dataLength = (stream[4] << 8) | stream[5];
            dataLength -= 3;
            int frameLength = MinFrameLength + dataLength;
            if (start + frameLength > stream.Length)
                return result; //not enaugh data in stream

            //frame found
            result[0] = start;
            result[1] = frameLength;

            buffer = new List<byte>(frameLength);

            for (int i = 0; i < frameLength; i++)
                buffer.Add(stream[i]);

            type = MPS_FrameType.Telemetry;
            return result;
        }


        protected int findSubArray(byte[] array, byte[] subArray)
        {
            int i;
            for (i = 0; i < array.Length - subArray.Length; i++)
            {
                int j;
                for (j = 0; j < subArray.Length; j++)
                {
                    if (array[i + j] != subArray[j])
                        break;
                }
                if (j == subArray.Length)
                    break;
            }

            if (i < array.Length - subArray.Length)
                return i;
            else
                return -1;
        }

        public override bool CsumOk()
        {
            int csum = (buffer[buffer.Count - 2] << 8) | buffer[buffer.Count - 1];

            if (csum == CalcCRC(buffer.ToArray(), buffer.Count - 2))
                return true;
            else
                return false;
        }

        public override bool LengthOk()
        {
            int dataLength = (buffer[4] << 8) | buffer[4];

            if (MinFrameLength + dataLength == buffer.Count)
                return true;
            else
                return false;
        }

    }
}
