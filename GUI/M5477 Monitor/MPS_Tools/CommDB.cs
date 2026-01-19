using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace MPS_Tools
{
    public partial class CommDB : Component
    {
        public CommDB()
        {
            InitializeComponent();

            fields = new List<DBField>();
            clients = new List<IDBClient>();

            //OnDataUpdated();
        }

        //public event EventHandler DBInit;
        //protected void OnDataUpdated()
        //{
        //    if (DBInit != null)
        //        DBInit(this, new EventArgs());
        //}

        private List<DBField> fields;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
//        [Editor(typeof(CustomCollectionEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public List<DBField> Fields
        {
            get { return fields; }
        }

        public void AddField(DBField newField)
        {
            fields.Add(newField);
        }

        /*
         * thouse function was never used 
         * and there is no reason to use them in the future
        public void SetField(string name, bool value)
        {
            DBFlagField field = (DBFlagField)this[name];
            field.Value = value;
        }

        public void SetField(string name, int value)
        {
            this[name].RowData = value;
        }

        public void SetField(string name, string value)
        {
            DBStringField field = (DBStringField)this[name];
            field.Value = value;
        }*/


        private bool AnyDataUpdated()
        {
            bool updated = false;
            foreach (DBField f in fields)
            {
                if (f.Updated)
                {
                    updated = true;
                    break;
                }
            }
            return updated;
        }

        private DBField[] GetUpdatadFields(string[] names)
        {
            List<DBField> updatedFields = new List<DBField> {};
            foreach (DBField f in fields)
            {
                if (f.Updated && names.Contains(f.Name))
                {
                    updatedFields.Add(f);
                }
            }

            return updatedFields.ToArray();
        }

        private void ClearUpdated()
        {
            foreach (DBField f in fields)
            {
                f.Updated = false;
            }
        }

        public void UpdateClients()
        {
            if (!AnyDataUpdated()) return;
            
            foreach (IDBClient c in clients)
            {
                if (c.DBFields == null || c.DBFields.Length == 0)
                    c.DBDataUpdated(null);
                else
                {
                    DBField[] updatadFields = GetUpdatadFields(c.DBFields);
                    if (updatadFields.Length != 0)
                        c.DBDataUpdated(updatadFields);
                }
            }

            ClearUpdated();
        }

        public DBField this[string fieldName]
        {
            get
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    if (fields[i].Name == fieldName)
                    {
                        return fields[i];
                    }
                }
                return null;
            }
        }
        

        private List<IDBClient> clients;
        public void RegisterClient(IDBClient client)
        {
            clients.Add(client);
        }
    }
