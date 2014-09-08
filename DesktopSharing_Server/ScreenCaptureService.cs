using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DesktopSharing_Server
{
    public class ScreenCaptureService
    {
        enum Status { Starting, Running, Stopped, ShuttingDown }
        System.Threading.Thread _Network_Thread;
        Status Running = Status.Stopped;

        UdpClient mytcpl;
        public ScreenCaptureService()
        {
            _Network_Thread = null;
        }
        public void OnStart()
        {
            OnStop();//just in case
            Running = Status.Starting;
            _Network_Thread = new System.Threading.Thread(new System.Threading.ThreadStart(RunNetwork));
            _Network_Thread.Start();
        }
        public void OnStop()
        {
            Running = Status.ShuttingDown;
            Stop(_Network_Thread);
        }

        private void Stop(Thread t)
        {
            try
            {
                if(t != null)
                {
                    t.Join(500);
                    if(t.ThreadState != System.Threading.ThreadState.Stopped)
                        t.Abort();
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void RunNetwork()
        {
            Debug.WriteLine("Starting Network Thread");
            Running = Status.Running;

            try
            {
                mytcpl = new UdpClient();
                var ep = new IPEndPoint(IPAddress.Parse("192.168.0.2"), 6000); // endpoint where server is listening (testing localy)
                mytcpl.Connect(ep);
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }


            while(Running == Status.Running)
            {
                try
                {

                    using(var ms = new MemoryStream())
                    {
                        using(var img = ScreenCapture.GetScreen(new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)))
                        {
                            img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            mytcpl.Send(ms.ToArray(), (int)ms.Length);
                        }
                    }


                } catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            Running = Status.Stopped;
            Debug.WriteLine("Finished Network Thread");
        }
    }
}
