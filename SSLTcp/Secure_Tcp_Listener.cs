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
    public class HeaderObject
    {
        public Socket Client = null;
        public const int BufferSize = 4;
        public byte[] buffer = new byte[BufferSize];
        public int BytesRead = 0;
    }
    public class SessionKeyObject
    {
        public Socket Client = null;
        public int BufferSize = 0;
        public byte[] buffer = new byte[0];
        public int BytesRead = 0;
    }
    public class Secure_Tcp_Listener : IDisposable
    {
        private Socket SecureServer;
        private string _Keyfile;

        public ManualResetEvent allDone = new ManualResetEvent(false);

        public delegate void NewClientHandler(Secure_Stream client);
        public event NewClientHandler NewClient;

        private Thread _ListeningThread;
        private bool _Running = false;

        public Secure_Tcp_Listener(string key, int port)
        {
            SecureServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SecureServer.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
            _Keyfile = File.ReadAllText(key);
        }
        public void Dispose()
        {
            _Running = false;
            if(_ListeningThread != null)
            {
                allDone.WaitOne(50, true);
                _ListeningThread.Join(500);
            }
            _ListeningThread = null;

            if(SecureServer != null)
                SecureServer.Close();
            SecureServer = null;
        }
        public void StartListening()
        {
            _Running = true;
            _ListeningThread = new Thread(new ThreadStart(_StartListening));
            _ListeningThread.Start();

        }        
        private void _StartListening()
        {
            try{
                SecureServer.Listen(100);
                while(_Running)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();
                    // Start an asynchronous socket to listen for connections.
                    Debug.WriteLine("Waiting for a connection...");
                    SecureServer.BeginAccept( new AsyncCallback(AcceptCallback), SecureServer);
                  
                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }
        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();
            Debug.WriteLine("New connection received");

            // Get the socket that handles the client request.
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);

            // Create the state object.
            var state = new HeaderObject();
            state.Client = handler;

            handler.BeginReceive(state.buffer, 0, HeaderObject.BufferSize, 0, new AsyncCallback(ReadHeaderCallback), state);
        }
        public void ReadHeaderCallback(IAsyncResult ar)
        {
            Debug.WriteLine("ReadHeaderCallback");
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            var state = (HeaderObject)ar.AsyncState;
            var handler = state.Client;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if(bytesRead > 0)
            {
                state.BytesRead += bytesRead;
                if(bytesRead == HeaderObject.BufferSize)
                {
                    var keystate = new SessionKeyObject();
                    keystate.Client = handler;
                    keystate.BufferSize = BitConverter.ToInt32(state.buffer, 0);
                    if(keystate.BufferSize > 4000)
                        return;
                    keystate.buffer = new byte[keystate.BufferSize];
                    handler.BeginReceive(keystate.buffer, 0, keystate.BufferSize, 0, new AsyncCallback(ReadKeysCallback), keystate);
                } else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, state.BytesRead, HeaderObject.BufferSize - state.BytesRead, 0, new AsyncCallback(ReadHeaderCallback), state);
                }
            }
        }
        public void ReadKeysCallback(IAsyncResult ar)
        {
            Debug.WriteLine("ReadKeysCallback");
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            var state = (SessionKeyObject)ar.AsyncState;
            var handler = state.Client;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if(bytesRead > 0)
            {
                state.BytesRead += bytesRead;
                if(state.BytesRead == state.BufferSize)
                {//done receiving data 
                    SendSessionKey(state);
                } else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, state.BytesRead, state.BufferSize - state.BytesRead, 0, new AsyncCallback(ReadKeysCallback), state);
                }
            }
        }
        private void SendSessionKey(SessionKeyObject state)
        {
            Debug.WriteLine("SendSessionKey");
            try
            {
                using(RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(_Keyfile);
                    state.buffer = rsa.Decrypt(state.buffer, true);//decrypt the session key
                    //hash the key and send it back to prove that I received it correctly
                    byte[] sessionkeyhash = SHA256.Create().ComputeHash(state.buffer);

                    //send it back to the client
                    state.Client.Send(BitConverter.GetBytes(sessionkeyhash.Length));
                    state.Client.Send(sessionkeyhash);
                    Debug.WriteLine("Key Exchange completed!");
                    if(NewClient != null)//raise event
                        NewClient(new Secure_Stream(state.Client, state.buffer));
                }
            } 
            catch(Exception e)
            {
                state.Client.Close();
                Debug.WriteLine(e.Message);
            }
        }

        

        //public Secure_Stream AcceptTcpClient()
        //{

        //    var Client = SecureServer.AcceptTcpClient();
        //    Client.ReceiveTimeout = 5000;
        //    var sessionkey = ExchangeKeys(_KeyLocation, Client.GetStream());
        //    if(sessionkey == null)
        //    {
        //        Client.Close();
        //        Client = null;
        //        throw new ArgumentException("Key Exchange Failed");
        //    }
        //    return new Secure_Stream(Client, sessionkey);
        //}

        //public byte[] ExchangeKeys(string key_location, NetworkStream stream)
        //{
        //    try
        //    {
        //        using(RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
        //        {
        //            var keyfile = File.ReadAllText(key_location);
        //            rsa.FromXmlString(keyfile);

        //            var b = BitConverter.GetBytes(0);
        //            stream.Read(b, 0, b.Length);
        //            var len = BitConverter.ToInt32(b, 0);
        //            if(len > 4000)
        //                throw new ArgumentException("Buffer Overlflow in Encryption key exchange!");
        //            byte[] sessionkey = new byte[len];
        //            stream.Read(sessionkey, 0, len);

        //            sessionkey = rsa.Decrypt(sessionkey, true);//decrypt the session key
        //            //hash the key and send it back to prove that I received it correctly
        //            byte[] sessionkeyhash = SHA256.Create().ComputeHash(sessionkey);
        //            //send it back to the client
        //            byte[] intBytes = BitConverter.GetBytes(sessionkeyhash.Length);
        //            stream.Write(intBytes, 0, intBytes.Length);
        //            stream.Write(sessionkeyhash, 0, sessionkeyhash.Length);

        //            Debug.WriteLine("Key Exchange completed!");
        //            return sessionkey;

        //        }
        //    } catch(Exception e)
        //    {
        //        Debug.WriteLine(e.Message);
        //        return null;
        //    }

        //}
    }
}
