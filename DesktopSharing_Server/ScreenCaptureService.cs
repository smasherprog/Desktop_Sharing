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
        string _UserName;
        Desktop_Sharing_Shared.Input.DesktopSwitcher _Current_Desktop = new Desktop_Sharing_Shared.Input.DesktopSwitcher();
        Desktop_Sharing_Shared.Screen.ScreenCapture _ScreenCapture = null;

        string WorkingDirectory;
        string _Users_DesktopPath;


        public event Desktop_Sharing_Shared.Input.PInvoke.MouseEventHandler InputMouseEvent;
        public event Desktop_Sharing_Shared.Keyboard.PInvoke.KeyEventHandler InputKeyEvent;
        public event Desktop_Sharing_Shared.Input.PInvoke.FileReceivedHandler FileReceivedEvent;
        public event Desktop_Sharing_Shared.Input.PInvoke.FolderReceivedHandler FolderReceivedEvent;

        public ScreenCaptureService()
        {  
            
            _RunningAsService = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToLower() == "system";
            InputMouseEvent += Desktop_Sharing_Shared.Input.PInvoke.SendMouseEvent;
            InputKeyEvent += Desktop_Sharing_Shared.Keyboard.PInvoke.KeyEvent;
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
            _ScreenCapture = new Desktop_Sharing_Shared.Screen.ScreenCapture();
            _Current_Desktop.BeginInteraction();

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
            _Users_DesktopPath = @"c:\users\" + _UserName + @"\desktop\";
     
           // _Current_Desktop.SelectDesktop("winlogon");
            Running = Status.Starting;
         
            RunNetwork();
            _Current_Desktop.EndInteraction();
        }
        public void OnStop()
        {
            Running = Status.ShuttingDown;
            // Stop(_Network_Thread);
            if(_LastImage != null)
                _LastImage.Dispose();
            _LastImage = null;
           if(_ScreenCapture!=null) _ScreenCapture.Dispose();
        }


        private void RunNetwork()
        {
            Console.WriteLine("Starting Network Thread");
            Running = Status.Running;
            try
            {
                using(var _Secure_Listener = new Secure_Tcp_Listener(WorkingDirectory + "\\privatekey.xml", 6000))
                {
                    bool locked = false;
                    Secure_Stream client = null;
                    while(Running == Status.Running)
                    {


                        //if(!locked)
                        //{
                        //    locked = true;
                        //    Console.WriteLine("Console locked detected");
                        //    if(_Current_Desktop.SelectDesktop("winlogin"))
                        //    {
                        //        Console.WriteLine("Successfuly changed desktops");
                        //    }else 
                        //    {
                        //        Console.WriteLine("Failed changed desktops");
                        //    }
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
                                InputKeyEvent(BitConverter.ToInt32(ms.Blocks[1], 0), (Desktop_Sharing_Shared.Keyboard.PInvoke.PInvoke_KeyState)BitConverter.ToInt32(ms.Blocks[2], 0));
                            }
                            break;
                        }
                    case ((int)Desktop_Sharing_Shared.Message_Types.FILE):
                        {
                            if(FileReceivedEvent != null)
                            {

                                FileReceivedEvent(_Users_DesktopPath + Desktop_Sharing_Shared.Utilities.Format.GetString(ms.Blocks[1]), ms.Blocks[2]);
                            }
                            break;
                        }
                    case ((int)Desktop_Sharing_Shared.Message_Types.FOLDER):
                        {
                            if(FolderReceivedEvent != null)
                            {
                                FolderReceivedEvent(_Users_DesktopPath + Desktop_Sharing_Shared.Utilities.Format.GetString(ms.Blocks[1]));
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
