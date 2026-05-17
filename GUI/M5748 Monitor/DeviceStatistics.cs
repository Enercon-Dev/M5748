using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MPS_Tools;

namespace M5748_Monitor
{
    public partial class DeviceStatistics : UserControl
    {
        public DeviceStatistics()
        {
            InitializeComponent();
        }

        private string dbFieldsGroup = "";
        public string DbFieldsGroup
        {
            get { return dbFieldsGroup; }
            set
            {
                if (dbFieldsGroup != "")
                    throw new Exception("\"DbFieldsGroup\" field may be set only once");

                dbFieldsGroup = value;
                if (value.Length > 0)
                {
                    ReplaceDevParameter();
                }
            }
        }

        public string Header
        {
            get { return groupBox.Text; }
            set { groupBox.Text = value; }
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
            foreach (Control c in groupBox.Controls)
            {
                if (c is IDBComponent)
                {
                    (c as IDBComponent).DB = db;
                }
            }
        }

        private void ReplaceDevParameter()
        {
            foreach (Control c in groupBox.Controls)
            {
                if (c is IDBClient)
                {
                    for (int i = 0; i < (c as IDBClient).DBFields.Length; i++)
                    {
                        (c as IDBClient).DBFields[i] = dbFieldsGroup + "." + (c as IDBClient).DBFields[i];
                    }
                }
            }
        }


    }
}
