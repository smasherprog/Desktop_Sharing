using Pipes;
using SecureTcp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
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
        bool _RunningAsService;
        string _UserName;
        IntPtr _Current_Desktop;

        ImageCodecInfo jgpEncoder;
        EncoderParameters myEncoderParameters;
        string WorkingDirectory;

        public event Desktop_Sharing_Shared.Input.PInvoke.MouseEventHandler InputMouseEvent;
        public event Desktop_Sharing_Shared.Input.PInvoke.KeyEventHandler InputKeyEvent;
        public event Desktop_Sharing_Shared.Input.PInvoke.FileReceivedHandler FileReceivedEvent;
        public event Desktop_Sharing_Shared.Input.PInvoke.FolderReceivedHandler FolderReceivedEvent;

        public ScreenCaptureService()
        {
            _Network_Thread = null;
            jgpEncoder = GetEncoder(ImageFormat.Jpeg);
            var myEncoder = System.Drawing.Imaging.Encoder.Quality;
            var myEncoderParameter = new EncoderParameter(myEncoder, 60L);
            myEncoderParameters = new EncoderParameters(1);
            myEncoderParameters.Param[0] = myEncoderParameter;
            InputMouseEvent += Desktop_Sharing_Shared.Input.PInvoke.SendMouseEvent;
            InputKeyEvent += Desktop_Sharing_Shared.Input.PInvoke.KeyEvent;
            FileReceivedEvent += Desktop_Sharing_Shared.Input.PInvoke.FileEvent;
            FolderReceivedEvent += Desktop_Sharing_Shared.Input.PInvoke.FolderEvent;
            //pipe.Data += new DesktopService_API.DataIsReady(DataBeingRecieved);
            //if(pipe.ServiceOn() == false)
            //    MessageBox.Show(pipe.error.Message);
            WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        }
        string DataBeingRecieved(string data)
        {
            Debug.WriteLine(data);
            return "";
        }

        public void OnStart()
        {

            OnStop();//just in case
            _RunningAsService = !Environment.UserInteractive;
            using(var searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem"))
            {
                using(var collection = searcher.Get())
                {
                    var s = ((string)collection.Cast<ManagementBaseObject>().First()["UserName"]).Split('\\');
                    if(s.Length > 1)
                        _UserName = s.LastOrDefault();
                    else
                        _UserName = s.FirstOrDefault();
                }
            }

            Running = Status.Starting;
            _Network_Thread = new System.Threading.Thread(new System.Threading.ThreadStart(RunNetwork));
            _Network_Thread.Start();
        }
        public void OnStop()
        {
            Running = Status.ShuttingDown;
            Stop(_Network_Thread);
            Desktop_Sharing_Shared.Input.PInvoke.CloseDesktop(_Current_Desktop);
            if(_LastImage != null)
                _LastImage.Dispose();
            _LastImage = null;
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
            //Desktop_Sharing_Shared.Input.PInvoke.SetWinSta0Desktop("Default");
       
            try
            {
                using(var _Secure_Listener = new Secure_Tcp_Listener(WorkingDirectory + "\\privatekey.xml", 6000))
                {

                    Secure_Stream client = null;
                    while(Running == Status.Running)
                    {


                        //if(Desktop_Sharing_Shared.Input.PInvoke.IsWorkstationLocked())
                        //{
                        //    Desktop_Sharing_Shared.Input.PInvoke.SetWinLogin();
                        //}

                        bool newclient = false;
                        try
                        {
                            if(client == null)
                            {
                                client = _Secure_Listener.AcceptTcpClient();
                                newclient = true;
  
                            }
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
                        SendPass(client, newclient);
                        ReceivePass(client);

                    }
                }


            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            Running = Status.Stopped;
            Debug.WriteLine("Finished Network Thread");
        }
        private void ReceivePass(Secure_Stream client)
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

                                int width = Screen.AllScreens.Sum(a => a.Bounds.Width);
                                int height = Screen.AllScreens.Max(a => a.Bounds.Height);

                                InputMouseEvent((Desktop_Sharing_Shared.Input.PInvoke.WinFormMouseEventFlags)BitConverter.ToInt32(ms.Blocks[1], 0),
                                    (int)((double)BitConverter.ToInt32(ms.Blocks[2], 0) / (double)width * (double)65535),
                                    (int)((double)BitConverter.ToInt32(ms.Blocks[3], 0) / (double)height * (double)65535),
                                    BitConverter.ToInt32(ms.Blocks[4], 0));
                            }
                            break;
                        }
                    case ((int)Desktop_Sharing_Shared.Message_Types.KEY_EVENT):
                        {
                            if(InputKeyEvent != null)
                            {
                                InputKeyEvent(BitConverter.ToInt32(ms.Blocks[1], 0), (Desktop_Sharing_Shared.Input.PInvoke.PInvoke_KeyState)BitConverter.ToInt32(ms.Blocks[2], 0));
                            }
                            break;
                        }
                    case ((int)Desktop_Sharing_Shared.Message_Types.FILE):
                        {
                            if(FileReceivedEvent != null)
                            {

                                FileReceivedEvent("c:\\users\\" + _UserName + "\\desktop\\" + Desktop_Sharing_Shared.Utilities.Format.GetString(ms.Blocks[1]), ms.Blocks[2]);
                            }
                            break;
                        }
                    case ((int)Desktop_Sharing_Shared.Message_Types.FOLDER):
                        {
                            if(FolderReceivedEvent != null)
                            {
                                FolderReceivedEvent("c:\\users\\" + _UserName + "\\desktop\\" + Desktop_Sharing_Shared.Utilities.Format.GetString(ms.Blocks[1]));
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
        private void SendPass(Secure_Stream client, bool sendsyncscreen = false)
        {

            try
            {

                var img = Desktop_Sharing_Shared.ScreenCapture.GetScreen(new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
                if(_LastImage == null || sendsyncscreen)
                {

                    var ms = new Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.RESOLUTION_CHANGE);
                    using(var memorys = new MemoryStream())
                    {
                        img.Save(memorys, jgpEncoder, myEncoderParameters);
                        ms.Add_Block(memorys.ToArray());
                    }
                    Debug.WriteLine("Sending RESOLUTION_CHANGE image to client");
                    client.Encrypt_And_Send(ms);
                } else
                {

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
                            Debug.WriteLine("Sending UPDATE_REGION image to client " + rect.Top + " " + rect.Left);
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
