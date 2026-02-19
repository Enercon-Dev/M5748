namespace MPS_Tools
{
    partial class Log
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
            this.LogDataGrid = new System.Windows.Forms.DataGridView();
            this.LogMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.clearLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyLogToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TimeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DataColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FillColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.LogDataGrid)).BeginInit();
            this.LogMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // LogDataGrid
            // 
            this.LogDataGrid.AllowUserToAddRows = false;
            this.LogDataGrid.AllowUserToResizeRows = false;
            this.LogDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.LogDataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.TimeColumn,
            this.DescriptionColumn,
            this.DataColumn,
            this.FillColumn});
            this.LogDataGrid.ContextMenuStrip = this.LogMenuStrip;
            this.LogDataGrid.Location = new System.Drawing.Point(3, 3);
            this.LogDataGrid.Name = "LogDataGrid";
            this.LogDataGrid.RowHeadersVisible = false;
            this.LogDataGrid.Size = new System.Drawing.Size(586, 198);
            this.LogDataGrid.TabIndex = 4;
            this.LogDataGrid.SelectionChanged += new System.EventHandler(this.LogDataGrid_SelectionChanged);
            // 
            // LogMenuStrip
            // 
            this.LogMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearLogToolStripMenuItem,
            this.copyLogToClipboardToolStripMenuItem});
            this.LogMenuStrip.Name = "LogMenuStrip";
            this.LogMenuStrip.Size = new System.Drawing.Size(183, 48);
            // 
            // clearLogToolStripMenuItem
            // 
            this.clearLogToolStripMenuItem.Name = "clearLogToolStripMenuItem";
            this.clearLogToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.clearLogToolStripMenuItem.Text = "Clear Log";
            this.clearLogToolStripMenuItem.Click += new System.EventHandler(this.clearLogToolStripMenuItem_Click);
            // 
            // copyLogToClipboardToolStripMenuItem
            // 
            this.copyLogToClipboardToolStripMenuItem.Name = "copyLogToClipboardToolStripMenuItem";
            this.copyLogToClipboardToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.copyLogToClipboardToolStripMenuItem.Text = "Copy Log To Clipboard";
            this.copyLogToClipboardToolStripMenuItem.Click += new System.EventHandler(this.copyLogToClipboardToolStripMenuItem_Click);
            // 
            // TimeColumn
            // 
            this.TimeColumn.HeaderText = "Time";
            this.TimeColumn.Name = "TimeColumn";
            this.TimeColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.TimeColumn.Width = 130;
            // 
            // DescriptionColumn
            // 
            this.DescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.DescriptionColumn.HeaderText = "Description";
            this.DescriptionColumn.Name = "DescriptionColumn";
            this.DescriptionColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.DescriptionColumn.Width = 66;
            // 
            // DataColumn
            // 
            this.DataColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.DataColumn.HeaderText = "Data";
            this.DataColumn.MinimumWidth = 300;
            this.DataColumn.Name = "DataColumn";
            this.DataColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.DataColumn.Width = 300;
            // 
            // FillColumn
            // 
            this.FillColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.FillColumn.HeaderText = "";
            this.FillColumn.MinimumWidth = 2;
            this.FillColumn.Name = "FillColumn";
            this.FillColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Log
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.LogDataGrid);
            this.Name = "Log";
            this.Size = new System.Drawing.Size(592, 204);
            this.Load += new System.EventHandler(this.Log_Load);
            ((System.ComponentModel.ISupportInitialize)(this.LogDataGrid)).EndInit();
            this.LogMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip LogMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem clearLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyLogToClipboardToolStripMenuItem;
        private System.Windows.Forms.DataGridView LogDataGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn DescriptionColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn DataColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn FillColumn;
    }
}
