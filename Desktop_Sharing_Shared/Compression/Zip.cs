using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Packaging;
using System.IO.Compression;
using System.Diagnostics;

namespace Desktop_Sharing_Shared.Compression
{
    public static class GZip
    {
        public static byte[] Compress(byte[] raw)
        {
            using(MemoryStream memory = new MemoryStream())
            {
                using(GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                Debug.WriteLine("Compressed Before: " + raw.Length + " to: " + memory.Length);
                return memory.ToArray();
            }
        }
        public static byte[] Decompress(byte[] gzip)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using(GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using(MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if(count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while(count > 0);
                    return memory.ToArray();
                }
            }
        }

    }
}
