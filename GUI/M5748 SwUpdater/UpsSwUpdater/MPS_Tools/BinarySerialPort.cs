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
    public partial class BinarySerialPort : System.IO.Ports.SerialPort
    {
        private List<byte> recivedData;
        public Form parentForm = null;
        protected IFrame basicFrame = null;
        
        
        public BinarySerialPort()
        {
            recivedData = new List<byte>();
            DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(BinarySerialPort_DataReceived);
            InitializeComponent();
        }


        private void BinarySerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (IsOpen)
            {
                try { parentForm.BeginInvoke(new MethodInvoker(GetNewDataFromPort)); }
                catch { }
                //parentForm.Invoke(new MethodInvoker(GetNewDataFromPort));
            }
        }

        protected void RestartTimeOut()
        {
            TimeOutTimer.Stop();
            TimeOutTimer.Start();
        }

        public byte[] GetRecivedData()
        {
            return recivedData.ToArray();
        }
        
        public void SetPerentForm(Form form)
        {
            parentForm = form;
        }

        public void SetBasicFrame(IFrame frame)
        {
            basicFrame = frame;
        }

        public event EventHandler BinaryDataRecived;
        protected void OnBinaryDataRecived()
        {
            if (BinaryDataRecived != null)
                BinaryDataRecived(this, new EventArgs());
        }

        public event RecivedTimeoutHandler RecivedTimeout;
        protected void OnRecivedTimeout()
        {
            if (RecivedTimeout != null)
                RecivedTimeout(this, new RecivedTimeoutEventArgs(new List<byte>(recivedData)));
        }

        public event NewFrameRecivedHandler NewFrameRecived;
        protected void OnNewFrame(IFrame frame)
        {
            if (NewFrameRecived != null)
                NewFrameRecived(this, new NewFrameRecivedEventArgs(frame));
        }

        private void GetNewDataFromPort()
        {
            int bytes = BytesToRead;
            byte[] buffer = new byte[bytes];
            Read(buffer, 0, bytes);
            recivedData.AddRange(buffer);

            RestartTimeOut(); //ParentForm.Invoke(new MethodInvoker(RestartTimeOut));

            if (basicFrame == null)
            {
                OnBinaryDataRecived();
            }
            else
            {
                while (true)
                {
                    IFrame newFrame = basicFrame.GetFrameCopy();
                    int[] result = newFrame.ReadFromStream(recivedData.ToArray());
                    if (result[0] < 0)
                        return;
                    else if (result[1] < 0)
                    {
                        recivedData.RemoveRange(0, result[0]);
                        return;
                    }
                    recivedData.RemoveRange(0, result[0] + result[1]);
                    OnNewFrame(newFrame);
                }
            }
        }

        private void TimeOutTimer_Tick(object sender, EventArgs e)
        {
            parentForm.Invoke(new MethodInvoker(timeOut));
        }

        private void timeOut()
        {
            if (BytesToRead != 0)
                GetNewDataFromPort();
            else
            {
                TimeOutTimer.Stop();
                if (recivedData.Count > 0)
                {
                    OnRecivedTimeout();
                    recivedData.Clear();
                }
            }
        }
    }


    public delegate void NewFrameRecivedHandler(object sender, NewFrameRecivedEventArgs e);
    public class NewFrameRecivedEventArgs : EventArgs
    {
        public IFrame Frame;

        public NewFrameRecivedEventArgs(IFrame frame)
        {
            Frame = frame;
        }
    }

    public delegate void RecivedTimeoutHandler(object sender, RecivedTimeoutEventArgs e);
    public class RecivedTimeoutEventArgs : EventArgs
    {
        public List<byte> RecivedData;

        public RecivedTimeoutEventArgs(List<byte> recivedData)
        {
            RecivedData = recivedData;
        }
    }
}
