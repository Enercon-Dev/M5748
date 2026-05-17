using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MPS_Tools;

namespace M1787_Monitor
{
    public partial class UserBatt : UserControl
    {
        public UserBatt()
        {
            InitializeComponent();
        }

        private int battNum = -1;
        public int BattNum
        {
            get { return battNum; }
            set
            {
                if (battNum != -1)
                    throw new Exception("\"BattNum\" field may be set only once");

                if (value >= 0)
                {
                    battNum = value;
                    battLabel.Text = "Batt " + (battNum + 1);
                    ReplaceBattParameter();
                }
            }
        }

        private CommDB db = null;
        public CommDB DB
        {
            get { return db; }
            set
            {
                if (db == value)
                    return;

                if (db == null)
                {
                    db = value;
                    SetComponentsDB();
                }
                else
                {
                    throw new Exception("\"DB\" field may be set only once");
                }
            }
        }


        private void SetComponentsDB()
        {
            foreach (Control c in this.Controls)
            {
                if (c is IDBComponent)
                {
                    (c as IDBComponent).DB = db;
                }
            }
        }

        private void ReplaceBattParameter()
        {
            foreach (Control c in this.Controls)
            {
                if (c is IDBClient)
                {
                    for (int i = 0; i < (c as IDBClient).DBFields.Length; i++)
                    {
                        (c as IDBClient).DBFields[i] = (c as IDBClient).DBFields[i].Replace("%1", battNum.ToString());
                    }
                }
            }
        }

        private void analogTelLable25_DataUpdated(object sender, EventArgs e)
        {
        }

        private void StatusLable_DataUpdated(object sender, EventArgs e)
        {
            switch (db[StatusLable.DBFields[1]].GetIntValue())
            {
                case 0: StatusLable.Text = "Error (ch)"; return;
                case 1: StatusLable.Text = "Charging"; return;
                case 2: StatusLable.Text = "Waiting (ch)"; return;
                case 3: StatusLable.Text = "ch. paused"; return;
                case 4: StatusLable.Text = "Bad (ch)"; return;
                case 5: StatusLable.Text = "Full (ch)"; return;
                case 6: StatusLable.Text = "No Batt"; return;
                case 7: StatusLable.Text = "Failed Charger"; return;

                case 8: StatusLable.Text = "Error (dsch)"; return;
                case 9: StatusLable.Text = "Discharging"; return;
                case 10: StatusLable.Text = "Waiting (dsch)"; return;
                case 12: StatusLable.Text = "Empty/Bad (dsch)"; return;
                case 13: StatusLable.Text = "Full (dsch)"; return;
                case 15: StatusLable.Text = "Dsch. Fail"; return;
                default: StatusLable.Text = "--"; return;
            }
        }

    }
}
