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
        public static Secure_Stream Connect(string key_location, string ipaddr, int port)
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.ReceiveTimeout = 5000;
            client.Connect(new IPEndPoint(IPAddress.Parse(ipaddr), port));
            var sessionkey = ExchangeKeys(key_location, client);
            if(sessionkey == null)
            {
                client.Close();
                client = null;
                throw new ArgumentException("Key Exchange Failed");
            }
            return new Secure_Stream(client, sessionkey);
        }

        private static byte[] ExchangeKeys(string key_location, Socket socket)
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
                    socket.Send(intBytes);
                    socket.Send(data);
  
                    //read the sessionkeyhash response from the server to ensure it received it correctly
                    var b = BitConverter.GetBytes(0);
                    socket.Receive(b, b.Length, SocketFlags.None);
                    var len = BitConverter.ToInt32(b, 0);
                    if(len > 4000)
                        throw new ArgumentException("Buffer Overlflow in Encryption key exchange!");
                    var serversessionkeyhash = new byte[len];
                    socket.Receive(serversessionkeyhash, serversessionkeyhash.Length, SocketFlags.None);

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
