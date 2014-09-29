using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Desktop_Sharing_Shared.Utilities
{
    public class Format
    {
        public static string FormatBytes(long bytes)
        {
            const long scale = 1024;
            string[] orders = new string[] { "YB", "ZB", "EB", "PB", "TB", "GB", "MB", "KB", "Bytes" };
            var max = (long)Math.Pow(scale, (orders.Length - 1));
            foreach(string order in orders)
            {
                if(bytes > max)
                    return string.Format("{0:##.##} {1}", Decimal.Divide(bytes, max), order);
                max /= scale;
            }
            return "0 Bytes";
        }
    }
}
