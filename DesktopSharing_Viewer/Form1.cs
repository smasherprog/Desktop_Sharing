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
            long bytecounter = 0;
            try
            {

                using(var server = SecureTcp.Secure_Tcp_Client.Connect(Directory.GetCurrentDirectory() + "\\publickey.xml", "127.0.0.1", 6000))
                {
                    while(Running == Status.Running)
                    {
                        if(server.Client.Available > 0)
                        {
                            var ms = server.Read_And_Unencrypt();

                            if((DateTime.Now - _Timer).TotalMilliseconds > 1000)
                            {
                                Debug.WriteLine(_Counter + " FPS Bytes received: " + bytecounter);
                                _Counter = 0;
                                _Timer = DateTime.Now;
                                bytecounter = 0;
                            }
                            _Counter += 1;

                            Debug.WriteLine("Received " + ms.Type.ToString());
                            if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.UPDATE_REGION)
                            {
                                var top = BitConverter.ToInt32(ms.Blocks[1], 0);
                                var left = BitConverter.ToInt32(ms.Blocks[2], 0);
                                Debug.WriteLine("got here");
                                pictureBox1.Invoke((MethodInvoker)delegate
                                {
                                    try
                                    {
                                        using(var memo = new MemoryStream(ms.Blocks[3]))
                                        using(var imgregion = Bitmap.FromStream(memo))
                                        using(var g = Graphics.FromImage(pictureBox1.Image))
                                        {
                                            g.DrawImage(imgregion, new Point(left, top));
                                            g.Flush();
                                        }
                                        pictureBox1.Invalidate();
                                        Debug.WriteLine("Received update from server");

                                    } catch(Exception e)
                                    {
                                        Debug.WriteLine(e.Message);
                                    }
                                });
                            } else if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.RESOLUTION_CHANGE)
                            {
                                pictureBox1.Invoke((MethodInvoker)delegate
                                {
                                    try
                                    {
                                        if(pictureBox1.Image != null)
                                            pictureBox1.Image.Dispose();
                                        using(var memo = new MemoryStream(ms.Blocks[1]))
                                        {
                                            pictureBox1.Image = Bitmap.FromStream(memo);
                                        }
                                        Debug.WriteLine("Received image from server");

                                    } catch(Exception e)
                                    {
                                        Debug.WriteLine(e.Message);
                                    }
                                });
                            }
                        }
                    }

                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            Running = Status.Stopped;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Running = Status.ShuttingDown;
        }


    }
}
