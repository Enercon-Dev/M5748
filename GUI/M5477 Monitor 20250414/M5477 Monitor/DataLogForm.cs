using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MPS_Tools;
using System.IO;

namespace MPS_Tools
{
    public partial class DataLogForm : Form, IDBClient
    {
        private CommDB db;
        private bool EnableClosing = false;

        public DataLogForm(CommDB db)
        {
            InitializeComponent();
            this.db = db;
        }

        private void DataLogForm_Load(object sender, EventArgs e)
        {
            if (db != null)
                db.RegisterClient(this);

            LoadFields();
            LoadSettings();
        }

        private void DataLogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (EnableClosing) return;
            if (e.CloseReason != CloseReason.UserClosing) return;
            this.Hide();
            e.Cancel = true;
        }

        private void DataLogForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseDataFile();
            SaveSettings();
        }

        public void CloseForm()
        {
            EnableClosing = true;
            this.Close();
        }

        private void LoadSettings()
        {
            //initialize XML (properties) file:
            SimpleXML XMLdoc = new SimpleXML("DataLog.xml");
            if (!XMLdoc.LoadedFromFile)
            {
                MessageBox.Show("Can't open \"DataLog.xml\" file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            string saveList = XMLdoc.ReadString("/SaveFields");

            foreach (string s in saveList.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                TreeNode foundNode = findTreeNode(treeView.Nodes, s);
                if (foundNode == null)
                {
                    MessageBox.Show("Fiels \"" + s + "\" not exist");
                    continue;
                }
                foundNode.Checked = true;
            }

            string triggerList = XMLdoc.ReadString("/TriggerFields");
            foreach (string s in triggerList.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                TreeNode foundNode = findTreeNode(treeView.Nodes, s);
                if (foundNode == null)
                {
                    MessageBox.Show("Fiels \"" + s + "\" not exist");
                    continue;
                }
                foundNode.BackColor = System.Drawing.Color.Red;
            }
        }


        private void SaveSettings()
        {
            //initialize XML (properties) file:
            SimpleXML XMLdoc = new SimpleXML("DataLog.xml");
            if (!XMLdoc.LoadedFromFile)
            {
                //MessageBox.Show("Can't open \"DataLog.xml\" file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            updateFieldsLists();
            string saveList = "";
            foreach (string s in saveFieldsList)
                saveList += s + " ";

            XMLdoc.WriteString("/SaveFields", saveList);


            string triggerList = "";
            foreach (string s in triggerFieldsList)
                triggerList += s + " ";

            XMLdoc.WriteString("/TriggerFields", triggerList);
            XMLdoc.SaveToFile();
        }

        ///////////// DB //////////////////
        private string[] dbFields = { };
        public string[] DBFields
        {
            get { return dbFields; }
            set
            {
                dbFields = value;
            }
        }

        public void DBDataUpdated(DBField[] fields)
        {
            SaveCurrentValues();
        }



        private void LoadFields()
        {
            for (int i=0; i< db.Fields.Count; i++)
            {
                string[] fieldName = db.Fields[i].Name.Split('.');
                TreeNodeCollection nodes = treeView.Nodes;
                for(int j=0; j<fieldName.Length; j++)
                {
                    if (nodes[fieldName[j]] == null)
                    {
                        nodes.Add(fieldName[j], fieldName[j]);
                    }
                    TreeNode node = nodes[fieldName[j]];
                    nodes = node.Nodes;
                }
            }
        }

        // Updates all child tree nodes recursively. 
        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;
                if (node.Nodes.Count > 0)
                {
                    // If the current node has child nodes, call the CheckAllChildsNodes method recursively. 
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        // NOTE   This code can be added to the BeforeCheck event handler instead of the AfterCheck event. 
        // After a tree node's Checked property is changed, all its child nodes are updated to the same value. 
        private void node_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // The code only executes if the user caused the checked state to change. 
            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.Nodes.Count > 0)
                {
                    /* Calls the CheckAllChildNodes method, passing in the current 
                    Checked value of the TreeNode whose checked state changed. */
                    this.CheckAllChildNodes(e.Node, e.Node.Checked);
                }
            }
        }



        TextWriter tw;
        int UpdateTimeout = 0;
        private void openFileButton_Click(object sender, EventArgs e)
        {
            updateFieldsLists();
            try { UpdateTimeout = int.Parse(updateTimeoutTextBox.Text); }
            catch { MessageBox.Show("Update Timeout must be a number"); }
            OpenDataFile();

            dbFields = triggerFieldsList.ToArray();
            
        }
        
        private void OpenDataFile()
        {
            try
            {
                tw = new StreamWriter(fileNameTextBox.Text);
            }
            catch { MessageBox.Show("cannot Open file"); }


            tw.Write("Time");

            foreach(string s in saveFieldsList)
                tw.Write("\t" + s);

            tw.Write(System.Environment.NewLine);
        }

        private void CloseDataFile()
        {
            if (tw != null)
            {
                tw.Close();
                tw = null;
            }
        }

        public void SaveCurrentValues()
        {
            if (tw == null)
                return;
            
            tw.Write(DateTime.Now.ToString());

            DateTime now = DateTime.Now;

            foreach (string s in saveFieldsList)
            {
                if ((now - db[s].UpdateTime).TotalSeconds <= UpdateTimeout)
                    tw.Write("\t" + db[s].ValueToShortString());
                else
                    tw.Write("\t-");
            }

            tw.Write(System.Environment.NewLine);
            tw.Flush();
        }




        private void closeFileButton_Click(object sender, EventArgs e)
        {
            CloseDataFile();
        }


        private void treeView_DoubleClick(object sender, EventArgs e)
        {
            if (treeView.SelectedNode == null)
                return;

            if (treeView.SelectedNode.BackColor != System.Drawing.Color.Red)
                treeView.SelectedNode.BackColor = System.Drawing.Color.Red;
            else
                treeView.SelectedNode.BackColor = System.Drawing.Color.Transparent;
        }


        List<string> saveFieldsList = new List<string>();
        List<string> triggerFieldsList = new List<string>();        
        private void updateFieldsLists()
        {
            addFieldsToLists(treeView.Nodes, saveFieldsList, triggerFieldsList);
        }

        private void addFieldsToLists(TreeNodeCollection nodes, List<string> saveFieldsList, List<string> triggerFieldsList)
        {
            foreach (TreeNode n in nodes)
            {
                if (n.Nodes.Count > 0)
                    addFieldsToLists(n.Nodes, saveFieldsList, triggerFieldsList);
                else
                {
                    if (n.Checked)
                        saveFieldsList.Add(n.FullPath);
                    if (n.BackColor == System.Drawing.Color.Red)
                        triggerFieldsList.Add(n.FullPath);
                }
            }
        }


        private TreeNode findTreeNode(TreeNodeCollection nodes, string nodeFullName)
        {
            TreeNode foundNode;
            foreach (TreeNode n in nodes)
            {
                if (n.FullPath == nodeFullName)
                    return n;
                if (n.Nodes.Count > 0)
                {
                    foundNode = findTreeNode(n.Nodes, nodeFullName);
                    if (foundNode != null)
                        return foundNode;
                }
            }
            return null;
        }

        private void updateDataButton_Click(object sender, EventArgs e)
        {
            updateNodesData(treeView.Nodes);
        }

        private void updateNodesData(TreeNodeCollection nodes)
        {
            foreach (TreeNode n in nodes)
            {
                if (n.Nodes.Count == 0)
                {
                    n.ToolTipText =
                    db[n.FullPath].UpdateTime.ToString() + "\n" +
                    db[n.FullPath].GetType().ToString() + "\n" +
                    db[n.FullPath].ValueToString();
                }
                else
                    updateNodesData(n.Nodes);
            }

        }
       







    }
}
