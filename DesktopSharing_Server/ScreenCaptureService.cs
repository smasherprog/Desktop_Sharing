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
        Status Running = Status.Stopped;
        Bitmap _LastImage = null;
        //Receiver pipe = new Receiver();
        bool _RunningAsService;
        string WorkingDirectory;

        Desktop_Sharing_Shared.Desktop.DesktopInfo _DesktopInfo;
        Desktop_Sharing_Shared.Screen.ScreenCapture _ScreenCapture;

        int MSPerFrame = 200;
        DateTime _LastFrame = DateTime.Now;

        public ScreenCaptureService()
        {
            _RunningAsService = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToLower().Contains(@"nt authority\system");
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
            _DesktopInfo = new Desktop_Sharing_Shared.Desktop.DesktopInfo();
            _ScreenCapture = new Desktop_Sharing_Shared.Screen.ScreenCapture(70);
            Running = Status.Starting;
            RunNetwork();
        }

        public void OnStop()
        {
            Running = Status.ShuttingDown;
            // Stop(_Network_Thread);
            if(_LastImage != null)
                _LastImage.Dispose();
            _LastImage = null;
            if(_ScreenCapture != null)
                _ScreenCapture.Dispose();
            if(_DesktopInfo != null)
                _DesktopInfo.Dispose();
        }


        private void RunNetwork()
        {
            Console.WriteLine("Starting Network Thread");
            Running = Status.Running;
            try
            {
                using(var _Secure_Listener = new Secure_Tcp_Listener(WorkingDirectory + "\\privatekey.xml", 6000))
                {

                    Secure_Stream client = null;
                    while(Running == Status.Running)
                    {
                        if(_RunningAsService)
                        {//only a program running as under the account     nt authority\system       is allowed to switch desktops
                            var d = _DesktopInfo.GetActiveDesktop();
                            if(d != _DesktopInfo.Current_Desktop)
                            {
                                _DesktopInfo.SwitchDesktop(d);
                            }
                        }
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
                                client.Dispose();
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
                if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.MOUSE_EVENT)
                {

                    int width = Screen.AllScreens.Sum(a => a.Bounds.Width);
                    int height = Screen.AllScreens.Max(a => a.Bounds.Height);

                    _DesktopInfo.InputMouseEvent((Desktop_Sharing_Shared.Mouse.PInvoke.WinFormMouseEventFlags)BitConverter.ToInt32(ms.Blocks[1], 0),
                            (int)((double)BitConverter.ToInt32(ms.Blocks[2], 0) / (double)width * (double)65535),
                            (int)((double)BitConverter.ToInt32(ms.Blocks[3], 0) / (double)height * (double)65535),
                            BitConverter.ToInt32(ms.Blocks[4], 0));
                } else if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.KEY_EVENT)
                {
                    _DesktopInfo.InputKeyEvent(BitConverter.ToInt32(ms.Blocks[1], 0), (Desktop_Sharing_Shared.Keyboard.PInvoke.PInvoke_KeyState)BitConverter.ToInt32(ms.Blocks[2], 0));
                } else if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.FILE)
                {
                    _DesktopInfo.FileEvent(Desktop_Sharing_Shared.Utilities.Format.GetString(ms.Blocks[1]), ms.Blocks[2]);
                } else if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.FOLDER)
                {
                    _DesktopInfo.FolderEvent(Desktop_Sharing_Shared.Utilities.Format.GetString(ms.Blocks[1]));
                }

            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        private void SendPass(Secure_Stream client, bool sendsyncscreen = false)
        {
            if((DateTime.Now - _LastFrame).TotalMilliseconds < MSPerFrame)
                return;
       
            try
            {
                var dt = DateTime.Now;
                var img = _ScreenCapture.GetScreen(new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
                Debug.WriteLine("Time taken to capture: " + (DateTime.Now - dt).TotalMilliseconds);
                if(_LastImage == null || sendsyncscreen)
                {

                    var ms = new Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.RESOLUTION_CHANGE);
                    using(var memorys = new MemoryStream())
                    {
                        img.Save(memorys, _ScreenCapture.jgpEncoder, _ScreenCapture.EncoderParameters);
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
                                updateregion.Save(memorys, _ScreenCapture.jgpEncoder, _ScreenCapture.EncoderParameters);
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

    }
}
