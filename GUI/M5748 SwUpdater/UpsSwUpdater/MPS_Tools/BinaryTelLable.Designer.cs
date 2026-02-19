namespace MPS_Tools
{
    partial class BinaryTelLable
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.AlarmOffMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AlarmSingleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AlarmNormalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AlarmCriticalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.DirectionStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.OnSetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OnClearMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OnTogleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.blinkTimer = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AlarmOffMenuItem,
            this.AlarmSingleMenuItem,
            this.AlarmNormalMenuItem,
            this.AlarmCriticalMenuItem,
            this.DirectionStripSeparator,
            this.OnSetMenuItem,
            this.OnClearMenuItem,
            this.OnTogleMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(138, 164);
            this.contextMenuStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip_ItemClicked);
            // 
            // AlarmOffMenuItem
            // 
            this.AlarmOffMenuItem.Name = "AlarmOffMenuItem";
            this.AlarmOffMenuItem.Size = new System.Drawing.Size(137, 22);
            this.AlarmOffMenuItem.Text = "Alarm Off";
            // 
            // AlarmSingleMenuItem
            // 
            this.AlarmSingleMenuItem.Name = "AlarmSingleMenuItem";
            this.AlarmSingleMenuItem.Size = new System.Drawing.Size(137, 22);
            this.AlarmSingleMenuItem.Text = "Alarm Single";
            // 
            // AlarmNormalMenuItem
            // 
            this.AlarmNormalMenuItem.Name = "AlarmNormalMenuItem";
            this.AlarmNormalMenuItem.Size = new System.Drawing.Size(137, 22);
            this.AlarmNormalMenuItem.Text = "Alarm Normal";
            // 
            // AlarmCriticalMenuItem
            // 
            this.AlarmCriticalMenuItem.Name = "AlarmCriticalMenuItem";
            this.AlarmCriticalMenuItem.Size = new System.Drawing.Size(137, 22);
            this.AlarmCriticalMenuItem.Text = "Alarm Critical";
            // 
            // DirectionStripSeparator
            // 
            this.DirectionStripSeparator.Name = "DirectionStripSeparator";
            this.DirectionStripSeparator.Size = new System.Drawing.Size(134, 6);
            // 
            // OnSetMenuItem
            // 
            this.OnSetMenuItem.Name = "OnSetMenuItem";
            this.OnSetMenuItem.Size = new System.Drawing.Size(137, 22);
            this.OnSetMenuItem.Text = "On Set";
            // 
            // OnClearMenuItem
            // 
            this.OnClearMenuItem.Name = "OnClearMenuItem";
            this.OnClearMenuItem.Size = new System.Drawing.Size(137, 22);
            this.OnClearMenuItem.Text = "On Clear";
            // 
            // OnTogleMenuItem
            // 
            this.OnTogleMenuItem.Name = "OnTogleMenuItem";
            this.OnTogleMenuItem.Size = new System.Drawing.Size(137, 22);
            this.OnTogleMenuItem.Text = "On Togle";
            // 
            // blinkTimer
            // 
            this.blinkTimer.Interval = 50;
            this.blinkTimer.Tick += new System.EventHandler(this.blinkTimer_Tick);
            // 
            // BinaryTelLable
            // 
            this.ContextMenuStrip = this.contextMenuStrip;
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem AlarmOffMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AlarmSingleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AlarmNormalMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AlarmCriticalMenuItem;
        private System.Windows.Forms.ToolStripSeparator DirectionStripSeparator;
        private System.Windows.Forms.ToolStripMenuItem OnSetMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OnClearMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OnTogleMenuItem;
        private System.Windows.Forms.Timer blinkTimer;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
