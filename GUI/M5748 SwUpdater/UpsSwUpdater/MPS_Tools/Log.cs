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
    public enum LogedStatus { LsCommand, LsTelemetry, LsTelemetryError, LsError, LsMessage, LsNon };
    
    public partial class Log : UserControl
    {
        private bool EnableSelection;
        public Log()
        {
            InitializeComponent();
            EnableSelection = false;
        }

        protected override void OnResize(EventArgs e)
        {
            LogDataGrid.Height = this.Height - LogDataGrid.Top * 2;
            LogDataGrid.Width = this.Width - LogDataGrid.Left * 2;;

            base.OnResize(e);
        }

        private void ClearSelection()
        {
            if (LogDataGrid.SelectedCells.Count > 0)
            {
                LogDataGrid.SelectedCells[0].Selected = false;
            }
        }
        
        private void LogDataGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (!EnableSelection) ClearSelection();
        }


        private void clearLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogDataGrid.RowCount = 0;
        }

        private void copyLogToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableSelection = true;
            try
            {
                LogDataGrid.SelectAll();
                Clipboard.SetDataObject(LogDataGrid.GetClipboardContent());
            }
            catch { }

            EnableSelection = false;
            ClearSelection();
            LogDataGrid.FirstDisplayedCell = LogDataGrid[0, LogDataGrid.Rows.Count - 1];
        }


        public static string BufferToHex(byte[] buffer, string delaminator)
        {
            string DataStr = "";
            
            for (int i = 0; i < buffer.Length; i++)
            {
                DataStr += buffer[i].ToString("X2") + delaminator;
                //if (i == 3 || i == buffer.Length - 3) DataStr += "   ";
            }

            return DataStr;
        }

        public static string BufferToHex(byte[] buffer)
        {
            return BufferToHex(buffer, " ");
        }
        

        public void Add(byte[] buffer, string description, LogedStatus stat)
        {
            Add(BufferToHex(buffer), description, stat);
        }

        public void Add(string buffer, string description, LogedStatus stat)
        {
            string[] row = { DateTime.Now.ToString(), description, buffer};
            LogDataGrid.Rows.Add(row);

            Color ststColor;
            switch (stat)
            {
                case LogedStatus.LsCommand: ststColor = Color.Green/*Color.Yellow*/; break;
                case LogedStatus.LsTelemetry: ststColor = Color.Green; break;
                case LogedStatus.LsTelemetryError:
                case LogedStatus.LsError: ststColor = Color.Red; break;
                default: ststColor = Color.White; break;
            }

            LogDataGrid[0, LogDataGrid.RowCount - 1].Style.BackColor = ststColor;

            LogDataGrid.FirstDisplayedCell = LogDataGrid[0, LogDataGrid.Rows.Count - 1];
        }

        public void Add(IFrame frame, LogedStatus stat)
        {
            Add(frame.ToString(), frame.Description, stat);
        }

        public void Add(IFrame frame)
        {
            LogedStatus status;
            if (frame.FrameType == MPS_FrameType.Command)
                status = LogedStatus.LsCommand;
            else if (frame.FrameType == MPS_FrameType.Telemetry)
                status = LogedStatus.LsTelemetry;
            else
                status = LogedStatus.LsNon;
            
            Add(frame.ToString(), frame.Description, status);
        }

        private void Log_Load(object sender, EventArgs e)
        {

        }


    }
}
