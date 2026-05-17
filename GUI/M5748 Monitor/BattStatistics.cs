using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MPS_Tools;

namespace M5521_Monitor
{
    public partial class BattStatistics : UserControl
    {
        public BattStatistics()
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
    }
}
