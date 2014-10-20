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
using System.Threading;

namespace SecureTcp
{

    public class Secure_Tcp_Listener : IDisposable
    {
        private Socket SecureServer;
        private string _SessionKey;

        public delegate void ClientEventHandler(Secure_Stream client);
        public event ClientEventHandler NewClientEvent;
        private int _Port;

        public Secure_Tcp_Listener(string key, int port)
        {
            _Port = port;
            _SessionKey = File.ReadAllText(key);
        }
        public void Dispose()
        {
            if(SecureServer != null)
                SecureServer.Close();
            SecureServer = null;
        }
        public void Start()
        {
            SecureServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SecureServer.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, _Port));
            SecureServer.Listen(100);
            _BeginAccept();
        }

        private void _BeginAccept()
        {
            SecureServer.BeginAccept(new AsyncCallback(AcceptCallback), SecureServer);
        }
        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Debug.WriteLine("New connection received");

                var listener = (Socket)ar.AsyncState;
                var handler = listener.EndAccept(ar);
                handler.NoDelay = true;
                var sessionkey = ExchangeKeys(_SessionKey, handler);
                if(sessionkey == null)
                {
                    handler.Close();
                    handler = null;
                }
                if(NewClientEvent != null)
                    NewClientEvent(new Secure_Stream(handler, sessionkey));
                
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            _BeginAccept();
        }

        private static byte[] ExchangeKeys(string sesskey, Socket sock)
        {
            try
            {
                using(RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(sesskey);
                    var b = BitConverter.GetBytes(0);
                    Utilities.Receive_Exact(sock, b, 0, b.Length);
                    var len = BitConverter.ToInt32(b, 0);
                    if(len > 4000)
                        throw new ArgumentException("Buffer Overlflow in Encryption key exchange!");
                    byte[] sessionkey = new byte[len];

                    Utilities.Receive_Exact(sock, sessionkey, 0, len);
                    sessionkey = rsa.Decrypt(sessionkey, true);//decrypt the session key
                    //hash the key and send it back to prove that I received it correctly
                    byte[] sessionkeyhash = SHA256.Create().ComputeHash(sessionkey);
                    //send it back to the client
                    byte[] intBytes = BitConverter.GetBytes(sessionkeyhash.Length);
                    sock.Send(intBytes);
                    sock.Send(sessionkeyhash);
          
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
