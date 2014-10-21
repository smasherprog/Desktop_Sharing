using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace DesktopSharing_Viewer.Code
{
    public class Viewer_Loop
    {
        public enum Status { Starting, Running, Stopped, ShuttingDown }
        public System.Threading.Thread _Thread;
        public Status Running = Status.Stopped;

        public delegate void UpdateImageHandler(Point tl, byte[] data);
        public event UpdateImageHandler UpdateImageEvent;

        public delegate void NewImageHandler(byte[] data);
        public event NewImageHandler NewImageEvent;

        public delegate void MousePositionChangedHandler(Point tl);
        public event MousePositionChangedHandler MousePositionChangedEvent;

        public delegate void MouseImageChangedHandler(Point tl, byte[] data);
        public event MouseImageChangedHandler MouseImageChangedEvent;

        private List<SecureTcp.Tcp_Message> _OutGoingMessages;
        private object _OutGoingMessagesLock = new object();

        private List<string> _OutGoingFiles;
        private object _OutGoingFilesLock = new object();

        private DateTime Secondcounter = DateTime.Now;
        public string IPtoConnect;

        public Viewer_Loop()
        {
            Running = Status.Stopped;
            _OutGoingMessages = new List<SecureTcp.Tcp_Message>();
            _OutGoingFiles = new List<string>();
            IPtoConnect = "127.0.0.1";
        }
        public void Start()
        {
            Stop();
            Running = Status.Starting;
            Secondcounter = DateTime.Now;
            _Thread = new System.Threading.Thread(new System.Threading.ThreadStart(Run));
            _Thread.Start();
        }
        public void SendFiles(string[] files)
        {
            lock(_OutGoingFilesLock)
            {
                _OutGoingFiles.AddRange(files);
            }
        }

        public void OnMouseEvent(Desktop_Sharing_Shared.Mouse.MouseEventStruct m)
        {
            var t = new SecureTcp.Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.MOUSE_EVENT);
            t.Add_Block(BitConverter.GetBytes((int)m.msg));
            t.Add_Block(BitConverter.GetBytes(m.x));
            t.Add_Block(BitConverter.GetBytes(m.y));
            t.Add_Block(BitConverter.GetBytes(m.wheel_delta));
            lock(_OutGoingMessagesLock)
            {
                _OutGoingMessages.Add(t);
            }
        }
        public void OnKeyEvent(Desktop_Sharing_Shared.Keyboard.KeyboardEventStruct k)
        {
            var t = new SecureTcp.Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.KEY_EVENT);
            t.Add_Block(BitConverter.GetBytes(k.bVk));
            t.Add_Block(BitConverter.GetBytes((int)k.s));
            lock(_OutGoingMessagesLock)
            {
                _OutGoingMessages.Add(t);
            }
        }
        public void Run()
        {

            Running = Status.Running;

            try
            {
                using(var server = SecureTcp.Secure_Tcp_Client.Connect(Directory.GetCurrentDirectory() + "\\publickey.xml", IPtoConnect, 443))
                {
                    server.MessageReceivedEvent += server_MessageReceivedEvent;
                    server.BeginRead();
                    while(Running == Status.Running)
                    {
                        SendPass(server);
                    }

                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            Running = Status.Stopped;
        }

        void server_MessageReceivedEvent(SecureTcp.Secure_Stream client, SecureTcp.Tcp_Message ms)
        {
            switch(ms.Type)
            {
                case ((int)Desktop_Sharing_Shared.Message_Types.UPDATE_REGION):
                    {
                        UpdateImageEvent(new Point(BitConverter.ToInt32(ms.Blocks[2], 0), BitConverter.ToInt32(ms.Blocks[1], 0)), ms.Blocks[3]);
                        break;
                    }
                case ((int)Desktop_Sharing_Shared.Message_Types.RESOLUTION_CHANGE):
                    {
                        NewImageEvent(ms.Blocks[1]);
                        break;
                    }
                case ((int)Desktop_Sharing_Shared.Message_Types.MOUSE_IMAGE_EVENT):
                    {
                        MouseImageChangedEvent(new Point(BitConverter.ToInt32(ms.Blocks[2], 0), BitConverter.ToInt32(ms.Blocks[1], 0)), ms.Blocks[3]);
                        break;
                    }
                case ((int)Desktop_Sharing_Shared.Message_Types.MOUSE_POSITION_EVENT):
                    {
                        MousePositionChangedEvent(new Point(BitConverter.ToInt32(ms.Blocks[2], 0), BitConverter.ToInt32(ms.Blocks[1], 0)));
                        break;
                    }
                default:
                    break;
            }
        }

        private void SendPass(SecureTcp.Secure_Stream client)
        {
            lock(_OutGoingMessagesLock)
            {
                foreach(var item in _OutGoingMessages)
                {
                    client.Encrypt_And_Send(item);
                }
                _OutGoingMessages.Clear();
            }
            lock(_OutGoingFilesLock)
            {
                foreach(var item in _OutGoingFiles)
                {
                    AddFileOrDirectory("", item);
                }
                _OutGoingFiles.Clear();
            }
        }
        private void AddFileOrDirectory(string root1, string fullpath)
        {
            if(Directory.Exists(fullpath))
            {
                try
                {
                    var di = new DirectoryInfo(fullpath);
                    var t = new SecureTcp.Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.FOLDER);
                    var foldername = root1 + di.Name;
                    t.Add_Block(Desktop_Sharing_Shared.Utilities.Format.GetBytes(foldername));
                    _OutGoingMessages.Add(t);
                    foreach(var item in di.GetDirectories())
                    {
                        AddFileOrDirectory(foldername + "\\", item.FullName);
                    }
                    foreach(var item in di.GetFiles())
                    {
                        AddFileOrDirectory(foldername + "\\", item.FullName);
                    }
                } catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            } else
            {
                try
                {
                    var fi = File.ReadAllBytes(fullpath);
                    var t = new SecureTcp.Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.FILE);
                    t.Add_Block(Desktop_Sharing_Shared.Utilities.Format.GetBytes(root1 + Path.GetFileName(fullpath)));
                    t.Add_Block(fi);
                    _OutGoingMessages.Add(t);

                } catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }
        public void Stop()
        {
            Running = Status.Stopped;
        }
    }
}
