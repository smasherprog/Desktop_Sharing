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
    
        //Receiver pipe = new Receiver();

        private Desktop_Service _Desktop_Service;
        private TCP_Server _Server;

        public ScreenCaptureService()
        {

        }

        public void OnStart()
        {
    
            _Desktop_Service = new Desktop_Service();
            _Desktop_Service.ScreenUpdateEvent += _Desktop_Service_ScreenUpdateEvent;
            _Desktop_Service.MouseImageChangedEvent += _Desktop_Service_MouseImageChangedEvent;
            _Desktop_Service.MousePositionChangedEvent += _Desktop_Service_MousePositionChangedEvent;
            _Server = new TCP_Server(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\privatekey.xml", 443);
            _Server.ReceiveEvent += _server_ReceiveEvent;
            _Server.NewClientEvent += _Server_NewClientEvent;
            _Server.DisconnectEvent += _Server_DisconnectEvent;
            RunNetwork();
        }

        void _Server_DisconnectEvent(Secure_Stream client)
        {
            if(_Server.ClientCount <= 0)
                _Desktop_Service.Capturing = false;//stop capturing since no clients are connected
        }

        public void OnStop()
        {
 
            if(_Desktop_Service != null)
                _Desktop_Service.Dispose();
            if(_Server != null)
                _Server.Dispose();
        }


        private void RunNetwork()
        {
            Console.WriteLine("Starting Network Thread");
         
            try
            {
                _Server.Start();
                _Desktop_Service.Start();//<-- main loop

            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            _Server.Stop();
            _Desktop_Service.Stop();

            Debug.WriteLine("Finished Network Thread");
        }
        static int counter = 0;
        private void _Desktop_Service_ScreenUpdateEvent(byte[] data, Rectangle r)
        {
            var ms = new Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.UPDATE_REGION);
            ms.Add_Block(BitConverter.GetBytes(r.Top));
            ms.Add_Block(BitConverter.GetBytes(r.Left));
            ms.Add_Block(BitConverter.GetBytes(r.Height));
            ms.Add_Block(BitConverter.GetBytes(r.Width));
            ms.Add_Block(Desktop_Sharing_Shared.Compression.GZip.Compress(data));
            _Server.Send(ms);
  
        }
        void _Desktop_Service_MousePositionChangedEvent(Point tl)
        {
            var ms = new Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.MOUSE_POSITION_EVENT);
            ms.Add_Block(BitConverter.GetBytes(tl.Y));
            ms.Add_Block(BitConverter.GetBytes(tl.X));
            _Server.Send(ms);
        }

        void _Desktop_Service_MouseImageChangedEvent(Point tl, byte[] data)
        {
            var ms = new Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.MOUSE_IMAGE_EVENT);
            ms.Add_Block(BitConverter.GetBytes(tl.Y));
            ms.Add_Block(BitConverter.GetBytes(tl.X));
            ms.Add_Block(data);
            _Server.Send(ms);
        }
   
        void _Server_NewClientEvent(Secure_Stream client)
        {
            if(!_Desktop_Service.Capturing)
            {
                _Desktop_Service.Capturing = true;
                Thread.Sleep(200);//make sure to sleep long enough for the background service to start up and get an image if needed.
            }
            var ms = new Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.RESOLUTION_CHANGE);
            var tmp = _Desktop_Service._LastImage;//make sure to get a copy
            ms.Add_Block(BitConverter.GetBytes(tmp.Dimensions.Height));
            ms.Add_Block(BitConverter.GetBytes(tmp.Dimensions.Width));
            ms.Add_Block(Desktop_Sharing_Shared.Compression.GZip.Compress(tmp.Data));
            Debug.WriteLine("Sending RESOLUTION_CHANGE image to client");
            client.Encrypt_And_Send(ms);

   
        }
        private void _server_ReceiveEvent(Tcp_Message ms)
        {
            try
            {
                if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.MOUSE_EVENT)
                {
                    int width = Screen.AllScreens.Sum(a => a.Bounds.Width);
                    int height = Screen.AllScreens.Max(a => a.Bounds.Height);
                    _Desktop_Service.MouseEvent(new Desktop_Sharing_Shared.Mouse.MouseEventStruct
                    {
                        msg = (Desktop_Sharing_Shared.Mouse.PInvoke.WinFormMouseEventFlags)BitConverter.ToInt32(ms.Blocks[1], 0),
                        wheel_delta = BitConverter.ToInt32(ms.Blocks[4], 0),
                        x = (int)((double)BitConverter.ToInt32(ms.Blocks[2], 0) / (double)width * (double)65535),
                        y = (int)((double)BitConverter.ToInt32(ms.Blocks[3], 0) / (double)height * (double)65535)
                    });


                } else if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.KEY_EVENT)
                {
                    _Desktop_Service.KeyEvent(new Desktop_Sharing_Shared.Keyboard.KeyboardEventStruct
                    {
                        bVk = BitConverter.ToInt32(ms.Blocks[1], 0),
                        s = (Desktop_Sharing_Shared.Keyboard.PInvoke.PInvoke_KeyState)BitConverter.ToInt32(ms.Blocks[2], 0)
                    });
                } else if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.FILE)
                {
                    _Desktop_Service.FileEvent(Desktop_Sharing_Shared.Utilities.Format.GetString(ms.Blocks[1]), ms.Blocks[2]);
                } else if(ms.Type == (int)Desktop_Sharing_Shared.Message_Types.FOLDER)
                {
                    _Desktop_Service.FolderEvent(Desktop_Sharing_Shared.Utilities.Format.GetString(ms.Blocks[1]));
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

    }
}
