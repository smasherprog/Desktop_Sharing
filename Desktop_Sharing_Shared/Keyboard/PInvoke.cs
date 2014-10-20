using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Keyboard
{
    public struct KeyboardEventStruct
    {
        public int bVk;
        public Desktop_Sharing_Shared.Keyboard.PInvoke.PInvoke_KeyState s;
    }
    public static class PInvoke
    {
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int MapVirtualKey(int uCode, int uMapType);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        public enum PInvoke_KeyState : int
        {
            UP = 0x0002,
            DOWN = 0
        }

        public delegate void KeyEventHandler(KeyboardEventStruct k);
        public static void KeyEvent(KeyboardEventStruct k)
        {
            var scan = MapVirtualKey(k.bVk, 0);
            //Console.WriteLine("Received " + bVk + " in state " + s +  "  scan code is " + scan);
            keybd_event((byte)k.bVk, (byte)scan, (int)k.s, 0);
        }
    }
}
