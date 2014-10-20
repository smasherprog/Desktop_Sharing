using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SecureTcp
{
    public class TCP_Server : IDisposable
    {

        private AutoResetEvent allDone = new AutoResetEvent(false);
        private object _ClientLock = new object();
        private List<Secure_Stream> _Clients = new List<Secure_Stream>();
        public int ClientCount = 0;
        private Secure_Tcp_Listener _Secure_Tcp_Listener;

        public delegate void ReceiveHandler(Tcp_Message m);
        public event ReceiveHandler ReceiveEvent;
        public event SecureTcp.Secure_Tcp_Listener.ClientEventHandler NewClientEvent;
        public event SecureTcp.Secure_Stream.DisconnectEventHandler DisconnectEvent;

        private string _Key;
        private int _Port;
        public TCP_Server(string key, int port)
        {
            _Key = key;
            _Port = port;

        }
        private void _Secure_Tcp_Listener_NewClient(Secure_Stream client)
        {
            lock(_ClientLock)
            {
                _Clients.Add(client);
            }
            ClientCount += 1;
            if(NewClientEvent != null)
                NewClientEvent(client);
            client.MessageReceivedEvent += client_MessageReceivedEvent;
            client.DisconnectEvent += client_DisconnectEvent;
            client.BeginRead();
        }

        void client_DisconnectEvent(Secure_Stream client)
        {
            ClientCount -= 1;
            if(DisconnectEvent != null)
                DisconnectEvent(client);
            client.Dispose();
        }

        void client_MessageReceivedEvent(Secure_Stream client, Tcp_Message ms)
        {
            if(ReceiveEvent != null)
                ReceiveEvent(ms);
        }
        public void Start()
        {
            _Secure_Tcp_Listener = new SecureTcp.Secure_Tcp_Listener(_Key, _Port);
            _Secure_Tcp_Listener.NewClientEvent += _Secure_Tcp_Listener_NewClient;
            ClientCount = 0;
            _Secure_Tcp_Listener.Start();
        
        }

        public void Stop()
        {

            lock(_ClientLock)
            {
                foreach(var item in _Clients)
                    item.Dispose();
                _Clients.Clear();
            }

            if(_Secure_Tcp_Listener != null)
                _Secure_Tcp_Listener.Dispose();
            _Secure_Tcp_Listener = null;
            ClientCount = 0;
        }
        public void Send(Tcp_Message m)
        {
            lock(_ClientLock)
            {
                _Clients.RemoveAll(a => a.Client==null);
                foreach(var item in _Clients)
                    item.Encrypt_And_Send(m);
            }
        }
        public void Dispose()
        {
            Stop();
        }
    }
}
