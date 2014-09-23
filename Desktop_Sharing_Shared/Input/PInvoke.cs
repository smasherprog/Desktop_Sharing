using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Input
{
    public static class PInvoke
    {
        public readonly static int WM_MOUSEMOVE = 512;
        public readonly static int WM_LBUTTONDOWN = 513;
        public readonly static int WM_LBUTTONUP = 514;
        public readonly static int WM_LBUTTONDBLCLK = 515;
        public readonly static int WM_RBUTTONDOWN = 516;
        public readonly static int WM_RBUTTONUP = 517;
        public readonly static int WM_RBUTTONDBLCLK = 518;
        public readonly static int WM_MBUTTONDOWN = 519;
        public readonly static int WM_MBUTTONUP = 520;
        public readonly static int WM_MBUTTONDBLCLK = 521;
        public readonly static int WM_MOUSEWHEEL = 522;
        public enum WinFormMouseEventFlags : int
        {
            LEFTDOWN = WM_LBUTTONDOWN,
            LEFTUP = WM_LBUTTONUP,
            MIDDLEDOWN = WM_MBUTTONDOWN,
            MIDDLEUP = WM_MBUTTONUP,
            MOVE = WM_MOUSEMOVE,
            RIGHTDOWN = WM_RBUTTONDOWN,
            RIGHTUP = WM_RBUTTONUP,
            WHEEL = WM_MOUSEWHEEL,
            XDOWN = WM_LBUTTONDOWN,
            XUP = WM_LBUTTONUP
        }

        public delegate void MouseEventHandler(WinFormMouseEventFlags msg, int x, int y, int wheel_delta);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(PInvoke_MouseEventFlags dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

        public static void SendMouseEvent(WinFormMouseEventFlags msg, int x, int y, int wheel_delta)
        {
          
        }
        [Flags]
        public enum PInvoke_MouseEventFlags : uint
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010,
            WHEEL = 0x00000800,
            XDOWN = 0x00000080,
            XUP = 0x00000100,
 
        }
    }
}
