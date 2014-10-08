using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DesktopSharing_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new ScreenCaptureService();
            p.OnStart();
        }
    }
}
