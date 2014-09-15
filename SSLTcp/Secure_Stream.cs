using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SecureTcp
{
    public class Secure_Stream: IDisposable
    {
        public TcpClient Client;
        byte[] _MySessionKey;
        private int _Buffer_Size = 1024 * 1024 * 8;//dont want to reallocate large chunks of memory if I dont have to
        byte[] _Buffer;
        public Secure_Stream(TcpClient c, byte[] sessionkey)
        {
            Client = c;
            _Buffer = new byte[_Buffer_Size];// 8 megabytes buffer
            _MySessionKey = sessionkey;
        }
        public void Dispose()
        {

            if(Client != null)
                Client.Close();
            Client = null;
        }
        public void Encrypt_And_Send(byte[] data)
        {
            Write(data, Client.GetStream());
        }
        public byte[] Read_And_Unencrypt()
        {
            return Read(Client.GetStream());
        }

        protected void Write(byte[] data, NetworkStream stream)
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
                        var encryptedbytes = encrypt.TransformFinalBlock(data, 0, data.Length);
                        var len = BitConverter.GetBytes(encryptedbytes.Length);
               
                        stream.Write(len, 0, len.Length);
                        stream.Write(encryptedbytes, 0, encryptedbytes.Length);
                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        protected byte[] Read(NetworkStream stream)
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
                
                            return decrypt.TransformFinalBlock(_Buffer, 0, len);
                        }
                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return new byte[0];
        }
    }
}
