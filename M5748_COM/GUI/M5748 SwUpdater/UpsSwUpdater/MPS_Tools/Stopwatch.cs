using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace M339_Monitor
{
    public partial class Stopwatch : UserControl
    {
        private bool StopwatchRuning = false;
        private DateTime TimerStart;
        
        public Stopwatch()
        {
            InitializeComponent();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            TimeLabel.Text = DateTime.Now.ToLongTimeString();
            if (StopwatchRuning)
            {
                TimeSpan interval = (DateTime.Now) - TimerStart;
                DateTime d = new DateTime(interval.Ticks);
                TimerLabel.Text = d.ToLongTimeString();
            }
        }

        private void TimerLabel_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void TimerLabel_DoubleClick(object sender, EventArgs e)
        {
            Start();
        }


        public void Start()
        {
            TimerStart = DateTime.Now;
            StopwatchRuning = true;

            StartTimeLabel.Text = TimerStart.ToLongTimeString();
            StopTimeLabel.ForeColor = Color.Silver;
        }

        public void Stop()
        {
            if (StopwatchRuning)
            {
                StopwatchRuning = false;

                StopTimeLabel.Text = DateTime.Now.ToLongTimeString();
                StopTimeLabel.ForeColor = Color.Black;
            }
        }
        
        [DefaultValue(false)]
        public bool Runing
        {
            get { return StopwatchRuning; }
            set
            {
                if (value)
                    Start();
                else
                    Stop();
            }
        }
    }
}
