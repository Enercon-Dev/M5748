using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;


namespace MPS_Tools
{
    public partial class UdpSerialPort : Component
    {
        private Socket socket;
        private List<byte> recivedData;
        public Form parentForm = null;
        protected IFrame basicFrame = null;
        
        
        public UdpSerialPort()
        {
            recivedData = new List<byte>();
            //DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(BinarySerialPort_DataReceived);
            InitializeComponent();
        }


        private string ip;
        public string IP
        {
            set { ip = value; }
            get { return ip; }
        }

        private int port;
        public int Port
        {
            set { port = value; }
            get { return port; }
        }


        
        public void Open()
        {
            //udpSocket = new UdpClient(
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(System.Net.IPAddress.Any, 0));
            //socket.Bind(new IPEndPoint(IPAddress.Parse("192.168.0.65"), 0));
            //LocalPortLabel.Text = socket.LocalEndPoint.ToString();

            socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
        }

        public bool IsOpen
        {
        get
            {
                if ((socket != null) && (socket.Connected))
                    return true;
                else
                    return false;
            }
        }

        public void Write(byte[] buffer)
        {
            socket.Send(buffer);
        }

        private void PullingTimer_Tick(object sender, EventArgs e)
        {
            if (IsOpen)
            {
                try { parentForm.BeginInvoke(new MethodInvoker(GetNewDataFromPort)); }
                catch { }
                //parentForm.Invoke(new MethodInvoker(GetNewDataFromPort));
            }
        }
        
        private byte[] UdpGetRecivedBytes()
        {
            if (socket == null)
                return new byte[0];

            byte[] buffer = new byte[socket.Available];
            if (buffer.Length > 0)
            {
                socket.Receive(buffer, buffer.Length, 0);
            }
            return buffer;
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

        public event EventHandler RecivedTimeout;
        protected void OnRecivedTimeout()
        {
            if (RecivedTimeout != null)
                RecivedTimeout(this, new EventArgs());
        }

        public event NewFrameRecivedHandler NewFrameRecived;
        protected void OnNewFrame(IFrame frame)
        {
            if (NewFrameRecived != null)
                NewFrameRecived(this, new NewFrameRecivedEventArgs(frame));
        }

        private void GetNewDataFromPort()
        {
            byte[] buffer = UdpGetRecivedBytes();
            if (buffer.Length == 0)
                return;

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
            //if (BytesToRead != 0)
                GetNewDataFromPort();
            //else
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
}
