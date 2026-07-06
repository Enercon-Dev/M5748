using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* changes:
UpsV3_Frame.Counter return valid value even if the frame is not Telemetry_Frame

*/
namespace MPS_Tools
{
    public class DataBuffer
    {
        protected List<byte> data;

        //default constructor
        public DataBuffer()
        {
            data = new List<byte>();
        }

        public DataBuffer(byte[] buffer)
        {
            data = new List<byte>(buffer);
        }

        //copy constructor
        public DataBuffer(DataBuffer buffer)
        {
            data = new List<byte>(buffer.data);
            offset = buffer.offset;
            bitOffset = buffer.bitOffset;
        }
        
        protected int offset = 0;
        public int Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        protected int bitOffset = 0;
        public int BitOffset
        {
            get { return bitOffset; }
        }

        public int Length
        {
            get { return data.Count; }
            set 
            {
                if (value > data.Count)
                {
                    data[value - 1] = 0; //todo: chack if this line actuali increase the length
                }
            }
        }

        public int RemainingBytes
        {
            get 
            {
                int remaining = data.Count - offset;
                if (BitOffset != 0)
                    remaining--;
                return remaining;
            }
        }

        public byte[] GetDataBuffer()
        {
            return data.ToArray();
        }

        public void SetDataBuffer(byte[] buffer)
        {
            data = new List<byte>(buffer);
            offset = 0;
            bitOffset = 0;
        }
        
        public byte this[int i]
        {
            get
            {
                int index = offset + i;
                if (index > Length - 1)
                    throw new Exception("index out of range");

                return data[index];
            }
            set
            {
                int index = offset + i;

                if (index >= data.Count)
                    data.AddRange(new byte[index + 1 - data.Count]);

                data[index] = value;
            }
        }

        protected void IncBitOffset()
        {
            bitOffset += 1;
            if (BitOffset >= 8)
            {
                bitOffset = 0;
                offset += 1;
            }
        }

        protected void roundBitOffset()
        {
            if (bitOffset != 0)
            {
                bitOffset = 0;
                offset += 1;
            }
        }

        public bool getBit()
        {
            bool data = ((this[0] & (1 << BitOffset)) != 0) ? true : false;
            IncBitOffset();
            return data;
        }

        public void fillBit(bool data)
        {
            int x = data ? 1 : 0;

            this[0] = (byte)(x << BitOffset);
            //todo: shuld there be a IncBitOffset(); ??
        }

        public int getByte()
        {
            roundBitOffset();
            int data = this[0];
            Offset += 1;
            return data;
        }
        
        public void fillByte(int data)
        {
            roundBitOffset();
            this[0] = (byte)(data & 0xFF);
            Offset += 1;
        }

        public int[] getArray(int length)
        {
            int[] data = new int[length];
            for (int x = 0; x < length; x++)
            {
                data[x] = getByte();
            }

            return data;
        }      
        
        public void fillArray(int[] data)
        {
            for (int x = 0; x < data.Length; x++)
            {
                fillByte(data[x]);
            }
        }

        public void fillArray(byte[] data)
        {
            for (int x = 0; x < data.Length; x++)
            {
                fillByte(data[x]);
            }
        }

        /*
        public virtual string getString(int maxLength)
        {
            roundBitOffset();
            string str = "";

            for (int x = 0; x < maxLength; x++)
            {
                if (this[x] == 0)
                    break;
                str += (char)(this[x]);
            }

            Offset += maxLength;

            return str;
        }*/


        public virtual string getString(int maxLength)
        {
            roundBitOffset();
            string str = "";
            while (RemainingBytes > 0)
            {
                int ch = getByte();
                if (ch != 0)
                    str += (char)ch;
                else
                    return str;
                
                if (str.Length >= maxLength)
                    return str;
            }

            return null;
        }

        /*
        public virtual bool fillString(string str, int maxLength)
        {
            if (str.Length >= maxLength)
                return false;

            for (int x = 0; x < str.Length; x++)
            {
                if ((str[x] < 32) || (str[x] >= 127)) //TODO: change to better check
                    return false;
            }


            for (int x = 0; x < maxLength; x++)
            {
                if (x < str.Length)
                {
                    fillByte(str[x]);
                }
                else
                {
                    fillByte(0);
                }
            }
            return true;
        }*/

        public virtual bool fillString(string str, int maxLength)
        {
            for (int x = 0; x < str.Length; x++)
            {
                if ((str[x] < 32) || (str[x] >= 127)) //TODO: change to better check
                    return false;
            }

            for (int x = 0; x < str.Length; x++)
            {
                fillByte(str[x]);
            }
            fillByte(0);
            return true;
        }


