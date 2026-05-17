namespace M5748_Monitor
{
    partial class UserBatt
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
            this.label31 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.battLabel = new System.Windows.Forms.Label();
            this.binaryTelLable3 = new MPS_Tools.BinaryTelLable();
            this.binaryTelLable2 = new MPS_Tools.BinaryTelLable();
            this.binaryTelLable1 = new MPS_Tools.BinaryTelLable();
            this.binaryTelLable24 = new MPS_Tools.BinaryTelLable();
            this.analogTelLable2 = new MPS_Tools.AnalogTelLable();
            this.analogTelLable5 = new MPS_Tools.AnalogTelLable();
            this.analogTelLable4 = new MPS_Tools.AnalogTelLable();
            this.analogTelLable3 = new MPS_Tools.AnalogTelLable();
            this.analogTelLable1 = new MPS_Tools.AnalogTelLable();
            this.StatusLable = new MPS_Tools.AnalogTelLable();
            this.binaryTelLable4 = new MPS_Tools.BinaryTelLable();
            this.binaryTelLable5 = new MPS_Tools.BinaryTelLable();
            this.SuspendLayout();
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(118, 13);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(37, 13);
            this.label31.TabIndex = 22;
            this.label31.Text = "Status";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(118, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "Temperature";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(118, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 13);
            this.label2.TabIndex = 22;
            this.label2.Text = "Time To Full/Empty";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(118, 65);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 22;
            this.label3.Text = "Voltage";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(118, 78);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 13);
            this.label4.TabIndex = 22;
            this.label4.Text = "Current";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(118, 130);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(83, 13);
            this.label5.TabIndex = 22;
            this.label5.Text = "State Of Charge";
            // 
            // battLabel
            // 
            this.battLabel.AutoSize = true;
            this.battLabel.Location = new System.Drawing.Point(3, 0);
            this.battLabel.Name = "battLabel";
            this.battLabel.Size = new System.Drawing.Size(25, 13);
            this.battLabel.TabIndex = 22;
            this.battLabel.Text = "batt";
            // 
            // binaryTelLable3
            // 
            this.binaryTelLable3.AutoSize = true;
            this.binaryTelLable3.DB = null;
            this.binaryTelLable3.DBFields = new string[] {
        "UserTel.Batt[%1].OverTempAlarm"};
            this.binaryTelLable3.ForeColor = System.Drawing.Color.Red;
            this.binaryTelLable3.Location = new System.Drawing.Point(3, 143);
            this.binaryTelLable3.Name = "binaryTelLable3";
            this.binaryTelLable3.Size = new System.Drawing.Size(77, 13);
            this.binaryTelLable3.TabIndex = 24;
            this.binaryTelLable3.Text = "Temp not High";
            this.binaryTelLable3.TextClear = "Temp not High";
            this.binaryTelLable3.TextSet = "Over Temp. Alarm";
            // 
            // binaryTelLable2
            // 
            this.binaryTelLable2.AutoSize = true;
            this.binaryTelLable2.DB = null;
            this.binaryTelLable2.DBFields = new string[] {
        "UserTel.Batt[%1].TimeAlarm"};
            this.binaryTelLable2.ForeColor = System.Drawing.Color.Red;
            this.binaryTelLable2.Location = new System.Drawing.Point(3, 130);
            this.binaryTelLable2.Name = "binaryTelLable2";
            this.binaryTelLable2.Size = new System.Drawing.Size(92, 13);
            this.binaryTelLable2.TabIndex = 24;
            this.binaryTelLable2.Text = "Rem Time >30min";
            this.binaryTelLable2.TextClear = "Rem Time >30min";
            this.binaryTelLable2.TextSet = "Rem Time <30min";
            // 
            // binaryTelLable1
            // 
            this.binaryTelLable1.AutoSize = true;
            this.binaryTelLable1.DB = null;
            this.binaryTelLable1.DBFields = new string[] {
        "UserTel.Batt[%1].LowChargeAlarm"};
            this.binaryTelLable1.ForeColor = System.Drawing.Color.Red;
            this.binaryTelLable1.Location = new System.Drawing.Point(3, 117);
            this.binaryTelLable1.Name = "binaryTelLable1";
            this.binaryTelLable1.Size = new System.Drawing.Size(82, 13);
            this.binaryTelLable1.TabIndex = 24;
            this.binaryTelLable1.Text = "Charge not Low";
            this.binaryTelLable1.TextClear = "Charge not Low";
            this.binaryTelLable1.TextSet = "Charge Low";
            // 
            // binaryTelLable24
            // 
            this.binaryTelLable24.AutoSize = true;
            this.binaryTelLable24.DB = null;
            this.binaryTelLable24.DBFields = new string[] {
        "UserTel.Batt[%1].TempAlarm"};
            this.binaryTelLable24.ForeColor = System.Drawing.Color.Red;
            this.binaryTelLable24.Location = new System.Drawing.Point(3, 26);
            this.binaryTelLable24.Name = "binaryTelLable24";
            this.binaryTelLable24.Size = new System.Drawing.Size(70, 13);
            this.binaryTelLable24.TabIndex = 24;
            this.binaryTelLable24.Text = "Temp Normal";
            this.binaryTelLable24.TextClear = "Temp Normal";
            this.binaryTelLable24.TextSet = "Temp Alarm";
            // 
            // analogTelLable2
            // 
            this.analogTelLable2.AutoSize = true;
            this.analogTelLable2.DB = null;
            this.analogTelLable2.DBFields = new string[] {
        "UserTel.Batt[%1].TimeToFullEmpty"};
            this.analogTelLable2.Location = new System.Drawing.Point(3, 78);
            this.analogTelLable2.Name = "analogTelLable2";
            this.analogTelLable2.Size = new System.Drawing.Size(16, 13);
            this.analogTelLable2.TabIndex = 23;
            this.analogTelLable2.Text = "---";
            // 
            // analogTelLable5
            // 
            this.analogTelLable5.AutoSize = true;
            this.analogTelLable5.DB = null;
            this.analogTelLable5.DBFields = new string[] {
        "UserTel.Batt[%1].StateOfCharge"};
            this.analogTelLable5.Location = new System.Drawing.Point(3, 156);
            this.analogTelLable5.Name = "analogTelLable5";
            this.analogTelLable5.Size = new System.Drawing.Size(16, 13);
            this.analogTelLable5.TabIndex = 23;
            this.analogTelLable5.Text = "---";
            // 
            // analogTelLable4
            // 
            this.analogTelLable4.AutoSize = true;
            this.analogTelLable4.DB = null;
            this.analogTelLable4.DBFields = new string[] {
        "UserTel.Batt[%1].Current"};
            this.analogTelLable4.Location = new System.Drawing.Point(3, 104);
            this.analogTelLable4.Name = "analogTelLable4";
            this.analogTelLable4.Size = new System.Drawing.Size(16, 13);
            this.analogTelLable4.TabIndex = 23;
            this.analogTelLable4.Text = "---";
            // 
            // analogTelLable3
            // 
            this.analogTelLable3.AutoSize = true;
            this.analogTelLable3.DB = null;
            this.analogTelLable3.DBFields = new string[] {
        "UserTel.Batt[%1].Voltage"};
            this.analogTelLable3.Location = new System.Drawing.Point(3, 91);
            this.analogTelLable3.Name = "analogTelLable3";
            this.analogTelLable3.Size = new System.Drawing.Size(16, 13);
            this.analogTelLable3.TabIndex = 23;
            this.analogTelLable3.Text = "---";
            // 
            // analogTelLable1
            // 
            this.analogTelLable1.AutoSize = true;
            this.analogTelLable1.DB = null;
            this.analogTelLable1.DBFields = new string[] {
        "UserTel.Batt[%1].Temperature"};
            this.analogTelLable1.Location = new System.Drawing.Point(3, 65);
            this.analogTelLable1.Name = "analogTelLable1";
            this.analogTelLable1.Size = new System.Drawing.Size(16, 13);
            this.analogTelLable1.TabIndex = 23;
            this.analogTelLable1.Text = "---";
            // 
            // StatusLable
            // 
            this.StatusLable.AutoSize = true;
            this.StatusLable.DB = null;
            this.StatusLable.DBFields = new string[] {
        "",
        "UserTel.Batt[%1].Status"};
            this.StatusLable.Location = new System.Drawing.Point(3, 13);
            this.StatusLable.Name = "StatusLable";
            this.StatusLable.Size = new System.Drawing.Size(16, 13);
            this.StatusLable.TabIndex = 23;
            this.StatusLable.Text = "---";
            this.StatusLable.DataUpdated += new System.EventHandler(this.StatusLable_DataUpdated);
            // 
            // binaryTelLable4
            // 
            this.binaryTelLable4.AutoSize = true;
            this.binaryTelLable4.DB = null;
            this.binaryTelLable4.DBFields = new string[] {
        "UserTel.Batt[%1].SMBusDetected"};
            this.binaryTelLable4.ForeColor = System.Drawing.SystemColors.ControlText;
            this.binaryTelLable4.Location = new System.Drawing.Point(3, 39);
            this.binaryTelLable4.Name = "binaryTelLable4";
            this.binaryTelLable4.ShadeMode = MPS_Tools.BinaryTelLable.Shade.Non;
            this.binaryTelLable4.Size = new System.Drawing.Size(58, 13);
            this.binaryTelLable4.TabIndex = 24;
            this.binaryTelLable4.Text = "No SMBus";
            this.binaryTelLable4.TextClear = "No SMBus";
            this.binaryTelLable4.TextSet = "SMBus Detected";
            // 
            // binaryTelLable5
            // 
            this.binaryTelLable5.AutoSize = true;
            this.binaryTelLable5.DB = null;
            this.binaryTelLable5.DBFields = new string[] {
        "UserTel.Batt[%1].Chargable"};
            this.binaryTelLable5.ForeColor = System.Drawing.SystemColors.ControlText;
            this.binaryTelLable5.Location = new System.Drawing.Point(3, 52);
            this.binaryTelLable5.Name = "binaryTelLable5";
            this.binaryTelLable5.Size = new System.Drawing.Size(81, 13);
            this.binaryTelLable5.TabIndex = 24;
            this.binaryTelLable5.Text = "Not Chargeable";
            this.binaryTelLable5.TextClear = "Not Chargeable";
            this.binaryTelLable5.TextSet = "Chargeable";
            // 
            // UserBatt
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.binaryTelLable3);
            this.Controls.Add(this.binaryTelLable2);
            this.Controls.Add(this.binaryTelLable1);
            this.Controls.Add(this.binaryTelLable5);
            this.Controls.Add(this.binaryTelLable4);
            this.Controls.Add(this.binaryTelLable24);
            this.Controls.Add(this.analogTelLable2);
            this.Controls.Add(this.analogTelLable5);
            this.Controls.Add(this.analogTelLable4);
            this.Controls.Add(this.analogTelLable3);
            this.Controls.Add(this.analogTelLable1);
            this.Controls.Add(this.StatusLable);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.battLabel);
            this.Controls.Add(this.label31);
            this.Name = "UserBatt";
            this.Size = new System.Drawing.Size(104, 179);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MPS_Tools.AnalogTelLable StatusLable;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.Label label1;
        private MPS_Tools.BinaryTelLable binaryTelLable24;
        private MPS_Tools.AnalogTelLable analogTelLable1;
        private System.Windows.Forms.Label label2;
        private MPS_Tools.AnalogTelLable analogTelLable2;
        private System.Windows.Forms.Label label3;
        private MPS_Tools.AnalogTelLable analogTelLable3;
        private System.Windows.Forms.Label label4;
        private MPS_Tools.AnalogTelLable analogTelLable4;
        private System.Windows.Forms.Label label5;
        private MPS_Tools.BinaryTelLable binaryTelLable1;
        private MPS_Tools.BinaryTelLable binaryTelLable2;
        private MPS_Tools.BinaryTelLable binaryTelLable3;
        private MPS_Tools.AnalogTelLable analogTelLable5;
        private System.Windows.Forms.Label battLabel;
        private MPS_Tools.BinaryTelLable binaryTelLable4;
        private MPS_Tools.BinaryTelLable binaryTelLable5;
    }
}
