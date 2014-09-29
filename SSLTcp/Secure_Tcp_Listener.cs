using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SecureTcp
{
    public class Secure_Tcp_Listener: IDisposable
    {
        private TcpListener SecureServer;
        private string _KeyLocation;
        public Secure_Tcp_Listener(string key, int port)
           
        {
            _KeyLocation = key;
            SecureServer = new TcpListener(System.Net.IPAddress.Any, port);
            SecureServer.Start();
        }
        public void Dispose()
        {
          
            if(SecureServer != null)
                SecureServer.Stop();
            SecureServer = null;
        }

        public Secure_Stream AcceptTcpClient()
        {
          
            var Client = SecureServer.AcceptTcpClient();
            Client.ReceiveTimeout = 5000;
            var sessionkey = ExchangeKeys(_KeyLocation, Client.GetStream());
            if(sessionkey == null)
            {
                Client.Close();
                Client = null;
                throw new ArgumentException("Key Exchange Failed");
            }
            return new Secure_Stream(Client, sessionkey);
        }

        public byte[] ExchangeKeys(string key_location, NetworkStream stream)
        {
            try
            {
               
                
                using(RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    var keyfile = File.ReadAllText(key_location);
                    rsa.FromXmlString(keyfile);

                    var b = BitConverter.GetBytes(0);
                    stream.Read(b, 0, b.Length);
                    var len = BitConverter.ToInt32(b, 0);
                    if(len > 4000)
                        throw new ArgumentException("Buffer Overlflow in Encryption key exchange!");
                    byte[] sessionkey =  new byte[len];
                    stream.Read(sessionkey, 0, len);

                    sessionkey = rsa.Decrypt(sessionkey, true);//decrypt the session key
                    //hash the key and send it back to prove that I received it correctly
                    byte[] sessionkeyhash = SHA256.Create().ComputeHash(sessionkey);
                    //send it back to the client
                    byte[] intBytes = BitConverter.GetBytes(sessionkeyhash.Length);
                    stream.Write(intBytes, 0, intBytes.Length);
                    stream.Write(sessionkeyhash, 0, sessionkeyhash.Length);

                    Debug.WriteLine("Key Exchange completed!");
                    return sessionkey;

                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }

        }
    }
}
