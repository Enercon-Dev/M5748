using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MPS_Tools
{
    public class TelContainer : Panel
    {
        private CommDB db = null;
        public CommDB DB
        {
            get { return db; }
            set
            {
                if (initalized)
                {
                    throw new Exception("\"DB\" may not be set after Initialize() is called");
                }
                db = value;
            }
        }

        private string[] parameters = { };
        public string[] Parameters
        {
            get { return parameters; }
            set
            {
                if (initalized)
                {
                    throw new Exception("\"Parameters\" may not be set after Initialize() is called");
                }
                parameters = value;
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

        private void substituteParameters()
        {
            foreach (string s in parameters)
            {
                string[] split = s.Split(' ');
                if (split.Length != 2)
                    throw new Exception("\"Parameters\" must contain 2 string seperated by single space");
                string index = split[0];
                string value = split[1];

                foreach (Control c in this.Controls)
                {
                    if (c is IDBClient)
                    {
                        for (int i = 0; i < (c as IDBClient).DBFields.Length; i++)
                        {
                            (c as IDBClient).DBFields[i] = (c as IDBClient).DBFields[i].Replace(index, value);
                        }
                    }
                }
            }
        }

        private bool initalized = false;
        public void Initialize()
        {
            //this method should be executed after control is initialized (includin automaticaly generated code by visual studio)
            // i.e. this function should be executed after InitializeComponent(); function
            // currently I do not know any automaic way to do it,

            if (initalized)
                return;
            substituteParameters();
            SetComponentsDB();

        }
    }
}
