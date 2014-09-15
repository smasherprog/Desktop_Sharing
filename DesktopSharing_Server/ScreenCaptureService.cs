using Pipes;
using SecureTcp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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

        //Receiver pipe = new Receiver();

        public ScreenCaptureService()
        {
            _Network_Thread = null;
            //pipe.Data += new DesktopService_API.DataIsReady(DataBeingRecieved);
            //if(pipe.ServiceOn() == false)
            //    MessageBox.Show(pipe.error.Message);

        }
        string DataBeingRecieved(string data)
        {
            Debug.WriteLine(data);
            return "";
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
            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);
            var myEncoderParameters = new EncoderParameters(1);
            var myEncoder =System.Drawing.Imaging.Encoder.Quality;
            var myEncoderParameter = new EncoderParameter(myEncoder, 60L);
            myEncoderParameters.Param[0] = myEncoderParameter;

            try
            {
                using(var _Secure_Listener = new Secure_Tcp_Listener(Directory.GetCurrentDirectory() + "\\privatekey.xml", 6000))
                {
                    Secure_Stream client = null;
                    while(Running == Status.Running)
                    {
                        try
                        {
                            if(client == null)
                                client = _Secure_Listener.AcceptTcpClient();
                        } catch(Exception e)
                        {
                            Debug.WriteLine(e.Message);
                        }

                        if(client == null)
                            continue;
                        try
                        {
                            if(!client.Client.Connected)
                            {
                                client.Client.Close();
                                client = null;
                                continue;
                            }
                        } catch(Exception e)
                        {
                            Debug.WriteLine(e.Message);
                        }
                        if(client == null)
                            continue;

                        try
                        {
                            using(var ms = new MemoryStream())
                            {
                                using(var img = ScreenCapture.GetScreen(new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)))
                                {
                                   
                                    img.Save(ms, jgpEncoder, myEncoderParameters);
                                    Debug.WriteLine("Sending image to client");
                                    client.Encrypt_And_Send(ms.ToArray());
                                }
                            }


                        } catch(Exception e)
                        {
                            Debug.WriteLine(e.Message);
                        }
                    }
                }

            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            Running = Status.Stopped;
            Debug.WriteLine("Finished Network Thread");
        }
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach(ImageCodecInfo codec in codecs)
            {
                if(codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
