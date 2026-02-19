namespace M339_Monitor
{
    partial class Stopwatch
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
            this.TimerLabel = new System.Windows.Forms.Label();
            this.StartTimeLabel = new System.Windows.Forms.Label();
            this.StopTimeLabel = new System.Windows.Forms.Label();
            this.TimeLabel = new System.Windows.Forms.Label();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // TimerLabel
            // 
            this.TimerLabel.AutoSize = true;
            this.TimerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.TimerLabel.Location = new System.Drawing.Point(3, 13);
            this.TimerLabel.Name = "TimerLabel";
            this.TimerLabel.Size = new System.Drawing.Size(120, 31);
            this.TimerLabel.TabIndex = 42;
            this.TimerLabel.Text = "00:00:00";
            this.TimerLabel.DoubleClick += new System.EventHandler(this.TimerLabel_DoubleClick);
            this.TimerLabel.Click += new System.EventHandler(this.TimerLabel_Click);
            // 
            // StartTimeLabel
            // 
            this.StartTimeLabel.AutoSize = true;
            this.StartTimeLabel.Location = new System.Drawing.Point(6, 52);
            this.StartTimeLabel.Name = "StartTimeLabel";
            this.StartTimeLabel.Size = new System.Drawing.Size(49, 13);
            this.StartTimeLabel.TabIndex = 41;
            this.StartTimeLabel.Text = "00:00:00";
            // 
            // StopTimeLabel
            // 
            this.StopTimeLabel.AutoSize = true;
            this.StopTimeLabel.Location = new System.Drawing.Point(74, 52);
            this.StopTimeLabel.Name = "StopTimeLabel";
            this.StopTimeLabel.Size = new System.Drawing.Size(49, 13);
            this.StopTimeLabel.TabIndex = 39;
            this.StopTimeLabel.Text = "00:00:00";
            // 
            // TimeLabel
            // 
            this.TimeLabel.AutoSize = true;
            this.TimeLabel.Location = new System.Drawing.Point(39, 0);
            this.TimeLabel.Name = "TimeLabel";
            this.TimeLabel.Size = new System.Drawing.Size(49, 13);
            this.TimeLabel.TabIndex = 40;
            this.TimeLabel.Text = "00:00:00";
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // Stopwatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.TimerLabel);
            this.Controls.Add(this.StartTimeLabel);
            this.Controls.Add(this.StopTimeLabel);
            this.Controls.Add(this.TimeLabel);
            this.Name = "Stopwatch";
            this.Size = new System.Drawing.Size(126, 66);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label TimerLabel;
        private System.Windows.Forms.Label StartTimeLabel;
        private System.Windows.Forms.Label StopTimeLabel;
        private System.Windows.Forms.Label TimeLabel;
        private System.Windows.Forms.Timer timer;
    }
}
