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
    public partial class AnalogTextBox : TextBox, IDBClient
    {
        public AnalogTextBox()
        {
            InitializeComponent();
            Data = data; //update the display
        }

        private int data = 0;
        //[DefaultValue(0)]
        public int Data
        {

            get { return data; }
            set
            {
                data = value;
                displayData();
            }
        }

        private void displayData()
        {
            double value;
            DBAnalogField field = (DBAnalogField)DefaultDBField;

            if (field != null)
            {
                //value = data * field.AFactor + field.BFactor;
#warning "fix function"
            }
            else
            {
                value = data;
            }
            //Text = value.ToString("N1");
#warning "fix function"
        }

        private void ProcesEnterdValue()
        {
            double a=1, b=0;
            DBAnalogField field = (DBAnalogField)DefaultDBField;

            if (field != null)
            {
                //a = field.AFactor;
                //b = field.BFactor;
#warning "fix function"
            }

            try { Data = Convert.ToInt32((Convert.ToDouble(this.Text) - b) / a); }
            catch { Data = data; }//set the display back to prev. value
        }
        
        protected override void OnLeave(EventArgs e)
        {
            ProcesEnterdValue();
            base.OnLeave(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                ProcesEnterdValue();
            else if (e.KeyCode == Keys.Escape)
                Data = data;//set the display back to prev. value

            base.OnKeyDown(e);
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
            blink();
        }

        public void SetToField(DBField field)
        {
            Data = field.RowData;
            toolTip.SetToolTip(this,
                field.UpdateTime.ToLongTimeString() + " \n" +
                //field.RowData.ToString());
                field.RowDataString);
        }

        private Color savedBackColor;
        private int blinkCounter = 0;
        private void blink()
        {
            if (!blinkTimer.Enabled)
            {
                blinkCounter = 0;
                savedBackColor = BackColor;
                BackColor = Color.FromArgb(192, 255, 255);
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
                    BackColor = Color.FromArgb(128, 255, 255);
                    break;
                case 2:
                    BackColor = Color.FromArgb(0, 255, 255);
                    break;
                case 3:
                    BackColor = Color.FromArgb(128, 255, 255);
                    break;
                case 4:
                    BackColor = Color.FromArgb(192, 255, 255);
                    break;
                default:
                    blinkTimer.Enabled = false;
                    BackColor = savedBackColor;
                    break;

            }
        }

    }
}