        public bool BigEndian = true;

        protected void ReverceOrder(int[] array)
        {
            int[] result = new int[array.Length];
            
            for (int i = 0; i < array.Length; i++)
            {
                result[i] = array[array.Length - 1 - i];
            }

            result.CopyTo(array, 0);
            //return result;
        }


        public virtual UInt32 getShort()
        {
            int[] array = getArray(2);
            if (!BigEndian) ReverceOrder(array); 
            
            UInt32 data = 0;
            data += ((UInt32)array[0]) << 8;
            data += ((UInt32)array[1]);
            return data;
        }

        public virtual void fillShort(UInt32 data)
        {
            int[] array = new int[2];
            array[0] = (int)((data >> 8) & 0xFF);
            array[1] = (int)(data & 0xFF);

            if (!BigEndian) ReverceOrder(array);
            fillArray(array);
        }

        public virtual UInt32 getLong()
        {
            int[] array = getArray(4);
            if (!BigEndian) ReverceOrder(array);
            
            UInt32 data = 0;
            data += ((UInt32)array[0]) << 24;
            data += ((UInt32)array[1]) << 16;
            data += ((UInt32)array[2]) << 8;
            data += ((UInt32)array[3]);
            return data;
        }

        public virtual void fillLong(UInt32 data)
        {
            int[] array = new int[4];
            array[0] = (byte)((data >> 24) & 0xFF);
            array[1] = (byte)((data >> 16) & 0xFF);
            array[2] = (byte)((data >> 8) & 0xFF);
            array[3] = (byte)(data & 0xFF);

            if (!BigEndian) ReverceOrder(array);
            fillArray(array);
        }


        protected virtual UInt32 GetBits(int length, int byteSize)
        {
            if ((byteSize != 8) && (byteSize != 7))
                throw new Exception("wrong byteSize parameter");

            if (length <= 0)
                return 0;

            UInt32 data = 0;

            if (BitOffset >= byteSize)
                roundBitOffset();

            if (length < (byteSize - BitOffset))
            {
                for (int i = 0; i < length; i++)
                {
                    if (getBit() == true)
                        data += (UInt32)1 << i;
                }
            }
            else if (((length % byteSize) == 0) &&
                     (length <= byteSize*4))
            {
                int[] array = getArray(length / byteSize);
                if (!BigEndian) ReverceOrder(array);

                for (int i = 0; i < array.Length; i++)
                    data += (UInt32)array[array.Length -1 -i] << (byteSize * i);
            }
            else
            {
                throw new Exception("access to buffer with wrong bit length parameter");
            }
 
            return data;
        }

        public virtual UInt32 GetBits(int length)
        {
            return GetBits(length,8);
        }
        public virtual UInt32 GetBits_7bit(int length)
        {
            return GetBits(length,7);
        }

        public virtual bool fillHexStr(string str, int breakes)
        {
            if (((str.Length + 1) % (2 + breakes)) != 0)
                return false;

            int length = (str.Length + 1) / (2 + breakes);
            int[] data = new int[length];

            for (int x = 0; x < length; x++)
            {
                try
                {
                    data[x] = Convert.ToInt32(str.Substring(x * (2 + breakes), 2), 16);
                }
                catch
                {
                    return false;
                }
            }

            for (int x = 0; x < length; x++)
            {
                fillByte(data[x]);
            }
            return true;
        }

        public virtual string getHexStr(int length, string breakStr)
        {
            roundBitOffset();
            string str = "";

            for (int x = 0; x < length; x++)
            {
                str += this[x].ToString("X2");
                if (x != (length - 1))
                    str += breakStr;
            }

            Offset += length;

            return str;
        }

        public virtual string getIp()
        {
            int[] array = getArray(4);
            if (!BigEndian) ReverceOrder(array);
            
            string str = "";
            str += array[0].ToString() + ".";
            str += array[1].ToString() + ".";
            str += array[2].ToString() + ".";
            str += array[3].ToString();
            return str;
        }
    }

    
    
    public interface IFrame
    {
        IFrame GetFrameCopy();
        string Description { get; set; }

        int[] ReadFromStream(byte[] buffer);
        void SetData(byte[] dataBuffer);
        byte[] GetFrameBuffer();
        byte[] GetDataBuffer();
        int FrameLength { get; }
        int DataLength { get; set; }

        MPS_FrameType FrameType { get; set; }
        byte this[int i] { get; set; }
        
        void PrepareForTransmition();

        bool CsumOk();
        bool LengthOk();

        string ToHexString();
    }

