using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SecureTcp
{
    public class ReceiveMessageObject
    {
        public Secure_Stream Client = null;
        public const int HeaderSize=20;
        public const int IVSize=20;
        public byte[] HeaderBuffer = new byte[HeaderSize];
        public byte[] MessageBuffer = new byte[0];
        public int BytesRead = 0;
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
            Sent_BPS = Received_BPS=Sent_Total = Received_Total = 0;
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
          
                    Client.Send(aes.IV);
                    using(ICryptoTransform encrypt = aes.CreateEncryptor())
                    {
                        var sendbuffer= Tcp_Message.ToBuffer(m);
                        var encryptedbytes = encrypt.TransformFinalBlock(sendbuffer, 0, sendbuffer.Length);
                
                        Client.Send(BitConverter.GetBytes(encryptedbytes.Length));
                        Client.Send(encryptedbytes);
                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        public delegate void MessageReceivedHandler(Secure_Stream client, Tcp_Message ms);
        public event MessageReceivedHandler MessageReceivedEvent;
        public void BeginRead()
        {
            _BeginRead(this);
        }
        private static void _BeginRead(Secure_Stream c)
        {
            var state = new ReceiveMessageObject();
            state.Client = c;
            c.Client.BeginReceive(state.HeaderBuffer, 0, ReceiveMessageObject.HeaderSize, 0,
                new AsyncCallback(ReadHeaderCallback), state);
        }
        private static void ReadHeaderCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            var state = (ReceiveMessageObject)ar.AsyncState;
            var handler = state.Client;

            // Read data from the client socket. 
            int bytesRead = handler.Client.EndReceive(ar);

            if(bytesRead > 0)
            {
                state.BytesRead += bytesRead;
                if(state.BytesRead == ReceiveMessageObject.HeaderSize)
                {
                    state.MessageBuffer = new byte[BitConverter.ToInt32(state.HeaderBuffer, 16)];
                    state.BytesRead = 0;
                    handler.Client.BeginReceive(state.MessageBuffer, 0, state.MessageBuffer.Length, 0, new AsyncCallback(ReadMessageCallback), state);
                } else
                {
                    // Not all data received. Get more.
                    handler.Client.BeginReceive(state.HeaderBuffer, state.BytesRead, ReceiveMessageObject.HeaderSize - state.BytesRead, 0, new AsyncCallback(ReadHeaderCallback), state);
                }
            }
        }
        private static void ReadMessageCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            var state = (ReceiveMessageObject)ar.AsyncState;
            var handler = state.Client;

            // Read data from the client socket. 
            var bytesRead = handler.Client.EndReceive(ar);
            if(bytesRead > 0)
            {
                state.BytesRead += bytesRead;
                if(state.BytesRead == state.MessageBuffer.Length)
                {//done receiving message
                    using(AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                    {
                        aes.KeySize = handler._MySessionKey.Length * 8;
                        aes.Key = handler._MySessionKey;
                        var iv = new byte[16];
                        Array.Copy(state.HeaderBuffer, iv, 16);
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
                            _BeginRead(state.Client);//start over again
                        }
                    }
                } else
                {
                    // Not all data received. Get more.
                    handler.Client.BeginReceive(state.MessageBuffer, state.BytesRead, state.MessageBuffer.Length - state.BytesRead, 0, new AsyncCallback(ReadMessageCallback), state);
                }
            }
        }
        public Tcp_Message Read_And_Unencrypt()
        {
            try
            {
               
                if(Client.Available>0)
                {
                    var iv = new byte[16];
          
                    Client.Receive(iv, 16, SocketFlags.None);
                    var b = BitConverter.GetBytes(0);
            
                    Client.Receive(b, b.Length, SocketFlags.None);
                    var len = BitConverter.ToInt32(b, 0);

                    using(AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                    {
                        aes.KeySize = _MySessionKey.Length * 8;
                        aes.Key = _MySessionKey;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        var buffer = new byte[len];
                        Client.Receive(buffer, buffer.Length, SocketFlags.None);
                  
                        using(ICryptoTransform decrypt = aes.CreateDecryptor())
                        {
                            var arrybuf = decrypt.TransformFinalBlock(buffer, 0, buffer.Length);
                            return Tcp_Message.FromBuffer(arrybuf);

                        }
                    }
                } else
                    return null;
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }

        }
        private static void ReadExact(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int read;
            while(count > 0 && (read = stream.Read(buffer, offset, count)) > 0)
            {
                offset += read;
                count -= read;
            }
            if(count != 0)
                throw new System.IO.EndOfStreamException();
        }
    }
}
