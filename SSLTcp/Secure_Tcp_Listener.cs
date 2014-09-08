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
using System.Threading.Tasks;

namespace SSLTcp
{
    public class Secure_Tcp_Listener : Secure_Base_Tcp, IDisposable
    {
        private TcpListener sslServer;
        private TcpClient Client;

        public Secure_Tcp_Listener(string key, int port):base(key)
        {
            Client = null;
            sslServer = new TcpListener(System.Net.IPAddress.Any, port);
            sslServer.Start();
        }
        public void Dispose()
        {
            if(Client != null)
                Client.Close();
            if(sslServer != null)
                sslServer.Stop();
            sslServer = null;
            Client = null;
        }

        public TcpClient AcceptTcpClient()
        {
            Client = sslServer.AcceptTcpClient();
            Client.ReceiveTimeout = 5000;
            if(!ExchangeKeys(Client.GetStream()))
            {
                Client.Close();
                Client = null;
            }
            return Client;
        }
        public void Write(byte[] data)
        {
            Write(data, Client.GetStream());
        }
        public byte[] Read()
        {
            return Read(Client.GetStream());
        }
    }
}