    public enum MPS_FrameType { Unknown, Telemetry, Command };


    public class BasicFrame : IFrame
    {
        protected List<byte> buffer;

        protected virtual int HeaderLength { get { return 0; } }
        protected virtual int TrailerLength { get { return 0; } }
        protected virtual int MaxDataLength { get { return 0; } }
        protected virtual int MaxFrameLength
        {
            get { return HeaderLength + MaxDataLength + TrailerLength; }
        }
        
        protected virtual int MinFrameLength
        {
            get { return HeaderLength + TrailerLength; }
        }

        public virtual IFrame GetFrameCopy()
        {
            return new BasicFrame(this);
        }

        protected string description = "";
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        protected MPS_FrameType type = MPS_FrameType.Unknown;
        public MPS_FrameType FrameType 
        {
            get { return type; }
            set { type = value; }
        }


        protected virtual void init(byte[] dataBuffer, MPS_FrameType frameType)
        {
            buffer = new List<byte>(new byte[HeaderLength + TrailerLength]);
            //buffer[dataBuffer.Length + HeaderLength + TrailerLength - 1] = 0;
            buffer.InsertRange(HeaderLength, dataBuffer);
            type = frameType;
            RecalcHeaderTrailer();
        }


        //default constructor
        public BasicFrame()
        {
            init(new byte[0], MPS_FrameType.Unknown);
        }

        //copy constructor
        public BasicFrame(BasicFrame frame)
        {
            buffer = new List<byte>(frame.buffer);
            type = frame.type;
            description = frame.description;
        }

        public BasicFrame(byte[] buffer)
        {
            init(buffer, MPS_FrameType.Command);
        }

        
        protected virtual void RecalcHeaderTrailer()
        {

        }

        public virtual int[] ReadFromStream(byte[] stream)
        {
            type = MPS_FrameType.Telemetry;
            int[] result = new int[2] { 0, 0 };
            return result;
        }

        public void SetData(byte[] dataBuffer)
        {
            buffer.RemoveRange(HeaderLength, buffer.Count - HeaderLength - TrailerLength);
            buffer.InsertRange(HeaderLength, dataBuffer);
            RecalcHeaderTrailer();
        }

        public byte[] GetFrameBuffer()
        {
            return buffer.ToArray();
        }

        public byte[] GetDataBuffer()
        {
            byte[] data = new byte[DataLength];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = buffer[HeaderLength + i];
            }

            return data;
        }

        public byte this[int i]
        {
            get
            {
                 if (i > buffer.Count-1)
                    throw new Exception("index out of range");
                
                return buffer[i];
             }
            set
            {
                buffer[i] = value;
                RecalcHeaderTrailer();
            }
        }

        public int FrameLength 
        { 
            get {return buffer.Count;} 
        }

        public int DataLength
        {
            get { return FrameLength - HeaderLength - TrailerLength; }
            set
            {
                if (value < DataLength)
                {
                    buffer.RemoveRange(HeaderLength + value, DataLength - value);
                }
                else
                {
                    buffer.InsertRange(DataLength - TrailerLength, new byte[value - DataLength]);
                }
            }
        }

        static int[] crc_table = {
            0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5, 0x60c6, 0x70e7, 0x8108, 0x9129, 0xa14a, 0xb16b, 0xc18c, 0xd1ad, 0xe1ce, 0xf1ef,
            0x1231, 0x0210, 0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6, 0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c, 0xf3ff, 0xe3de,
            0x2462, 0x3443, 0x0420, 0x1401, 0x64e6, 0x74c7, 0x44a4, 0x5485, 0xa56a, 0xb54b, 0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
            0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6, 0x5695, 0x46b4, 0xb75b, 0xa77a, 0x9719, 0x8738, 0xf7df, 0xe7fe, 0xd79d, 0xc7bc,
            0x48c4, 0x58e5, 0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823, 0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969, 0xa90a, 0xb92b,
            0x5af5, 0x4ad4, 0x7ab7, 0x6a96, 0x1a71, 0x0a50, 0x3a33, 0x2a12, 0xdbfd, 0xcbdc, 0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a, 
            0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03, 0x0c60, 0x1c41, 0xedae, 0xfd8f, 0xcdec, 0xddcd, 0xad2a, 0xbd0b, 0x8d68, 0x9d49, 
            0x7e97, 0x6eb6, 0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70, 0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a, 0x9f59, 0x8f78, 
            0x9188, 0x81a9, 0xb1ca, 0xa1eb, 0xd10c, 0xc12d, 0xf14e, 0xe16f, 0x1080, 0x00a1, 0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067, 
            0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c, 0xe37f, 0xf35e, 0x02b1, 0x1290, 0x22f3, 0x32d2, 0x4235, 0x5214, 0x6277, 0x7256, 
            0xb5ea, 0xa5cb, 0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d, 0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405, 
            0xa7db, 0xb7fa, 0x8799, 0x97b8, 0xe75f, 0xf77e, 0xc71d, 0xd73c, 0x26d3, 0x36f2, 0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634, 
            0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9, 0xb98a, 0xa9ab, 0x5844, 0x4865, 0x7806, 0x6827, 0x18c0, 0x08e1, 0x3882, 0x28a3, 
            0xcb7d, 0xdb5c, 0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a, 0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0, 0x2ab3, 0x3a92, 
            0xfd2e, 0xed0f, 0xdd6c, 0xcd4d, 0xbdaa, 0xad8b, 0x9de8, 0x8dc9, 0x7c26, 0x6c07, 0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1, 
            0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba, 0x8fd9, 0x9ff8, 0x6e17, 0x7e36, 0x4e55, 0x5e74, 0x2e93, 0x3eb2, 0x0ed1, 0x1ef0};
  
