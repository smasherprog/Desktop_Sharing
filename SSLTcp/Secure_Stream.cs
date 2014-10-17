using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SecureTcp
{
    public class MessageObject
    {
        public Secure_Stream Client = null;
        public const int HeaderSize = 20;
        public const int IVSize = 16;
        public byte[] HeaderBuffer = new byte[HeaderSize];
        public byte[] MessageBuffer = new byte[0];
        public int BytesCounter = 0;
    }

    public class Secure_Stream : IDisposable
    {
        public Socket Client;
        byte[] _MySessionKey;

        public long Received_Total;
        public long Sent_Total;

        public long Received_BPS = 1024 * 100;
        public long Sent_BPS = 1024 * 100;

        private List<long> _Bytes_Received_in_Window;
        private List<long> _Bytes_Sent_in_Window;

        private const int _Window_Size = 5000;//5 seconds
        private DateTime _WindowCounter = DateTime.Now;

        public Secure_Stream(Socket c, byte[] sessionkey)
        {
            Client = c;
            Client.NoDelay = true;
            _MySessionKey = sessionkey;
            Sent_BPS = Received_BPS = Sent_Total = Received_Total = 0;
            _Bytes_Received_in_Window = new List<long>();
            _Bytes_Sent_in_Window = new List<long>();
        }
        public void Dispose()
        {
            if(Client != null)
                Client.Close();
            Client = null;
        }
        public void Encrypt_And_Send(Tcp_Message m)
        {
            Write(m);
            var l = m.length;
            _Bytes_Sent_in_Window.Add(l);
            Sent_Total += l;
            UpdateCounters();
        }

        private void UpdateCounters()
        {
            if((DateTime.Now - _WindowCounter).TotalMilliseconds > _Window_Size)
            {
                Sent_BPS = (int)(((double)_Bytes_Sent_in_Window.Sum()) / ((double)(_Window_Size / 1000)));
                Received_BPS = (int)(((double)_Bytes_Received_in_Window.Sum()) / ((double)(_Window_Size / 1000)));

                Debug.WriteLine("Received: " + SizeSuffix(Received_BPS));
                Debug.WriteLine("Sent: " + SizeSuffix(Sent_BPS));

                _Bytes_Received_in_Window.Clear();
                _Bytes_Sent_in_Window.Clear();
                _WindowCounter = DateTime.Now;
            }
        }
        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string SizeSuffix(Int64 value)
        {
            if(value <= 0)
                return "0 bytes";
            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }
        protected void Write(Tcp_Message m)
        {
            try
            {
                using(AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    aes.KeySize = _MySessionKey.Length * 8;
                    aes.Key = _MySessionKey;
                    aes.GenerateIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using(ICryptoTransform encrypt = aes.CreateEncryptor())
                    {

                        var sendbuffer = Tcp_Message.ToBuffer(m);
                        var mes = new MessageObject();
                        mes.MessageBuffer = encrypt.TransformFinalBlock(sendbuffer, 0, sendbuffer.Length);

                        mes.Client = this;
                        Buffer.BlockCopy(aes.IV, 0, mes.HeaderBuffer, 0, aes.IV.Length);

                        var len = BitConverter.GetBytes(mes.MessageBuffer.Length);
                        Buffer.BlockCopy(len, 0, mes.HeaderBuffer, aes.IV.Length, len.Length);
                        Debug.WriteLine("Sending " + mes.MessageBuffer.Length);
                        Client.BeginSend(mes.HeaderBuffer, 0, MessageObject.HeaderSize, 0, new AsyncCallback(SendHeaderCallback), mes);
            
                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        private static void SendHeaderCallback(IAsyncResult ar)
        {

            var state = (MessageObject)ar.AsyncState;
            var handler = state.Client;
            int bytesCount = handler.Client.EndSend(ar);
            Debug.WriteLine("SendHeaderCallback " + bytesCount);
            if(bytesCount > 0)
            {
                state.BytesCounter += bytesCount;
                if(state.BytesCounter == MessageObject.HeaderSize)
                {
                    state.BytesCounter = 0;
                    handler.Client.BeginSend(state.MessageBuffer, 0, state.MessageBuffer.Length, 0, new AsyncCallback(SendMessageCallback), state);
                } else
                {
                   handler.Client.BeginSend(state.HeaderBuffer, state.BytesCounter, MessageObject.HeaderSize - state.BytesCounter, 0, new AsyncCallback(SendHeaderCallback), state);
                }
            }
        }
        private static void SendMessageCallback(IAsyncResult ar)
        {
            Debug.WriteLine("SendMessageCallback");
            var state = (MessageObject)ar.AsyncState;
            var handler = state.Client;
            int bytesCount = handler.Client.EndSend(ar);
            if(bytesCount > 0)
            {
                state.BytesCounter += bytesCount;
                if(state.BytesCounter != state.MessageBuffer.Length)
                {
                    handler.Client.BeginSend(state.MessageBuffer, state.BytesCounter, state.MessageBuffer.Length - state.BytesCounter, 0, new AsyncCallback(SendMessageCallback), state);
                } else  Debug.WriteLine("SendMessageCallback DONE");
            }
        }




        public delegate void MessageReceivedHandler(Secure_Stream client, Tcp_Message ms);
        public event MessageReceivedHandler MessageReceivedEvent;
        //starts the async background
        public void BeginRead()
        {
            _BeginRead(this);
        }
        private static void _BeginRead(Secure_Stream c)
        {
            var state = new MessageObject();
            state.Client = c;
            c.Client.BeginReceive(state.HeaderBuffer, 0, MessageObject.HeaderSize, 0, new AsyncCallback(ReadHeaderCallback), state);
        }
        private static void ReadHeaderCallback(IAsyncResult ar)
        {
            Debug.WriteLine("ReadHeaderCallback");
            var state = (MessageObject)ar.AsyncState;
            var handler = state.Client;
            int bytesCount = handler.Client.EndReceive(ar);
            if(bytesCount > 0)
            {
                state.BytesCounter += bytesCount;
                if(state.BytesCounter == MessageObject.HeaderSize)
                {

                    state.MessageBuffer = new byte[BitConverter.ToInt32(state.HeaderBuffer, MessageObject.IVSize)];
                    state.BytesCounter = 0;
                    handler.Client.BeginReceive(state.MessageBuffer, 0, state.MessageBuffer.Length, 0, new AsyncCallback(ReadMessageCallback), state);
                } else
                {
                    handler.Client.BeginReceive(state.HeaderBuffer, state.BytesCounter, MessageObject.HeaderSize - state.BytesCounter, 0, new AsyncCallback(ReadHeaderCallback), state);
                }
            }
        }
        private static void ReadMessageCallback(IAsyncResult ar)
        {
            Debug.WriteLine("ReadMessageCallback");
            var state = (MessageObject)ar.AsyncState;
            var handler = state.Client;
            var bytesCount = handler.Client.EndReceive(ar);
            if(bytesCount > 0)
            {
                state.BytesCounter += bytesCount;
                if(state.BytesCounter == state.MessageBuffer.Length)
                {//done receiving message
                    using(AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                    {
                        aes.KeySize = handler._MySessionKey.Length * 8;
                        aes.Key = handler._MySessionKey;
                        var iv = new byte[MessageObject.IVSize];
                        Buffer.BlockCopy(state.HeaderBuffer,0, iv, 0, MessageObject.IVSize);
                
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using(ICryptoTransform decrypt = aes.CreateDecryptor())
                        {
                            var arrybuf = decrypt.TransformFinalBlock(state.MessageBuffer, 0, state.MessageBuffer.Length);
                            //Received_Total += state.MessageBuffer.Length;
                            //_Bytes_Received_in_Window.Add(state.MessageBuffer.Length);
                            //UpdateCounters();

                            if(handler.MessageReceivedEvent != null)
                                handler.MessageReceivedEvent(state.Client, Tcp_Message.FromBuffer(arrybuf));
                            Debug.WriteLine("ReadMessageCallback DONE");
                            _BeginRead(state.Client);//start over again
                        }
                    }
                } else
                {

                    handler.Client.BeginReceive(state.MessageBuffer, state.BytesCounter, state.MessageBuffer.Length - state.BytesCounter, 0, new AsyncCallback(ReadMessageCallback), state);
                }
            }
        }

        //public Tcp_Message Read_And_Unencrypt()
        //{
        //    try
        //    {

        //        if(Client.Available > 0)
        //        {
        //            var iv = new byte[MessageObject.IVSize];

        //            Receive(Client, iv, 0, MessageObject.IVSize);

        //            var b = BitConverter.GetBytes(0);

        //            Receive(Client, b, 0, b.Length);
        //            var len = BitConverter.ToInt32(b, 0);

        //            using(AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
        //            {
        //                aes.KeySize = _MySessionKey.Length * 8;
        //                aes.Key = _MySessionKey;
        //                aes.IV = iv;

        //                aes.Mode = CipherMode.CBC;
        //                aes.Padding = PaddingMode.Zeros;
        //                var buffer = new byte[len];
        //                Receive(Client, buffer, 0, buffer.Length);

        //                using(ICryptoTransform decrypt = aes.CreateDecryptor())
        //                {
        //                    var arrybuf = decrypt.TransformFinalBlock(buffer, 0, buffer.Length);
        //                    return Tcp_Message.FromBuffer(arrybuf);

        //                }
        //            }
        //        } else
        //            return null;
        //    } catch(Exception e)
        //    {
        //        Debug.WriteLine(e.Message);
        //        return null;
        //    }

        //}
        //private static void Receive(Socket socket, byte[] buffer, int offset, int size)
        //{
        //    int received = 0;  // how many bytes is already received
        //    do
        //    {

        //        try
        //        {
        //            received += socket.Receive(buffer, offset + received, size - received, SocketFlags.None);
        //        } catch(SocketException ex)
        //        {
        //            if(ex.SocketErrorCode == SocketError.WouldBlock ||
        //                ex.SocketErrorCode == SocketError.IOPending ||
        //                ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
        //            {
        //            } else
        //                throw ex;  // any serious error occurr
        //        }
        //    } while(received < size);
        //}
    }
}
