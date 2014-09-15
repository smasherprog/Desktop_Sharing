using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SecureTcp
{
    public static class Secure_Tcp_Client
    {
        public static Secure_Stream Connect(string key_location, string ip, int port)
        {
            var secureclient = new TcpClient(ip, port);
            secureclient.ReceiveTimeout = 5000;
            var sessionkey = ExchangeKeys(key_location, secureclient.GetStream());
            if(sessionkey == null)
            {
                secureclient.Close();
                secureclient = null;
                throw new ArgumentException("Key Exchange Failed");
            }
            return new Secure_Stream(secureclient, sessionkey);
        }

        private static byte[] ExchangeKeys(string key_location, NetworkStream stream)
        {
            try
            {
                byte[] sessionkey = null;
                using(Aes aes = Aes.Create())
                {
                    aes.KeySize = aes.LegalKeySizes[0].MaxSize;
                    sessionkey = aes.Key;
                }
                byte[] sessionkeyhash = SHA256.Create().ComputeHash(sessionkey);

                using(RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    var keyfile = File.ReadAllText(key_location);
                    rsa.FromXmlString(keyfile);
                    //encrypt the session key with the public key
                    var data = rsa.Encrypt(sessionkey, true);
                    //send it with the length
                    byte[] intBytes = BitConverter.GetBytes(data.Length);
                    stream.Write(intBytes, 0, intBytes.Length);
                    stream.Write(data, 0, data.Length);

                    //read the sessionkeyhash response from the server to ensure it received it correctly
                    var b = BitConverter.GetBytes(0);
                    stream.Read(b, 0, b.Length);
                    var len = BitConverter.ToInt32(b, 0);
                    if(len > 4000)
                        throw new ArgumentException("Buffer Overlflow in Encryption key exchange!");
                    var serversessionkeyhash = new byte[len];
                    stream.Read(serversessionkeyhash, 0, len);

                    //compare the sessionhash returned by the server to our hash
                    if(serversessionkeyhash.SequenceEqual(sessionkeyhash))
                    {
                        Debug.WriteLine("Key Exchange completed!");
                        return sessionkey;
                    } else
                    {
                        Debug.WriteLine("Key Exchange failed keys are not the same!");
                        return null;
                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }

        }

    }
}
