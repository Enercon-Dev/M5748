using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MPS_Tools;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO.Ports;


// V1.00 -  10/02/2021
// first release

namespace M1787_Monitor
{
    public partial class MainForm : Form
    {
        public static MainForm form;
        DataLogForm logForm = null;
        TelDecode telDecoder;
        public Commands commands;
        public const int TotalBattNum = 6;
        public const int BattNum = 3;
        public const int ChargersNum = 2;
        //public bool legacyTel = false;
        private byte[] transmitedBuffer = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            dbInit();
            LoadParameters();
            serialPort.parentForm = this;
            serialPort.SetBasicFrame(new M1787Frame());
            try
            {
                UartNameComboBox.Items.Clear();

                string[] ports = SerialPort.GetPortNames()
                                           .OrderBy(p => p)
                                           .ToArray();

                foreach (string port in ports)
                    UartNameComboBox.Items.Add(port);

                if (UartNameComboBox.Items.Count > 0)
                {
                    UartNameComboBox.SelectedIndex = 0;
                }
            }
            catch
            {
                MessageBox.Show("Fail to open " + serialPort.PortName + " at BautRate " + serialPort.BaudRate.ToString(),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

            form = this;
            logForm = new DataLogForm(db);
            telDecoder = new TelDecode(db, log, this);
            commands = new Commands(log, this);
            autoReqRadioButton_CheckedChanged(null, null);
            //tabControl1.SelectedIndex = 1;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            logForm.CloseForm();
            serialPort.Close();
            CloseTcpConnection();
            SaveParameters();
        }


        private void LoadParameters()
        {
            SimpleXML XML = new SimpleXML("M1787 Monitor.xml");
           // UartNameComboBox.Text = XML.ReadString("/ComName", "COM1");
            EnableLogCheckBox.Checked = XML.ReadString("/LogEnable", "true") == "true";
            blinkCheckBox.Checked = XML.ReadString("/BlinkEnable", "true") == "true";
            autoCommanCheckBox.Checked = XML.ReadString("/AutoCommand", "false") == "true";
            //switch (XML.ReadString("/AutoRequest", "Off"))
            //{
            //    case "Off": autoReqOffRadioButton.Checked = true; break;
            //    case "Status": AutoReqStatusRadioButton.Checked = true; break;
            //    case "Statistics": autoReqStatRadioButton.Checked = true; break;
            //}
        }

        private void SaveParameters()
        {
            SimpleXML XML = new SimpleXML("M1787 Monitor.xml");

            XML.WriteString("/ComName", UartNameComboBox.Text);
            XML.WriteString("/LogEnable", EnableLogCheckBox.Checked ? "true" : "false");
            XML.WriteString("/BlinkEnable", blinkCheckBox.Checked ? "true" : "false");
            XML.WriteString("/AutoCommand", autoCommanCheckBox.Checked ? "true" : "false");

            if (AutoReqStatusRadioButton.Checked)
                XML.WriteString("/AutoRequest", "Status");
            else
                XML.WriteString("/AutoRequest", "Off");

            XML.SaveToFile();
        }


        public void Send(DataBuffer buffer, String description)
        {
            if (serialPort.IsOpen == false)
            {
                Console.Beep();
                return;
            }

            if (comReplyTimer.Enabled)
            {
                Console.Beep();
                return;
            }

            M1787Frame frame = new M1787Frame(buffer.GetDataBuffer());
            frame.Description = description;
            frame.PrepareForTransmition();
            comReplyTimer.Enabled = true;
            SendingCommandLabel.Visible = true;

            if (EnableLogCheckBox.Checked)
                log.Add(frame);
            TransmitBuffer(frame.GetFrameBuffer());
        }



        public void TransmitBuffer(byte[] buffer)
        {
            transmitedBuffer = buffer;
            try
            {
                serialPort.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                MessageBox.Show("Send fail: " + e.ToString(),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                transmitedBuffer = null;
            }
            
        }

        private void comReplyTimer_Tick(object sender, EventArgs e)
        {
            comReplyTimer.Enabled = false;
            SendingCommandLabel.Visible = false;
        }

        private bool buffersEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null)
                return false;
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        private void serialPort_NewFrameRecived(object sender, MPS_Tools.NewFrameRecivedEventArgs e)
        {
            //frame received (it could be a loopback frame)

            if (!e.Frame.CsumOk())
            {
                log.Add(e.Frame.ToHexString(), "tel. wrong CRC", LogedStatus.LsTelemetryError);
            }
            else if (!buffersEqual(transmitedBuffer, e.Frame.GetFrameBuffer()))
            {
                //handle incoming frame but only if it is not the transmitted frame
                transmitedBuffer = null;
#warning todo
                if (true) //here we should verify that the incoming frame is the answer to the send frame
                {
                    comReplyTimer.Enabled = false;
                    SendingCommandLabel.Visible = false;
                }

                telDecoder.DecodeTelemetry(e.Frame);
                timer1_Tick(null, null); //tel received - request automaticaly next tel
            }
        }

        private void serialPort_RecivedTimeout(object sender, RecivedTimeoutEventArgs e)
        {
            log.Add(e.RecivedData.ToArray(), "Time Out", LogedStatus.LsTelemetryError);
        }

        public void decodeArrayTel(DataBuffer buffer)
        {
            /*
            chart1.Series["Array1"].Points.Clear();


            double min= double.MaxValue, max = double.MinValue, sum=0;;
            for(int i=0; i<127; i++)
            {
                double value = buffer.getShort() * 0.001;
                chart1.Series["Array1"].Points.Add(value);
                if (value > max) max = value;
                if (value < min) min = value;
                if (i<20) sum += value/20;
            }
            maxLabel.Text = max.ToString();
            minLabel.Text = min.ToString();
            deltaLabel.Text = (max - min).ToString();

            averageLabel.Text = "average 20 = " + sum.ToString();
             */
        }

        private void OpenDataLogbutton_Click(object sender, EventArgs e)
        {
            logForm.Show();
        }


        int autoTelRequestType = 0;
        private async void timer1_Tick(object sender, EventArgs e)
        {
            if (!timer1.Enabled)
                return;

            if (sender == null && autoTelRequestType == 0)
            {
                return;
            }

            if (AutoReqStatusRadioButton.Checked)
            {
                //request status of all 3 boards starting with HPF,
                //  the otherf two will be requested through NextTelRequests() function
                commands.geHpfStatusTel();
            }
            else if (autoReqHPFRadioButton.Checked)
            {
                commands.geHpfStatusTel();
            }
            else if (autoReqIsoRadioButton.Checked)
            {
                commands.getIsoStatusTel();
            }
            else if (autoReqBuckRadioButton.Checked)
            {
                settingsCmdButton_Click(null, null);
            }
            else if (autoReqTCPRadioButton.Checked)
            {
                switch (autoTelRequestType++)
                {
                    case 0:
                        await SendTCPCommand("DBG1?");
                        break;

                    case 1:
                        await SendTCPCommand("MEAS:VOLT?");
                        break;

                    case 2:
                        await SendTCPCommand("MEAS:CURR?");
                        break;

                    case 3:
                        await SendTCPCommand("SOUR:CCORCVMODE?");
                        break;

                    case 4:
                        await SendTCPCommand("SOUR:OPMODE?");
                        break;

                    case 5:
                        await SendTCPCommand("STAT:PROT:COND?");
                        autoTelRequestType = 0;
                        break;

                }

            }
        }

        
        public void NextTelRequests(int stage)
        {
            if (AutoReqStatusRadioButton.Checked)
            {
                if (stage == 1)
                    commands.getIsoStatusTel();
                else if (stage == 2)
                    settingsCmdButton_Click(null, null);
            }
        }



        /*
        private void autoRequestCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = autoRequestCheckBox.Checked;
        }*/

        private void autoReqRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (autoReqOffRadioButton.Checked)
            {
                timer1.Enabled = false;
            }
            else
            {
                timer1.Enabled = true;
                timer1.Interval = 1000;
            }
            return;
            autoTelRequestType = 0; //currenty not in use
        }


