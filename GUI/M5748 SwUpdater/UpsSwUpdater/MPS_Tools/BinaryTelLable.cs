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
    public partial class BinaryTelLable : Label, IDBClient, IDBComponent
    {
        public BinaryTelLable()
        {
            alartType = AlartType_t.AlertOff;
            alartType = AlartType_t.AlertOff;
            InitializeComponent();

            UpdateContextMenuStripContent();
        }

        private Color defaultForeColor;
        private bool defaultForeColorSet = false;
        private bool state = false;
        [DefaultValue(false)]
        public bool State
        {
            get {return state;}
            set 
            {
                state = value;
                Text = state ? textSet : textClear;

                if (this.DesignMode) //do not change color in design mode
                    return;
                if (!defaultForeColorSet)
                {
                    defaultForeColorSet = true;
                    defaultForeColor = ForeColor;
                }
                if ((shadeMode == Shade.Both) ||
                    (state && shadeMode == Shade.Set) ||
                    (!state && shadeMode == Shade.Clear))
                    ForeColor = Color.FromKnownColor(System.Drawing.KnownColor.GrayText);
                else
                    ForeColor = defaultForeColor;
            }
        }

        private string textSet = "Set";
        [DefaultValue("Set")]
        public string TextSet
        {
            get { return textSet; }
            set 
            { 
                textSet = value;
                State = state; //update the display
            }
        }

        private string textClear = "Clear";
        [DefaultValue("Clear")]
        public string TextClear
        {
            get { return textClear; }
            set 
            {
                textClear = value;
                State = state; //update the display
            }
        }
        /*
        private bool enableShade = true;
        [DefaultValue(true)]
        public bool EnableShade
        {
            get { return enableShade; }
            set
            {
                enableShade = value;
                State = state; //update the display
            }
        }*/

        private Shade shadeMode = Shade.Clear;
        [DefaultValue(Shade.Clear)]
        public Shade ShadeMode
        {
            get { return shadeMode; }
            set
            {
                shadeMode = value;
                State = state; //update the display
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
            State = field.GetFlagValue();
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

        private  Color c1 = Color.FromArgb(230, 255, 255);
        private  Color c2 = Color.FromArgb(230, 255, 255);
        private  Color c3 = Color.FromArgb(230, 255, 255);

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
                default :
                    blinkTimer.Enabled = false;
                    BackColor = savedBackColor;
                    break;

            }
        }

        public enum Shade { Set, Clear, Both, Non };

        ////////////////////////////////////////////////////////////////
        /////////////////// Alerts /////////////////////////////////////
        ////////////////////////////////////////////////////////////////
        private bool alartAssociated = false;
        [DefaultValue(false)]
        public bool AlartAssociated
        {
            get { return alartAssociated; }
            set
            {
                alartAssociated = value;
                UpdateContextMenuStripContent();
            }
        }

        private AlartType_t alartType = AlartType_t.AlertOff;
        [DefaultValue(AlartType_t.AlertOff)]
        public AlartType_t AlartType
        {
            get { return alartType; }
            set
            {
                alartType = value;
                UpdateContextMenuStrip();
            }
        }

        private AlertDirection_t alertDirection;
        [DefaultValue(AlertDirection_t.AlertSet)]
        public AlertDirection_t AlertDirection
        {
            get { return alertDirection; }
            set
            {
                alertDirection = value;
                UpdateContextMenuStrip();
            }
        }

        public void SetAlertStatus(AlartType_t type, AlertDirection_t direction)
        {
            AlartType = type;
            AlertDirection = direction;
        }



        private void UpdateContextMenuStripContent()
        {
            contextMenuStrip.Items["AlarmOffMenuItem"].Visible = alartAssociated;
            contextMenuStrip.Items["AlarmSingleMenuItem"].Visible = alartAssociated;
            contextMenuStrip.Items["AlarmNormalMenuItem"].Visible = alartAssociated;
            contextMenuStrip.Items["AlarmCriticalMenuItem"].Visible = alartAssociated;
            contextMenuStrip.Items["DirectionStripSeparator"].Visible = alartAssociated;
            contextMenuStrip.Items["OnSetMenuItem"].Visible = alartAssociated;
            contextMenuStrip.Items["OnClearMenuItem"].Visible = alartAssociated;
            contextMenuStrip.Items["OnTogleMenuItem"].Visible = alartAssociated;
            UpdateContextMenuStrip();
        }


        private void UpdateContextMenuStrip()
        {
            ((ToolStripMenuItem)contextMenuStrip.Items["AlarmOffMenuItem"]).Checked = 
                (alartType == AlartType_t.AlertOff);
            ((ToolStripMenuItem)contextMenuStrip.Items["AlarmSingleMenuItem"]).Checked =
                (alartType == AlartType_t.AlertSingle);
            ((ToolStripMenuItem)contextMenuStrip.Items["AlarmNormalMenuItem"]).Checked =
                (alartType == AlartType_t.AlertNormal);
            ((ToolStripMenuItem)contextMenuStrip.Items["AlarmCriticalMenuItem"]).Checked =
                (alartType == AlartType_t.AlertCritical);

            ((ToolStripMenuItem)contextMenuStrip.Items["OnSetMenuItem"]).Checked =
                (AlertDirection == AlertDirection_t.AlertSet);
            ((ToolStripMenuItem)contextMenuStrip.Items["OnClearMenuItem"]).Checked =
                (AlertDirection == AlertDirection_t.AlertClear);
            ((ToolStripMenuItem)contextMenuStrip.Items["OnTogleMenuItem"]).Checked =
                (AlertDirection == AlertDirection_t.AlertTogle);
        }

        private void contextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Name)
            {
                case "AlarmOffMenuItem":      AlartType = AlartType_t.AlertOff; break;
                case "AlarmSingleMenuItem":   AlartType = AlartType_t.AlertSingle; break;
                case "AlarmNormalMenuItem":   AlartType = AlartType_t.AlertNormal; break;
                case "AlarmCriticalMenuItem": AlartType = AlartType_t.AlertCritical; break;

                case "OnSetMenuItem":   AlertDirection = AlertDirection_t.AlertSet; break;
                case "OnClearMenuItem": AlertDirection = AlertDirection_t.AlertClear; break;
                case "OnTogleMenuItem": AlertDirection = AlertDirection_t.AlertTogle; break;
            }
        }


    }



    public enum AlartType_t { AlertOff = 0, AlertSingle = 1, AlertNormal = 2, AlertCritical = 3 };
    public enum AlertDirection_t { AlertSet = 0, AlertClear = 1, AlertTogle = 2 };
}
