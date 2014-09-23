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
        Bitmap _LastImage = null;
        //Receiver pipe = new Receiver();

        ImageCodecInfo jgpEncoder;
        EncoderParameters myEncoderParameters;

        public event Desktop_Sharing_Shared.Input.Constants.MouseEventHandler InputMouseEvent;

        public ScreenCaptureService()
        {
            _Network_Thread = null;
            jgpEncoder = GetEncoder(ImageFormat.Jpeg);
            var myEncoder = System.Drawing.Imaging.Encoder.Quality;
            var myEncoderParameter = new EncoderParameter(myEncoder, 60L);
            myEncoderParameters = new EncoderParameters(1);
            myEncoderParameters.Param[0] = myEncoderParameter;
            InputMouseEvent += Desktop_Sharing_Shared.Input.PInvoke.SendMouseEvent;
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
                        SendPass(client);
                        ReadPass(client);
  
                    }
                }

            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            Running = Status.Stopped;
            Debug.WriteLine("Finished Network Thread");
        }
        private void ReadPass(Secure_Stream client)
        {
            try
            {
                if(client.Client.Available <= 0)
                    return;
                var ms = client.Read_And_Unencrypt();
                switch(ms.Type)
                {
                    case ((int)Desktop_Sharing_Shared.Message_Types.MOUSE_EVENT):
                        {
                            if(InputMouseEvent != null)
                            {
                                InputMouseEvent(BitConverter.ToInt32(ms.Blocks[1], 0), BitConverter.ToInt32(ms.Blocks[2], 0), BitConverter.ToInt32(ms.Blocks[3], 0), BitConverter.ToInt32(ms.Blocks[4], 0));
                            }
                            break;
                        }
                    default:
                        break;
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        private void SendPass(Secure_Stream client)
        {

            try
            {

                var img = Desktop_Sharing_Shared.ScreenCapture.GetScreen(new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
                if(_LastImage == null)
                {
                    Debug.WriteLine("Sending " + (int)Desktop_Sharing_Shared.Message_Types.RESOLUTION_CHANGE);
                    var ms = new Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.RESOLUTION_CHANGE);
                    using(var memorys = new MemoryStream())
                    {
                        img.Save(memorys, jgpEncoder, myEncoderParameters);
                        ms.Add_Block(memorys.ToArray());
                    }
                    Debug.WriteLine("Sending image to client");
                    client.Encrypt_And_Send(ms);
                } else
                {
                    Debug.WriteLine("Sending " + (int)Desktop_Sharing_Shared.Message_Types.UPDATE_REGION);
                    var ms = new Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.UPDATE_REGION);

                    var rect = Desktop_Sharing_Shared.Bitmap_Helper.Get_Diff(_LastImage, img);
                    if(rect.Width > 0 && rect.Height > 0)
                    {

                        using(var updateregion = img.Clone(rect, img.PixelFormat))
                        {
                            ms.Add_Block(BitConverter.GetBytes(rect.Top));
                            ms.Add_Block(BitConverter.GetBytes(rect.Left));
                            using(var memorys = new MemoryStream())
                            {
                                updateregion.Save(memorys, jgpEncoder, myEncoderParameters);
                                ms.Add_Block(memorys.ToArray());
                            }
                            Debug.WriteLine("Sending image to client");
                            client.Encrypt_And_Send(ms);
                        }
                    }

                    _LastImage.Dispose();
                }
                _LastImage = img;


            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }

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