        private void blinkCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            AnalogTelLable.blinkEnable = blinkCheckBox.Checked;
            BinaryTelLable.blinkEnable = blinkCheckBox.Checked;
        }



        private void getArrayButton_Click(object sender, EventArgs e)
        {
            commands.getArrayTel();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 255; i++)
            {
                //chart1.Series["Array1"].Points.Add(33+i*0.01);
            }
        }


        private void UartNameComboBox_DropDown(object sender, EventArgs e)
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            UartNameComboBox.Items.Clear();
          //  UartNameComboBox.Items.Add("TCP");
            foreach (string port in ports)
                UartNameComboBox.Items.Add(port);
        }

        private async void UartNameComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (UartNameComboBox.Text == "")
                return;

            string selectedPort = (UartNameComboBox.SelectedItem as string);

            if (selectedPort == "TCP")
            {
                await SetTcpConnection();
            }
            else if (!serialPort.IsOpen ||
                (serialPort.PortName != selectedPort))
            {
                try
                {
                    serialPort.Close();
                    serialPort.PortName = selectedPort;
                    serialPort.Open();
                }
                catch
                {
                    MessageBox.Show("Can't open com \"" + selectedPort + "\"");
                }
            }
        }

        private void copyUserTelButton_Click(object sender, EventArgs e)
        {
            //Clipboard.SetText(telDecoder.mainUserTelDecodeDisplay().Replace("UserTel.", ""));
        }


        private void getMasterStatisticsButton_Click(object sender, EventArgs e)
        {
            //commands.getStatTel();
        }

        #region special_tel_labels

        private void busATLable_DataUpdated(object sender, EventArgs e)
        {
            double bus = db["HPF.AnIn.BUS_P"].GetAnalogValue() - db["HPF.AnIn.BUS_N"].GetAnalogValue();
           // busATLable.Text = bus.ToString(busATLable.NumberFormat);
        }

        private void buckVersionATLable_DataUpdated(object sender, EventArgs e)
        {
            buckVersionATLable.Text = db["BUCK.Version.Major"].GetIntValue() + "." + db["BUCK.Version.Minor"].GetIntValue();
        }

        private void isoVersionATLable_DataUpdated(object sender, EventArgs e)
        {
            //isoVersionATLable.Text = db["ISO.Version.Major"].GetIntValue() + "." + db["ISO.Version.Minor"].GetIntValue();
        }

        private void hpfVersionATLable_DataUpdated(object sender, EventArgs e)
        {
            int a = db["Version.Major"].GetIntValue();
            int b = db["Version.Minor"].GetIntValue();
            hpfVersionATLable.Text = db["Version.Major"].GetIntValue() + "." + db["Version.Minor"].GetIntValue();
        }

        #endregion special_tel_labels

        #region command_buttons

        private void settingsCmdButton_Click(object sender, EventArgs e)
        {
            SettingsCmdParam param = new SettingsCmdParam();
            param.OutputEnable = outEnCheckBox.Checked;
            param.Vout = double.Parse(voutSetTextBox.Text);
            param.Iout = double.Parse(ioutSetTextBox.Text);
            param.OverVoltage = double.Parse(ovSetTextBox.Text);
            param.OverCurrent = double.Parse(ocSetTextBox.Text);
            commands.setSettingsCommand(param);
        }


        private void error1Button_Click(object sender, EventArgs e)
        {
            byte x;
            x = byte.Parse(errorParam1TextBox.Text, System.Globalization.NumberStyles.AllowHexSpecifier);
            byte[] buffer = { 0xC6, 0xED, 0xF2, 0xB9, 0x01, 0, 0 };
            buffer[5] = x;
            buffer[6] = (byte)M1787Frame.CalcCSUM(buffer, 6);
            log.Add(buffer, "wrong OKcode", LogedStatus.LsCommand);
            TransmitBuffer(buffer);
        }

        private void error2Button_Click(object sender, EventArgs e)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x02);
            buffer.fillByte(0x01);
            buffer.fillShort(0x07D0);
            buffer.fillByte(0x00);
            Send(buffer, "Wrong Length");
        }

        private void error3Button_Click(object sender, EventArgs e)
        {
            byte[] buffer = { 0xC6, 0xED, 0xC6, 0xED, 0xF2, 0xB9, 0x01, 0x41, 0xA0 };
            log.Add(buffer, "get User Status Tel + extra bytes", LogedStatus.LsCommand);
            TransmitBuffer(buffer);
        }

        private void error4Button_Click(object sender, EventArgs e)
        {
            byte[] buffer = { 0xC6, 0xED, 0xF2, 0xB9, 0x01, 0x41, 0xA1 };
            log.Add(buffer, "get User Status Tel + wrong CSUM", LogedStatus.LsCommand);
            TransmitBuffer(buffer);
        }

        private void error5Button_Click(object sender, EventArgs e)
        {
            byte[] buffer = { 0xC6, 0xED, 0xF2, 0xB9, 0x02, 0x80, 0x01, 0xE1 };
            log.Add(buffer, "sending ACK", LogedStatus.LsCommand);
            TransmitBuffer(buffer);
        }



        private void button1_Click(object sender, EventArgs e)
        {
            string str = "*IDN?\r\n" + "SYST:COMM:MAC?\r\n" + "DBG1?\r\n" + "MEAS:VOLT?\r\n" + "MEAS:CURR?\r\n" + "SOUR:CCORCVMODE?\r\n" + "SOUR:OPMODE?\r\n" + "STAT:PROT:COND?\r\n";
            //string str = "*IDN?\r\n" + "SYST:COMM:MAC?\r" + "DBG1?\n" + "MEAS:VOLT?\n\r" + "MEAS:CURR?\r\n";
            SendTCPCommand(str, false);

            //telDecoder.decodeScpiResponce("*IDN?", "55, test ggg");
            return;

            //var frame = new M1787Frame(new byte[] {0x83, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 127,  0, 0,0,0,0,0,0,0,0,0});
            //telDecoder.DecodeTelemetry(frame); //127 does not work well = -1.1A

            var buf = new DataBuffer(new byte[] { 0x2, 0x94 });
            db["ISO.AnIn.Temp1"].Decode(buf);
            db.UpdateClients();
        }

        private async void buckPresetButton_Click(object sender, EventArgs e)
        {
            var par = (sender as Button).Tag.ToString().Split(',');
            voutSetTextBox.Text = par[0];
            ioutSetTextBox.Text = par[1];
            await SendTCPCommand("SOUR:VOLT " + voutSetTextBox.Text);
            await SendTCPCommand("SOUR:CURR " + ioutSetTextBox.Text);

            if (!AutoReqStatusRadioButton.Checked && !autoReqBuckRadioButton.Checked)
                settingsCmdButton_Click(null, null);
        }

        private void getStatusButton_Click(object sender, EventArgs e)
        {
            commands.geHpfStatusTel();
        }

        private void SetOutputControlBtn_Click(object sender, EventArgs e)
        {
            bool[] outputs = new bool[]
            {
                dch_EN2_Enable.Checked,
                dch_EN1_Enable.Checked,
                Heater_Enable.Checked,
            chargeSwEnable.Checked,
                McuEnable.Checked,
            OvTestEnable.Checked,
            BpRstEnable.Checked,
            DsblTestEnable.Checked
            };
           
            
            commands.SetOutputControl(outputs);
        }

        private void getIsoTelButton_Click(object sender, EventArgs e)
        {
            commands.getIsoStatusTel();
        }

        #endregion command_buttons


        #region TCP_Port
        TcpClient client;
        byte[] tcpBuffer = new byte[200];
        /*
        private void openTcpButton_Click(object sender, EventArgs e)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse("192.168.3.10"), 7);
            client = new TcpClient();
            client.Connect(ipEndPoint);
            //client.BeginConnect("192.168.0.10", 7, new AsyncCallback(BeginConnectCallback), null);
            NetworkStream stream = client.GetStream();
            stream.BeginRead(tcpBuffer, 0, tcpBuffer.Length, new AsyncCallback(ReadCallback), this);
        }

        private delegate void DecodeTelemetryDelegate(byte[] data);
        public static void ReadCallback(IAsyncResult ar)
        {
            MainForm form = ar.AsyncState as MainForm;
            NetworkStream stream = form.client.GetStream();
            stream.EndRead(ar);

            //form.telDecoder.decodeDebugTel(form.tcpBuffer);
            try { form.BeginInvoke(new MainForm.DecodeTelemetryDelegate(form.telDecoder.decodeDebugTel), new object[] { form.tcpBuffer }); }
            catch { }

            stream.BeginRead(form.tcpBuffer, 0, TelDecode.DEBUG_TEL_LENGTH, new AsyncCallback(ReadCallback), form);
        }*/

        private async Task SetTcpConnection()
        {
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.161"), 5025);
            client = new TcpClient();

            NetworkStream stream;
            try
            {
                //todo: this part should be async as well somehow
                tcpStatusLabel.Text = "Opening";
                await Task.Run(() => client.Connect(ipEndPoint));
                stream = client.GetStream();
                tcpStatusLabel.Text = "Open";
            }
            catch
            {
                tcpStatusLabel.Text = "Fail to Open";
                return;
            }


            string receivedStr;
            while (client.Connected)
            {
                try
                {
                    int i = await stream.ReadAsync(form.tcpBuffer, 0, form.tcpBuffer.Length);
                    if (i == 0) //this happends when the other side closes the connection
                    {
                        await SendTCPCommand("DBG1?"); //for testing purpose - forcfully sends more data after the UUT closes his side of the TCP connection
                        //throw new Exception("close"); //just a fancy way to close connection
                    }
                    receivedStr = Encoding.ASCII.GetString(form.tcpBuffer, 0, i);
                    //log.Add(receivedStr, "tcp tel", LogedStatus.LsMessage);
                    telDecoder.decodeScpiResponce(lastScpiCmd, receivedStr);
                }
                catch
                {
                    client.Close();
                    tcpStatusLabel.Text = "Close";
                }
            }
        }

        private void CloseTcpConnection()
        {
            if (client == null || !client.Connected)
                return;
            client.Close();
            tcpStatusLabel.Text = "Close";
        }

        private string lastScpiCmd;
        private async Task SendTCPCommand(string cmd, bool appendNewLine=true)
        {
            if (client == null || !client.Connected)
                return;


            lastScpiCmd = cmd; //save the command for future responce decoding
            if (appendNewLine)
                cmd = cmd + "\r\n";
            log.Add(cmd, "TCP command", LogedStatus.LsCommand);
            var sendBuffer = Encoding.ASCII.GetBytes(cmd);
            NetworkStream stream = client.GetStream();
            try
            {
                await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);
            }
            catch
            {
                log.Add("Fail to send TCP command", "Error", LogedStatus.LsError);
                return;
            }
        }


        private async void open2TcpButton_Click(object sender, EventArgs e)
        {
            await SetTcpConnection();
        }

        private void closeTcpButton_Click(object sender, EventArgs e)
        {
            CloseTcpConnection();
        }

        #endregion TCP_Port

        #region TCP_Command_Butons

        private async void tcpManualSendButton_Click(object sender, EventArgs e)
        {
            await SendTCPCommand(tcpCommandTextBox.Text);
        }

        private async void testTcpButton_Click(object sender, EventArgs e)
        {
            await SendTCPCommand(tcpCommandTextBox.Text, false);
        }

        private async void getAButton_Click(object sender, EventArgs e)
        {
            await SendTCPCommand("DBG1?");
        }

        private async void getBButton_Click(object sender, EventArgs e)
        {
            await SendTCPCommand("DBG2?");
        }

        private async void getControlButton_Click(object sender, EventArgs e)
        {
            await SendTCPCommand("DBG3?");
        }

        private async void tcpCommandTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                await SendTCPCommand(tcpCommandTextBox.Text);
        }

        private async void setSNButton_Click(object sender, EventArgs e)
        {
            uint sn;
            if (setSNTextBox.Text.Length == 0 ||
                setSNTextBox.Text != verifySNTextBox.Text ||
                !uint.TryParse(setSNTextBox.Text, out sn))
            {
                MessageBox.Show("Wrong SN", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (sn > 10000) //no special reason for 10K limit
            {
                MessageBox.Show("SN to big", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await SendTCPCommand("PROD:SETSN_JS8M "+ sn);
            verifySNTextBox.Text = "---";
        }

        #endregion TCP_Command_Butons

        private async void voutSetTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                await SendTCPCommand("SOUR:VOLT " + voutSetTextBox.Text);
        }

        private async void ioutSetTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                await SendTCPCommand("SOUR:CURR " + ioutSetTextBox.Text);
        }

        private async void ovSetTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                await SendTCPCommand("SOUR:VOLT:PROT " + ovSetTextBox.Text);
        }

        private async void ocSetTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                await SendTCPCommand("SOUR:CURR:PROT " + ocSetTextBox.Text);
        }

        private async void outEnCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            await SendTCPCommand("OUTP:STAT " + (outEnCheckBox.Checked ? "1" : "0"));
        }

        private async void getScpiRegButton_Click(object sender, EventArgs e)
        {
        }


        private async void scpiCommand_Click(object sender, EventArgs e)
        {
            string tag = (sender as Control).Tag.ToString();
            await SendTCPCommand(tag);
        }


        string sdReasonStr = "";
        private void sdReasonATLable_DataUpdated(object sender, EventArgs e)
        {
            List<string> sdReasons = new List<string>();
            string header = "SD_Reason.";
            foreach (DBField f in db.Fields)
            {
                if (f.Name.StartsWith(header) && f.GetFlagValue())
                    sdReasons.Add(f.Name.Substring(header.Length));
            }
            sdReasonATLable.Text = sdReasons.Count.ToString();
            sdReasonStr = string.Join(Environment.NewLine, sdReasons);
            sdReasonATLable.SetToolTip(sdReasonStr);
        }

        private void sdReasonATLable_DoubleClick(object sender, EventArgs e)
        {
            MessageBox.Show(sdReasonStr, "Shutdown reasons");
        }

      
    }

    public enum CommAddress { Master = 1, ChargerA = 2, ChargerB = 3}
}