/*
    public class CustomCollectionEditor : System.ComponentModel.Design.CollectionEditor
    {
        private Type[] types;
        public CustomCollectionEditor(Type type)
            : base(type)
        {
            types = new Type[] { typeof(DBFlagField), typeof(DBAnalogField), typeof(DBStringField) };
        }
        // Return the types that you want to allow the user to add into your collection.  
        protected override Type[] CreateNewItemTypes()
        {
            return types;
        }

        
        protected override System.ComponentModel.Design.CollectionEditor.CollectionForm CreateCollectionForm()
        {
            System.ComponentModel.Design.CollectionEditor.CollectionForm frm = base.CreateCollectionForm();
            //ask it "which one of your controls is a property grid?" 
            PropertyGrid grid = null;
            grid = FindPropertyGrid(frm);
            if (grid != null)
            {
                MessageBox.Show("found!!!");
                grid.BackColor = SystemColors.Control;
                grid.LineColor = SystemColors.Control;
                grid.ToolbarVisible = true;
                grid.HelpVisible = true;
                grid.Site = site;
                grid.PropertyTabs.AddTabType(typeof(System.Windows.Forms.Design.EventsTab), PropertyTabScope.Global);
                grid.Site = site;
            }
            return frm;
        }

        private PropertyGrid FindPropertyGrid(Control control)
        {
            PropertyGrid grid = control as PropertyGrid;
            foreach (Control child in control.Controls)
            {
                if (grid != null)
                {
                    break;
                }
                grid = FindPropertyGrid(child);
            }
            return grid;
        } 


        private ISite site;

        // and this method to retrieve a value for it 
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            MessageBox.Show("site updated");
            site = (context.Instance as IComponent).Site;
            return base.EditValue(context, provider, value);
        }

    }
*/
    //[ToolboxItem(false)]
    [PropertyTab(typeof(System.Windows.Forms.Design.EventsTab), PropertyTabScope.Component)]
    [DesignTimeVisibleAttribute(false)]
    [DefaultEvent("Decoding")]
    public abstract class DBField : Component
    {
        
        public DBField()
        {
            init();
        }

        public DBField(string _name, string _desc)
        {
            init();
            Name = _name;
            Description = _desc;
        }

        public DBField(string _name, string _desc, IDecoder _decoder)
        {
            init();
            Name = _name;
            Description = _desc;
            decoders.Add(_decoder);
        }

        public DBField(string _name, string _desc, IDecoder[] _decoders)
        {
            init();
            Name = _name;
            Description = _desc;

            for (int i = 0; i < _decoders.Length; i++)
            {
                decoders.Add(_decoders[i]);
            }
        }


        private void init()
        {
            decoders = new List<IDecoder>();
        }


        public string name;
        public string Name
        {
            get { return name; }
            set { name = value;}
        }

        public string Description;


//#warning "Dictionary is a temporery solution"
        //since change in context name inside the decoder will not change the context name in dictionary
        
        //protected Dictionary<string, IDecoder> decoders;
        protected List<IDecoder> decoders;
        public void AddDecoder(IDecoder decoder)
        {
            //decoders.Add(decoder.Context, decoder);
            decoders.Add(decoder);
        }

        public IDecoder GetDecoder(string context)
        {
            foreach (IDecoder d in decoders.ToArray())
            {
                if (d.IsContextSupported(context))
                    return d;
            }
            return null;
        }

        public virtual void Decode(DataBuffer buffer, string context)
        {
            if (OnDecoding(buffer))
                return;

            IDecoder d = GetDecoder(context);
            if (d == null)
                throw new Exception("context \"" + context + "\" isnt supported by this Decoder");

            try { d.Decode(context, buffer, this); }
            catch (Exception ex)
            {
                throw new Exception("Fail to decode \"" + this.name + "\" field", ex);
            }
        }

        public virtual void Decode(DataBuffer buffer)
        {
            Decode(buffer, "");
        }


        public delegate void DecodingHandler(object sender, DecodingArgs e);
        public event DecodingHandler Decoding;
        protected bool OnDecoding(DataBuffer buffer)
        {
            if (Decoding != null)
            {
                Decoding(this, new DecodingArgs(buffer));
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual void Fill(DataBuffer buffer)
        {
            throw new Exception("Can't fill data to buffer");
        }



        public virtual int RowData
        {
            get { return 0; }
            set { }
        }

        protected string rowDataString;
        public string RowDataString
        {
            get { return rowDataString; }
            set { rowDataString = value; }

        }


        private bool updated = false;
        public bool Updated
        {
            get { return updated; }
            set 
            {
                updated = value;
                if (updated)
                    updateTime = DateTime.Now;
            }
        }

        private DateTime updateTime;
        public DateTime UpdateTime
        {
            get { return updateTime; }
        }

        public virtual bool GetFlagValue()
        {
            throw new Exception("Data is not boolean");
        }

        public virtual string GetStringValue()
        {
            throw new Exception("Data is not string");
        }

        public virtual double GetAnalogValue()
        {
            throw new Exception("Data is not double");
        }

        public virtual int GetIntValue()
        {
            throw new Exception("Data is not integer");
        }

        public abstract string ValueToString();
        public abstract string ValueToShortString();

    }


    public class DecodingArgs : System.EventArgs
    {
        private DataBuffer buffer;
        public DecodingArgs(DataBuffer buf)
        {
            buffer = buf;
        }

        public DataBuffer Buffer
        {
            get { return buffer; }
        }
    }


    public class DBFlagField : DBField
    {
        public DBFlagField() : base() {}

        public DBFlagField(string _name, string _desc)
            : base(_name, _desc) {}

        public DBFlagField(string _name, string _desc, IDecoder _decoder)
            : base(_name, _desc, _decoder) { }

        public DBFlagField(string _name, string _desc, IDecoder[] _decoders)
            : base(_name, _desc, _decoders) { }


        private bool value = false;
        public bool Value
        {
            get { return value; }
            set
            {
                this.value = value;
                Updated = true;
            }
        }

        private int rowData = 0;
        public override int RowData
        {
            get { return rowData; }
            set 
            {
                rowData = value;
                Updated = true;
            }
        }

        public override bool GetFlagValue()
        {
            return value;
        }

        public override string ValueToString()
        {
            return value ? "set" : "clear";
        }

        public override string ValueToShortString()
        {
            return value ? "1" : "0";
        }

        public override void Fill(DataBuffer buffer)
        {
            buffer.fillBit(Value);
        }

    }


    public class DBAnalogField : DBField
    {
        
        private int rowData = 0;
        private double value = 0;
        private string units;

        public DBAnalogField() : base() {}

        public DBAnalogField(string _name, string _desc)
            : base(_name, _desc) {}

        public DBAnalogField(string _name, string _desc, IDecoder _decoder)
            : base(_name, _desc, _decoder) {}

        public DBAnalogField(string _name, string _desc, IDecoder[] _decoders)
            : base(_name, _desc, _decoders) { }

        public DBAnalogField(string _name, string _desc, string _units)
            : base(_name, _desc) 
        {
            units = _units;
        }

        public DBAnalogField(string _name, string _desc, IDecoder _decoder, string _units)
            : base(_name, _desc, _decoder)
        {
            units = _units;
        }

        public DBAnalogField(string _name, string _desc, IDecoder[] _decoders, string _units)
            : base(_name, _desc, _decoders)
        {
            units = _units;
        }

        public String Units
        {
            get { return units; }
            set { units = value; }
        }

        public double Value
        {
            get {return value;}
            set 
            {
                this.value = value;
                Updated = true;
            }
        }

        public override int RowData
        {
            get { return rowData; }
            set 
            {
                rowData = value;
                Updated = true;
            }
        }

        public override double GetAnalogValue()
        {
            return value;
        }
        
        public override string ValueToString()
        {
            return value.ToString() + units;
        }

        public override string ValueToShortString()
        {
            return value.ToString();
        }

        public override void Fill(DataBuffer buffer)
        {
            //buffer.fillByte(RowData);
            throw new Exception("Can't fill data to frame");
        }
    }


    public class DBStringField : DBField
    {
        private string value = "";

        public DBStringField() : base() {}

        public DBStringField(string _name, string _desc)
            : base(_name, _desc) {}

        public DBStringField(string _name, string _desc, IDecoder _decoder)
            : base(_name, _desc, _decoder) { }

        public string Value
        {
            get { return value; }
            set 
            { 
                this.value = value;
                Updated = true;
            }
        }

        public override string GetStringValue()
        {
            return value;
        }

        public override string ValueToString()
        {
            return value;
        }

        public override string ValueToShortString()
        {
            return value;
        }

        public override void Fill(DataBuffer buffer)
        {
            //buffer.fillString(Value, maxLength);
            throw new Exception("Can't fill data to frame");
        }
    }


    public class DBRawIntField : DBField
    {
        private int rowData = 0;
        //private int value = 0;
        
        public DBRawIntField() : base() {}

        public DBRawIntField(string _name, string _desc)
            : base(_name, _desc) {}

        public DBRawIntField(string _name, string _desc, IDecoder _decoder)
            : base(_name, _desc, _decoder) { }

        public DBRawIntField(string _name, string _desc, IDecoder[] _decoders)
            : base(_name, _desc, _decoders) { }


        public override int RowData
        {
            get { return rowData; }
            set
            {
                rowData = value;
                Updated = true;
            }
        }
        /*
        public int Value
        {
            get { return rowData; }
            set
            {
                this.rowData = value;
                Updated = true;
            }
        }*/

        public override int GetIntValue()
        {
            return rowData;
        }

        public override string ValueToString()
        {
            return rowData.ToString();
        }

        public override string ValueToShortString()
        {
            return rowData.ToString();
        }
    }


    public interface IDBClient
    {
        void DBDataUpdated(DBField[] fields);
        string[] DBFields { get; }
    }

    public interface IDBComponent
    {
        CommDB DB { get; set; }
        event EventHandler DataUpdated;
        DBField DefaultDBField { get; }
    }



 

 
 
 


}

