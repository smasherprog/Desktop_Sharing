using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SecureTcp
{

    public class Secure_Stream : IDisposable
    {
        public TcpClient Client;
        byte[] _MySessionKey;
        private int _Buffer_Size = 1024 * 1024 * 8;//dont want to reallocate large chunks of memory if I dont have to
        byte[] _Buffer;
        public long Received_Total;
        public long Sent_Total;
        private long _Last_Received_BPS;
        public long Received_BPS;
        private long _Last_Sent_BPS;
        public long Sent_BPS;

        private DateTime _SecondCounter = DateTime.Now;

        public Secure_Stream(TcpClient c, byte[] sessionkey)
        {
            Client = c;
            _Buffer = new byte[_Buffer_Size];// 8 megabytes buffer
            _MySessionKey = sessionkey;
            Sent_BPS = Received_BPS=Sent_Total = Received_Total = 0;
        }
        public void Dispose()
        {
            if(Client != null)
                Client.Close();
            Client = null;
        }
        public void Encrypt_And_Send(Tcp_Message m)
        {

            Write(m, Client.GetStream()); 
            var l = m.length;
            _Last_Sent_BPS += l;
            Sent_Total += l;
            UpdateCounters();
        }
        public Tcp_Message Read_And_Unencrypt()
        {
            var r= Read(Client.GetStream());
            var l = r.length;
            Received_Total += l;
            _Last_Received_BPS += l;
            UpdateCounters();
            return r;
        }
        private void UpdateCounters()
        {
            if((DateTime.Now - _SecondCounter).TotalMilliseconds > 1000)
            {
                Sent_BPS= _Last_Sent_BPS;
                Received_BPS = _Last_Received_BPS;
                _Last_Received_BPS = _Last_Sent_BPS = 0;
                _SecondCounter = DateTime.Now;
            }
        }
        protected void Write(Tcp_Message m, NetworkStream stream)
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

                    var iv = aes.IV;
                    stream.Write(iv, 0, iv.Length);
                    using(ICryptoTransform encrypt = aes.CreateEncryptor())
                    {
                        var sendbuffer= Tcp_Message.ToBuffer(m);
                        var encryptedbytes = encrypt.TransformFinalBlock(sendbuffer, 0, sendbuffer.Length);

                        stream.Write(BitConverter.GetBytes(encryptedbytes.Length), 0, 4);
                        stream.Write(encryptedbytes, 0, encryptedbytes.Length);
                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        protected Tcp_Message Read(NetworkStream stream)
        {
            try
            {
                if(stream.DataAvailable)
                {
                    var iv = new byte[16];
                    stream.Read(iv, 0, 16);

                    var b = BitConverter.GetBytes(0);
                    stream.Read(b, 0, b.Length);
                    var len = BitConverter.ToInt32(b, 0);
                    using(AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                    {
                        aes.KeySize = _MySessionKey.Length * 8;
                        aes.Key = _MySessionKey;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        int readbytes = 0;
                        while(readbytes < len)
                        {
                            readbytes += stream.Read(_Buffer, readbytes, len);
                        }
                        using(ICryptoTransform decrypt = aes.CreateDecryptor())
                        {
                            var arrybuf = decrypt.TransformFinalBlock(_Buffer, 0, len);
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
    }
}
