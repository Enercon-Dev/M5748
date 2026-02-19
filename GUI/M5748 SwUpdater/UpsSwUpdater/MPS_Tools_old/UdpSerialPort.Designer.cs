namespace MPS_Tools
{
    partial class UdpSerialPort
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
            this.TimeOutTimer = new System.Windows.Forms.Timer(this.components);
            this.PullingTimer = new System.Windows.Forms.Timer(this.components);
            // 
            // TimeOutTimer
            // 
            this.TimeOutTimer.Tick += new System.EventHandler(this.TimeOutTimer_Tick);
            // 
            // PullingTimer
            // 
            this.PullingTimer.Enabled = true;
            this.PullingTimer.Interval = 10;
            this.PullingTimer.Tick += new System.EventHandler(this.PullingTimer_Tick);

        }

        #endregion

        private System.Windows.Forms.Timer TimeOutTimer;
        private System.Windows.Forms.Timer PullingTimer;
    }
}
