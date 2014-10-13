using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Keyboard
{
    public class PInvoke
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int MapVirtualKey(int uCode, int uMapType);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        public enum PInvoke_KeyState : int
        {
            UP = 0x0002,
            DOWN = 0
        }

        public delegate void KeyEventHandler(int bVk, PInvoke_KeyState s);
        public static void KeyEvent(int bVk, PInvoke_KeyState s)
        {
            var scan = MapVirtualKey(bVk, 0);
            //Console.WriteLine("Received " + bVk + " in state " + s +  "  scan code is " + scan);
            keybd_event((byte)bVk, (byte)scan, (int)s, 0);
        }
    }
}
