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
    public static class Utilities
    {
        public static void Receive_Exact(Socket socket, byte[] buffer, int offset, int size)
        {
            int received = 0;  // how many bytes is already received
            do
            {
                try
                {
                    received += socket.Receive(buffer, offset + received, size - received, SocketFlags.None);
                } catch(SocketException ex)
                {
                    if(ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                    } else
                        throw ex;  // any serious error occurr
                }
            } while(received < size);
        }
        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string SizeSuffix(Int64 value)
        {
            if(value <= 0)
                return "0 bytes";
            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }
    }
}
