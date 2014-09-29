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
        public Action<Point, byte[]> Update_Image;
        public Action<byte[]> New_Image;
        private List<SecureTcp.Tcp_Message> _OutGoingMessages;
        private object _OutGoingMessagesLock = new object();

        private DateTime Secondcounter = DateTime.Now;
        public Viewer_Loop()
        {
            Running = Status.Stopped;
            _OutGoingMessages = new List<SecureTcp.Tcp_Message>();
        }
        public void Start()
        {
            Stop();
            Running = Status.Starting;
            Secondcounter = DateTime.Now;
            _Thread = new System.Threading.Thread(new System.Threading.ThreadStart(Run));
            _Thread.Start();
        }
        public void OnMouseEvent(Desktop_Sharing_Shared.Input.PInvoke.WinFormMouseEventFlags msg, int x, int y, int wheel_delta)
        {
            lock(_OutGoingMessagesLock)
            {
                var t = new SecureTcp.Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.MOUSE_EVENT);
                t.Add_Block(BitConverter.GetBytes((int)msg));
                t.Add_Block(BitConverter.GetBytes(x));
                t.Add_Block(BitConverter.GetBytes(y));
                t.Add_Block(BitConverter.GetBytes(wheel_delta));
                _OutGoingMessages.Add(t);
            }
        }
        public void OnKeyEvent(int bVk, Desktop_Sharing_Shared.Input.PInvoke.PInvoke_KeyState s)
        {
            lock(_OutGoingMessagesLock)
            {
                var t = new SecureTcp.Tcp_Message((int)Desktop_Sharing_Shared.Message_Types.KEY_EVENT);
                t.Add_Block(BitConverter.GetBytes(bVk));
                t.Add_Block(BitConverter.GetBytes((int)s));
                _OutGoingMessages.Add(t);
            }
        }
        public void Run()
        {

            Running = Status.Running;

            try
            {
                using(var server = SecureTcp.Secure_Tcp_Client.Connect(Directory.GetCurrentDirectory() + "\\publickey.xml", "192.168.207.130", 6000))
                {
                    while(Running == Status.Running)
                    {
                        ReceivePass(server);
                        SendPass(server);
                    }

                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            Running = Status.Stopped;
        }
        private void ReceivePass(SecureTcp.Secure_Stream client)
        {
            if(client.Client.Available <= 0)
                return;
            var ms = client.Read_And_Unencrypt();
            switch(ms.Type)
            {
                case ((int)Desktop_Sharing_Shared.Message_Types.UPDATE_REGION):
                    {
                        Update_Image(new Point(BitConverter.ToInt32(ms.Blocks[2], 0), BitConverter.ToInt32(ms.Blocks[1], 0)), ms.Blocks[3]);
                        break;
                    }
                case ((int)Desktop_Sharing_Shared.Message_Types.RESOLUTION_CHANGE):
                    {
                        New_Image(ms.Blocks[1]);
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
        }
        public void Stop()
        {
            Running = Status.Stopped;
        }
    }
}
