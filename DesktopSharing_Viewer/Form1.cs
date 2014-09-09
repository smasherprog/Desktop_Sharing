using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace DesktopSharing
{
    public partial class Form1 : Form
    {
        enum Status { Starting, Running, Stopped, ShuttingDown }
        System.Threading.Thread _Thread;
        Status Running = Status.Stopped;
        DateTime _Timer = DateTime.Now;
        int _Counter = 0;
        UdpClient mytcpl;
        public Form1()
        {
            InitializeComponent();
            FormClosing += Form1_FormClosing;
        }

        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Running = Status.ShuttingDown;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(Running == Status.Stopped)
            {
         
                Running = Status.Starting;
                _Thread = new System.Threading.Thread(new System.Threading.ThreadStart(StartCaptureing));
                _Thread.Start();
            }

        }
        private void StartCaptureing()
        {
            Running = Status.Running;
            _Timer = DateTime.Now;
            _Counter = 0;
            int port = 0;
            IPEndPoint RemoteIpEndPoint = null;

                port = Convert.ToInt32(textBox1.Text);
                RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, port);
                mytcpl = new UdpClient(port);
           
            while(Running == Status.Running)
            {
                    if(mytcpl.Available > 0)
                    {
                        using(var ms = new MemoryStream(mytcpl.Receive(ref RemoteIpEndPoint)))
                        {
                            if((DateTime.Now - _Timer).TotalMilliseconds > 1000)
                            {
                                Debug.WriteLine(_Counter + " FPS");
                                _Counter = 0;
                                _Timer = DateTime.Now;
                            }

                            _Counter += 1;
                            pictureBox1.Invoke((MethodInvoker)delegate
                            {
                                try
                                {
                                    if(pictureBox1.Image != null)
                                        pictureBox1.Image.Dispose();
                                    pictureBox1.Image = Image.FromStream(ms);
                                    // pictureBox1.Image = ScreenCapture.GetScreen(new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
                                } catch(Exception e)
                                {
                                    Debug.WriteLine(e.Message);
                                }
                            });
                        }
                    }

            }
            Running = Status.Stopped;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Running = Status.ShuttingDown;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                Pipes.Sender.SendMessage("hey there");
                // Use this if using a specific name named pipe.
                //  NamedPipe.Sender.SendMessage(messages, "Jabberwocky");
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}