        public static int CalcCRC(byte[] buffer, int length)
        {
            int crc = 0;
            int index;

            for (int counter = 0; counter < length; counter++)
            {
                index = (crc >> 8) ^ buffer[counter];
                crc = (crc << 8) ^ crc_table[index & 0xFF];
            }
            return (crc & 0xFFFF);
        }

        public static int CalcCRC(byte[] buffer)
        {
            return CalcCRC(buffer, buffer.Length);
        }

        public virtual bool CsumOk()
        {
            return true;
        }

        public virtual bool LengthOk()
        {
            return true;
        }

        public virtual void PrepareForTransmition()
        {

        }

        public virtual string ToHexString(string delaminator)
        {
            string DataStr = "";
            
            for (int i = 0; i < buffer.Count; i++)
            {
                DataStr += buffer[i].ToString("X2") + delaminator;
                if (i == (HeaderLength - 1) || i == (buffer.Count - TrailerLength - 1)) 
                    DataStr += "   ";
            }

            return DataStr;
        }

        public virtual string ToHexString()
        {
            return ToHexString(" ");
        }

        public override string ToString()
        {
            return ToHexString();
        }
    }

    public class UpsV3_Frame : BasicFrame
    {
        protected override int HeaderLength { get { return 8; } }
        protected override int TrailerLength { get { return 2; } }
        protected override int MaxDataLength { get { return 255; } }
        protected static int commandsCounter = 0;

        //default constructor
        public UpsV3_Frame()
        {
            init(new byte[0], MPS_FrameType.Unknown);
        }

        //copy constructor
        public UpsV3_Frame(UpsV3_Frame frame)
        {
            buffer = new List<byte>(frame.buffer);
            type = frame.type;
            description = frame.description;
        }

        public UpsV3_Frame(byte[] buffer)
        {
            init(buffer, MPS_FrameType.Command);
        }

        public override IFrame GetFrameCopy()
        {
            return new UpsV3_Frame(this);
        }

        public int Counter
        {
            get 
            {
                //if (type == MPS_FrameType.Telemetry)
                    return buffer[6];
                //else
                //    return -1;
            }
        }

        private static byte[] commandMagicNumber = { 0x89, 0x15, 0xB3, 0x87 };
        private static byte[] telemMagicNumber = { 0x87, 0xB3, 0x15, 0x89 };
        
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
                buffer[4] = (byte)(((DataLength+2) >> 8) & 0xFF);
                buffer[5] = (byte)(((DataLength+2) & 0xFF));
                
                //fill counter
                //buffer[6] = (byte)commandsCounter;
                //commandsCounter++;
                //if (commandsCounter > 255)
                //    commandsCounter = 1;
                buffer[7] = 0;

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
            buffer[6] = (byte)commandsCounter;
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
            if ( start + MinFrameLength  > stream.Length)
                return result; //not enaugh data in stream

            int dataLength = (stream[4]<<8) | stream[5];
            dataLength -= 2;
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
            for (i = 0; i < array.Length - subArray.Length; i++ )
            {
                int j;
                for (j=0; j<subArray.Length; j++)
                {
                    if (array[i + j] != subArray[j])
                        break;
                }
                if (j == subArray.Length)
                    break;
            }

            if (i < array.Length)
                return i;
            else
                return -1;
        }

        public override bool CsumOk()
        {
            int csum = (buffer[buffer.Count - 2]<<8) | buffer[buffer.Count - 1];

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
