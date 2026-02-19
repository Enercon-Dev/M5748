using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MPS_Tools
{
    public partial class AnalogTelLable : Label, IDBClient, IDBComponent
    {
        public AnalogTelLable()
        {
            InitializeComponent();
        }

        private string text_prefix = "";
        [DefaultValue("")]
        public string TextPrefix
        {
            get { return text_prefix; }
            set
            {
                text_prefix = value;
            }
        }

        private string numberFormat = "";
        [DefaultValue("")]
        public string NumberFormat
        {
            get { return numberFormat; }
            set
            {
                numberFormat = value;
            }
        }

        ////////////////////////////////////////////////////////////////
        /////////////////// DB /////////////////////////////////////////
        ////////////////////////////////////////////////////////////////

        private CommDB db;
        public CommDB DB
        {
            set
            {
                db = value;
                if (db != null)
                    db.RegisterClient(this);
            }
            get { return db; }
        }


        private string[] dbFields = { };
        public string[] DBFields
        {
            get { return dbFields; }
            set
            {
                dbFields = value;
            }
        }


        public DBField DefaultDBField
        {
            get
            {
                if (db != null && dbFields.Length > 0 && dbFields[0] != null)
                    return db[dbFields[0]];
                else
                    return null;
            }
        }


        public void DBDataUpdated(DBField[] fields)
        {
            if (DefaultDBField != null)
            {
                SetToField(DefaultDBField);
            }
            OnDataUpdated();
            blink();
        }

        public void SetToField(DBField field)
        {
            if (field is DBAnalogField)
            {
                Text = text_prefix + field.GetAnalogValue().ToString(numberFormat) +
                    ((DBAnalogField)field).Units;
            }
            else if (field is DBRawIntField)
            {
                Text = text_prefix + field.GetIntValue().ToString();
            }
            else
            {
                Text = text_prefix + field.ValueToString();
            }
            SetToolTip(field);
        }

        public void SetToolTip(DBField field)
        {
            SetToolTip(field.UpdateTime.ToLongTimeString() + " \n" +
                       field.RowDataString);
        }

        public void SetToolTip(string tip)
        {
            toolTip.SetToolTip(this, tip);
        }

        public event EventHandler DataUpdated;
        protected void OnDataUpdated()
        {
            if (DataUpdated != null)
                DataUpdated(this, new EventArgs());
        }


        public static bool blinkEnable = true;
        
        private Color c1 = Color.FromArgb(230, 255, 255);
        private Color c2 = Color.FromArgb(230, 255, 255);
        private Color c3 = Color.FromArgb(230, 255, 255);

        private Color savedBackColor;
        private int blinkCounter = 0;
        private void blink()
        {
            if (!blinkEnable)
                return;
            
            if (!blinkTimer.Enabled)
            {
                blinkCounter = 0;
                savedBackColor = BackColor;
                BackColor = c1;
                blinkTimer.Enabled = true;
            }
            else
            {
                //timer already runing - restart it
                blinkTimer.Enabled = false;
                blinkTimer.Enabled = true;
            }
        }

        private void blinkTimer_Tick(object sender, EventArgs e)
        {
            blinkCounter++;
            switch (blinkCounter)
            {
                case 1:
                    BackColor = c2;
                    break;
                case 2:
                    BackColor = c3;
                    break;
                case 3:
                    BackColor = c2;
                    break;
                case 4:
                    BackColor = c1;
                    break;
                default:
                    blinkTimer.Enabled = false;
                    BackColor = savedBackColor;
                    break;

            }
        }



    }
}
