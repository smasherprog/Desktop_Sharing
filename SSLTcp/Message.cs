using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SecureTcp
{
    public class Tcp_Message
    {
        public Tcp_Message(int t)
        {
            Blocks = new List<byte[]>();
            Add_Block(BitConverter.GetBytes(t));
        }
        public void Add_Block(byte[] b)
        {
            Blocks.Add(b);
        }

        public void Add_Blocks(params byte[][] list)
        {
            foreach(var item in list)
            {
                Add_Block(item);
            }

        }
        public long length
        {
            get
            {
                var t = 4 + (Blocks.Count * 4);
                foreach(var item in Blocks)
                    t += item.Length;
                return t;
            }
        }
        public static Tcp_Message FromBuffer(byte[] b)
        {
            var m = new Tcp_Message(1);
            m.Blocks.Clear();

            var curbuff = 0;
            var tempbuffer = new byte[4];
            Buffer.BlockCopy(b, curbuff, tempbuffer, 0, 4);
            curbuff += 4;
            var end = BitConverter.ToInt32(tempbuffer, 0);
            for(var i = 0; i < end; i++)
            {
                Buffer.BlockCopy(b, curbuff, tempbuffer, 0, 4);
                curbuff += 4;
                var tmpbuf = new byte[BitConverter.ToInt32(tempbuffer, 0)];
                Buffer.BlockCopy(b, curbuff, tmpbuf, 0, tmpbuf.Length);
                curbuff += tmpbuf.Length;
                m.Add_Block(tmpbuf);
            }

            return m;
        }
        public static byte[] ToBuffer(Tcp_Message m)
        {
            var sizeneeded = 4 + (m.Blocks.Count * 4);
            foreach(var item in m.Blocks)
                sizeneeded += item.Length;
            var inbuffer = new byte[sizeneeded];
            var curbuff = 0;
            Buffer.BlockCopy(BitConverter.GetBytes(m.Blocks.Count), 0, inbuffer, curbuff, 4);
            curbuff += 4;
            foreach(var item in m.Blocks)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(item.Length), 0, inbuffer, curbuff, 4);
                curbuff += 4;
                Buffer.BlockCopy(item, 0, inbuffer, curbuff, item.Length);
                curbuff += item.Length;
            }
            return inbuffer;
        }
        private static int roundUp(int numToRound, int multiple)
        {
            if(multiple == 0)
            {
                return numToRound;
            }

            int remainder = numToRound % multiple;
            if(remainder == 0)
            {
                return numToRound;
            }

            return numToRound + multiple - remainder;
        }
        public int Type
        {
            get
            { return BitConverter.ToInt32(Blocks[0], 0); }
            set
            { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Blocks[0], 0, 4); }
        }
        public List<byte[]> Blocks;

    }
}
