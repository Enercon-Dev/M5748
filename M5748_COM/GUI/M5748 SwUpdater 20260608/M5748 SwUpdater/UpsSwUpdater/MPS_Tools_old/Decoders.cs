using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS_Tools
{
    public interface IDecoder
    {
        //void Decode(DataBuffer buffer, DBField field);
        string[] Contexts {get; /*set;*/}
        void Decode(string context, DataBuffer buffer, DBField field);
        bool IsContextSupported(string context);
    }

    public abstract class Decoder : IDecoder 
    {
        public Decoder()
        {
            //BigEndian = BigEndian_Default;
        }
        
        //Endiannes shuld be handeled on DataBuffer level. 
        //There is no reason to support diferent types of endiannes in same buffer
        //public static bool BigEndian_Default = true;
        //public bool BigEndian;

        protected string context = "";
        public virtual string[] Contexts
        {
            get { return new string[] {context}; }
            //set { context = value; }
        }

        public virtual bool IsContextSupported(string context)
        {
            foreach (string s in Contexts)
            {
                if (s == context)
                    return true;
            }
            return false;
        }
        
        //public abstract void Decode(DataBuffer buffer, DBField field);
        public virtual void Decode(string _context, DataBuffer buffer, DBField field)
        {
            //check the _context against Context property since it may be overwriten by a child 
            if ((Contexts.Length != 1) || (_context != Contexts[0]))
                throw new Exception("context \"" + _context + " isnt supported by this Decoder");
            else
                SimpleDecode(buffer, field);
        }
        
        //this function does the decoding without checking the context. for this reason it should be private.
        //Override it if the decoder support single context, else override the Decode(string, DataBuffer, DBField) function
        protected abstract void SimpleDecode(DataBuffer buffer, DBField field);

        public virtual int FieldSize
        {
            get { return 0; }
            set { }
        }

        /*
#warning "getBitsFromBuffer, getBytesFromBuffer, getDataFromBuffer shuld be part of the buffer interfase"
        protected uint getBitsFromBuffer(DataBuffer buffer, int bits)
        {
            if ((bits < 0) || (bits > 32))
            {
                throw new Exception("bits parameter illigal");
            }

            uint result = 0;
            for (int i = 0; i < bits; i++)
            {
                if (buffer.getBit())
                    result |= (uint)1 << i;
            }

            return result;
        }

        protected uint getBytesFromBuffer(DataBuffer buffer, int bytes)
        {
            if ((bytes < 0) || (bytes > 4))
            {
                throw new Exception("bytes parameter illigal");
            }

            uint result = 0;

            for (int i = 0; i < bytes; i++)
            {
                if (BigEndian)
                {
                    result |= (uint)(buffer.getByte()) << ((bytes - 1 - i) * 8);
                }
                else
                {
                    result |= (uint)(buffer.getByte()) << (i * 8);
                }
            }

            return result;
        }

        protected uint getDataFromBuffer(DataBuffer buffer, int bits)
        {
            if ((bits % 8) == 0)
            {
                return getBytesFromBuffer(buffer, bits / 8);
            }
            else
            {
                return getBitsFromBuffer(buffer, bits);
            }
        }

        protected uint get7bitDataFromBuffer(DataBuffer buffer, int bits)
        {
            if ((bits % 7) != 0)
            {
                throw new Exception("bits mast be x7");
            }

            uint data = getBytesFromBuffer(buffer, bits / 7);

            return (data & 0x0000007F) |
                   ((data & (0x0000007F << 7)) >> 1) |
                   ((data & (0x0000007F << 14)) >> 2) |
                   ((data & (0x0000007F << 21)) >> 3);
        }
        */
    }


    public class FlagDecoder : Decoder
    {
        public FlagDecoder() : base() { }
        public FlagDecoder(int _fieldSize, bool _reverced)
            : base()
        {
            fieldSize = _fieldSize;
            reverced = _reverced;
        }

        public FlagDecoder(string _context, int _fieldSize, bool _reverced)
            : base()
        {
            context = _context;
            fieldSize = _fieldSize;
            reverced = _reverced;
        }


        private int fieldSize = 1;
        public override int FieldSize
        {
            get {return fieldSize;}
            set {fieldSize = value;}
        }

        private bool reverced;
        public bool Reverced
        {
            get { return reverced; }
            set { reverced = value; }
        }

        protected override void SimpleDecode(DataBuffer buffer, DBField field)
        {
            if (!(field is DBFlagField))
            {
                throw new Exception("the field is not a DBFlagField");
            }
            
            //int data = (int)getDataFromBuffer(buffer, fieldSize);
            int data = (int)buffer.GetBits(fieldSize);
            (field as DBFlagField).RowData = data;
            field.RowDataString = data.ToString();
            (field as DBFlagField).Value = (data != 0) ? (!reverced) : reverced;
        }
    }

    public class TxtFlagDecoder : Decoder
    {
        public TxtFlagDecoder() : base() { }
        public TxtFlagDecoder(bool _reverced)
            : base()
        {
            reverced = _reverced;
        }

        public TxtFlagDecoder(string _context, bool _reverced)
            : base()
        {
            context = _context;
            reverced = _reverced;
        }

        private bool reverced;
        public bool Reverced
        {
            get { return reverced; }
            set { reverced = value; }
        }

        protected override void SimpleDecode(DataBuffer buffer, DBField field)
        {
            if (!(field is DBFlagField))
            {
                throw new Exception("the field is not a DBFlagField");
            }

            char ch = Convert.ToChar(buffer.getByte());
            //(field as DBFlagField).RowData = ch;
            field.RowDataString = ch.ToString();
            if (ch == '0')
                (field as DBFlagField).Value = reverced;
            else if (ch == '1')
                (field as DBFlagField).Value = !reverced;
            else
            {
                //wrong/unexpected char
            }
        }
    }


    public class RawIntDecoder : Decoder
    {
        public RawIntDecoder() : base() { }
        public RawIntDecoder(int _fieldSize)
            : base()
        {
            fieldSize = _fieldSize;
        }

        public RawIntDecoder(string _context, int _fieldSize)
            : base()
        {
            context = _context;
            fieldSize = _fieldSize;
        }


        protected int fieldSize = 8;
        public override int FieldSize
        {
            get {return fieldSize;}
            set {fieldSize = value;}
        }

        protected override void SimpleDecode(DataBuffer buffer, DBField field)
        {
            //int data = (int)getDataFromBuffer(buffer, fieldSize);
            int data = (int)buffer.GetBits(fieldSize);
            field.RowData = data;
            field.RowDataString = data.ToString();
        }
    }


    public class RawIntDecoder7bit : RawIntDecoder
    {
        public RawIntDecoder7bit() : base() { }
        public RawIntDecoder7bit(int _fieldSize)
            : base(_fieldSize) { }
        public RawIntDecoder7bit(string _context, int _fieldSize)
            : base(_context, _fieldSize) { }

        protected override void SimpleDecode(DataBuffer buffer, DBField field)
        {
            //int data = (int)get7bitDataFromBuffer(buffer, fieldSize);
            int data = (int)buffer.GetBits_7bit(fieldSize);
            field.RowData = data;
            field.RowDataString = data.ToString();
        }
    }


    public class AnalogDecoder : RawIntDecoder
    {
        public AnalogDecoder() : base() { }
        public AnalogDecoder(int _fieldSize, double _aFactor, double _bFactor)
            : base(_fieldSize)
        {
            //fieldSize = _fieldSize;
            aFactor = _aFactor;
            bFactor = _bFactor;
        }

        public AnalogDecoder(string _context, int _fieldSize, double _aFactor, double _bFactor)
            : base(_context, _fieldSize)
        {
            //context = _context;
            //fieldSize = _fieldSize;
            aFactor = _aFactor;
            bFactor = _bFactor;
        }


        /*
        protected int fieldSize = 8;
        public override int FieldSize
        {
            get
            {
                return fieldSize;
            }
            set
            {
                fieldSize = value;
            }
        }*/

        protected double aFactor = 1;
        public double AFactor
        {
            get { return aFactor; }
            set { aFactor = value; }
        }

        protected double bFactor = 0;
        public double BFactor
        {
            get { return bFactor; }
            set { bFactor = value; }
        }

        protected override void SimpleDecode(DataBuffer buffer, DBField field)
        {
            if (!(field is DBAnalogField))
            {
                throw new Exception("the field is not a DBAnalogField");
            }
            base.SimpleDecode(buffer, field);
            //int data = (int)getDataFromBuffer(buffer, fieldSize);
            //(field as DBAnalogField).RowData = data;
            //field.RowDataString = data.ToString();
            (field as DBAnalogField).Value = field.RowData * aFactor + bFactor;
        }
    }




    public class AnalogDecoder7bit : AnalogDecoder
    {
        public AnalogDecoder7bit() : base() { }
        public AnalogDecoder7bit(int _fieldSize, double _aFactor, double _bFactor)
            : base(_fieldSize, _aFactor, _bFactor) { }
        public AnalogDecoder7bit(string _context, int _fieldSize, double _aFactor, double _bFactor)
            : base(_context, _fieldSize, _aFactor, _bFactor) { }


        protected override void SimpleDecode(DataBuffer buffer, DBField field)
        {
            if (!(field is DBAnalogField))
            {
                throw new Exception("the field is not a DBAnalogField");
            }

            //int data = (int)get7bitDataFromBuffer(buffer, fieldSize);
            int data = (int)buffer.GetBits_7bit(fieldSize);
            field.RowData = data;
            field.RowDataString = data.ToString();
            (field as DBAnalogField).Value = data * aFactor + bFactor;
        }
    }

    public class StringDecoder : Decoder
    {
        public StringDecoder() : base() { }
        public StringDecoder(int _maxLength)
            : base()
        {
            maxLength = _maxLength;
        }

        public StringDecoder(string _context, int _maxLength)
            : base()
        {
            context = _context;
            maxLength = _maxLength;
        }
        
        public StringDecoder(string _context)
            : base()
        {
            context = _context;
        }
        
        private int maxLength = int.MaxValue;

        protected override void SimpleDecode(DataBuffer buffer, DBField field)
        {
            if (!(field is DBStringField))
            {
                throw new Exception("the field is not a DBStringField");
            }

            string value = "";
            string row = "";
            for (int i = 0; i < maxLength; i++)
            {
                if (buffer.RemainingBytes == 0)
                    break;
                
                int ch = buffer.getByte();

                if (ch != 0)
                {
                    value += Convert.ToChar(ch);
                    row += ch.ToString() + " ";
                }
                else
                    break;
            }

            field.RowDataString = row;
            (field as DBStringField).Value = value;
        }
    }

}
