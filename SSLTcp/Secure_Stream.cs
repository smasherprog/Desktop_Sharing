using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

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

        private AutoResetEvent _SendResetEvent = new AutoResetEvent(true);

        private const int _Buffer_Size = 1024 * 1024 * 8;//dont want to reallocate large chunks of memory if I dont have to
        byte[] _Buffer = new byte[_Buffer_Size];// 8 megabytes buffer

        public long Received_Total;
        public long Sent_Total;

        public long Received_BPS = 1024 * 100;
        public long Sent_BPS = 1024 * 100;

        private long _Bytes_Received_in_Window;
        private long _Bytes_Sent_in_Window;

        private const int _Window_Size = 5000;//5 seconds
        private DateTime _WindowCounter = DateTime.Now;

        public delegate void DisconnectEventHandler(Secure_Stream client);
        public event DisconnectEventHandler DisconnectEvent;

        public Secure_Stream(Socket c, byte[] sessionkey)
        {
            Client = c;
            Client.NoDelay = true;
            _MySessionKey = sessionkey;
            Sent_BPS = Received_BPS = Sent_Total = Received_Total = 0;
            _Bytes_Received_in_Window = 0;
            _Bytes_Sent_in_Window = 0;

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
            _Bytes_Sent_in_Window += l;
            Sent_Total += l;
            UpdateCounters();
        }

        private void UpdateCounters()
        {
            if((DateTime.Now - _WindowCounter).TotalMilliseconds > _Window_Size)
            {
                Sent_BPS = (int)(((double)_Bytes_Sent_in_Window) / ((double)(_Window_Size / 1000)));
                Received_BPS = (int)(((double)_Bytes_Received_in_Window) / ((double)(_Window_Size / 1000)));

                Debug.WriteLine("Received: " + Utilities.SizeSuffix(Received_BPS));
                Debug.WriteLine("Sent: " + Utilities.SizeSuffix(Sent_BPS));

                _Bytes_Received_in_Window = 0;
                _Bytes_Sent_in_Window = 0;
                _WindowCounter = DateTime.Now;
            }
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
                        var headermessage = new MessageObject();
                        headermessage.Client = this;
                        headermessage.MessageBuffer = headermessage.HeaderBuffer;
                        var datamessage = new MessageObject();
                        datamessage.Client = this;

                        datamessage.MessageBuffer = encrypt.TransformFinalBlock(sendbuffer, 0, sendbuffer.Length);


                        Buffer.BlockCopy(aes.IV, 0, headermessage.MessageBuffer, 0, aes.IV.Length);

                        var len = BitConverter.GetBytes(datamessage.MessageBuffer.Length);
                        Buffer.BlockCopy(len, 0, headermessage.MessageBuffer, aes.IV.Length, len.Length);

                      //  Debug.WriteLine("Sending ");
                     //   _SendResetEvent.WaitOne();

                        Client.BeginSend(headermessage.MessageBuffer, 0, headermessage.MessageBuffer.Length, 0, new AsyncCallback(SendMessageCallback), headermessage);
                        Client.BeginSend(datamessage.MessageBuffer, 0, datamessage.MessageBuffer.Length, 0, new AsyncCallback(SendMessageCallback), datamessage);
                    
                    }
                }
            } catch(Exception e)
            {
              //  _SendResetEvent.Set();//just in case another thread was waiting
                if(DisconnectEvent != null)
                    DisconnectEvent(this);
                Debug.WriteLine(e.Message);
            }
        }
        //private void SendHeaderCallback(IAsyncResult ar)
        //{
        //    try
        //    {
        //        var state = (MessageObject)ar.AsyncState;
        //        var handler = state.Client;
        //        int bytesCount = handler.Client.EndSend(ar);
        //        //  Debug.WriteLine("SendHeaderCallback " + bytesCount);
        //        if(bytesCount > 0)
        //        {
        //            state.BytesCounter += bytesCount;
        //            if(state.BytesCounter == MessageObject.HeaderSize)
        //            {
        //                state.BytesCounter = 0;
        //                handler.Client.BeginSend(state.MessageBuffer, 0, state.MessageBuffer.Length, 0, new AsyncCallback(SendMessageCallback), state);
        //            } else
        //            {
        //                handler.Client.BeginSend(state.HeaderBuffer, state.BytesCounter, MessageObject.HeaderSize - state.BytesCounter, 0, new AsyncCallback(SendHeaderCallback), state);
        //            }
        //        }
        //    } catch(Exception e)
        //    {
        //       // _SendResetEvent.Set();//just in case another thread was waiting
        //        if(DisconnectEvent != null)
        //            DisconnectEvent(this);
        //        Debug.WriteLine(e.Message);
        //    }
        //}
        private void SendMessageCallback(IAsyncResult ar)
        {
            try
            {
                //Debug.WriteLine("SendMessageCallback");
                var state = (MessageObject)ar.AsyncState;
                var handler = state.Client;
                int bytesCount = handler.Client.EndSend(ar);
                if(bytesCount > 0)
                {
                    state.BytesCounter += bytesCount;
                    if(state.BytesCounter != state.MessageBuffer.Length)
                    {
                        handler.Client.BeginSend(state.MessageBuffer, state.BytesCounter, state.MessageBuffer.Length - state.BytesCounter, 0, new AsyncCallback(SendMessageCallback), state);
                    } else
                    {
                        // Debug.WriteLine("SendMessageCallback DONE");
                       // _SendResetEvent.Set();
                    }

                }
            } catch(Exception e)
            {
              //  _SendResetEvent.Set();//just in case another thread was waiting
                if(DisconnectEvent != null)
                    DisconnectEvent(this);
                Debug.WriteLine(e.Message);
            }
        }


        public delegate void MessageReceivedHandler(Secure_Stream client, Tcp_Message ms);
        public event MessageReceivedHandler MessageReceivedEvent;
        //starts the async background
        public void BeginRead()
        {
            _BeginRead(this);
        }
        private void _BeginRead(Secure_Stream c)
        {
            try
            {
                var state = new MessageObject();
                state.Client = c;
                c.Client.BeginReceive(state.HeaderBuffer, 0, MessageObject.HeaderSize, 0, new AsyncCallback(ReadHeaderCallback), state);
            } catch(Exception e)
            {
                if(DisconnectEvent != null)
                    DisconnectEvent(this);
                Debug.WriteLine(e.Message);
            }
        }
        private void ReadHeaderCallback(IAsyncResult ar)
        {
            try
            {
                //  Debug.WriteLine("ReadHeaderCallback");
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
            } catch(Exception e)
            {
                if(DisconnectEvent != null)
                    DisconnectEvent(this);
                Debug.WriteLine(e.Message);
            }

        }
        private void ReadMessageCallback(IAsyncResult ar)
        {
            try
            {
                // Debug.WriteLine("ReadMessageCallback");
                var state = (MessageObject)ar.AsyncState;
                var handler = state.Client;
                var bytesCount = handler.Client.EndReceive(ar);
                if(bytesCount > 0)
                {
                    state.BytesCounter += bytesCount;
                    if(state.BytesCounter == state.MessageBuffer.Length)
                    {//done receiving message

                        _BeginRead(state.Client);//start over again

                        using(AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                        {
                            aes.KeySize = handler._MySessionKey.Length * 8;
                            aes.Key = handler._MySessionKey;
                            var iv = new byte[MessageObject.IVSize];
                            Buffer.BlockCopy(state.HeaderBuffer, 0, iv, 0, MessageObject.IVSize);

                            aes.IV = iv;
                            aes.Mode = CipherMode.CBC;
                            aes.Padding = PaddingMode.PKCS7;

                            using(ICryptoTransform decrypt = aes.CreateDecryptor())
                            {
                                var arrybuf = decrypt.TransformFinalBlock(state.MessageBuffer, 0, state.MessageBuffer.Length);
                                Received_Total += state.MessageBuffer.Length;
                                _Bytes_Received_in_Window += state.MessageBuffer.Length;
                                UpdateCounters();
                                

                                if(handler.MessageReceivedEvent != null)
                                    handler.MessageReceivedEvent(state.Client, Tcp_Message.FromBuffer(arrybuf));
                                // Debug.WriteLine("ReadMessageCallback DONE");
                            }
                        }
                    } else
                    {

                        handler.Client.BeginReceive(state.MessageBuffer, state.BytesCounter, state.MessageBuffer.Length - state.BytesCounter, 0, new AsyncCallback(ReadMessageCallback), state);
                    }
                }
            } catch(Exception e)
            {
                if(DisconnectEvent != null)
                    DisconnectEvent(this);
                Debug.WriteLine(e.Message);
            }
        }
    }
}
