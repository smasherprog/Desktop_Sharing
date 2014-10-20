using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Mouse
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ICONINFO
    {
        public bool fIcon;         // Specifies whether this structure defines an icon or a cursor. A value of TRUE specifies 
        public Int32 xHotspot;     // Specifies the x-coordinate of a cursor's hot spot. If this structure defines an icon, the hot 
        public Int32 yHotspot;     // Specifies the y-coordinate of the cursor's hot spot. If this structure defines an icon, the hot 
        public IntPtr hbmMask;     // (HBITMAP) Specifies the icon bitmask bitmap. If this structure defines a black and white icon, 
        public IntPtr hbmColor;    // (HBITMAP) Handle to the icon color bitmap. This member can be optional if this 
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public Int32 x;
        public Int32 y;
    }
    
    public struct MouseEventStruct{
        public Desktop_Sharing_Shared.Mouse.PInvoke.WinFormMouseEventFlags msg;
        public int x, y, wheel_delta;
    }
 
    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO
    {
        public Int32 cbSize;        // Specifies the size, in bytes, of the structure. 
        public Int32 flags;         // Specifies the cursor state. This parameter can be one of the following values:
        public IntPtr hCursor;          // Handle to the cursor. 
        public POINT ptScreenPos;       // A POINT structure that receives the screen coordinates of the cursor. 
    }
    public static class PInvoke
    {
        public const Int32 CURSOR_SHOWING = 0x00000001;

        [DllImport("user32.dll", EntryPoint = "GetCursorInfo")]
        public static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll", EntryPoint = "CopyIcon")]
        public static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll", EntryPoint = "GetIconInfo")]
        public static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr hdc);

        public const int WM_MOUSEMOVE = 512;
        public const int WM_LBUTTONDOWN = 513;
        public const int WM_LBUTTONUP = 514;
        public const int WM_LBUTTONDBLCLK = 515;
        public const int WM_RBUTTONDOWN = 516;
        public const int WM_RBUTTONUP = 517;
        public const int WM_RBUTTONDBLCLK = 518;
        public const int WM_MBUTTONDOWN = 519;
        public const int WM_MBUTTONUP = 520;
        public const int WM_MBUTTONDBLCLK = 521;
        public const int WM_MOUSEWHEEL = 522;

        public enum WinFormMouseEventFlags : int
        {
            LEFTDOWN = WM_LBUTTONDOWN,
            LEFTUP = WM_LBUTTONUP,
            LEFTDCLICK = WM_LBUTTONDBLCLK,
            MIDDLEDOWN = WM_MBUTTONDOWN,
            MIDDLEUP = WM_MBUTTONUP,
            MIDDLEDCLICK = WM_MBUTTONDBLCLK,
            MOVE = WM_MOUSEMOVE,
            RIGHTDOWN = WM_RBUTTONDOWN,
            RIGHTUP = WM_RBUTTONUP,
            RIGHTDCLICK = WM_RBUTTONDBLCLK,
            WHEEL = WM_MOUSEWHEEL,
            XDOWN = WM_LBUTTONDOWN,
            XUP = WM_LBUTTONUP
        }


        public delegate void MouseEventHandler(MouseEventStruct m);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(PInvoke_MouseEventFlags dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

        public static void SendMouseEvent(MouseEventStruct m)
        {
            var command = PInvoke_MouseEventFlags.LEFTDOWN;
            if(m.msg == WinFormMouseEventFlags.MOVE)
                command = PInvoke_MouseEventFlags.MOVE | PInvoke_MouseEventFlags.ABSOLUTE;
            else if(m.msg == WinFormMouseEventFlags.LEFTDOWN)
                command = PInvoke_MouseEventFlags.LEFTDOWN;
            else if(m.msg == WinFormMouseEventFlags.LEFTUP)
                command = PInvoke_MouseEventFlags.LEFTUP;
            else if(m.msg == WinFormMouseEventFlags.WHEEL)
                command = PInvoke_MouseEventFlags.WHEEL;
            else if(m.msg == WinFormMouseEventFlags.RIGHTUP)
                command = PInvoke_MouseEventFlags.RIGHTUP;
            else if(m.msg == WinFormMouseEventFlags.RIGHTDOWN)
                command = PInvoke_MouseEventFlags.RIGHTDOWN;
            else if(m.msg == WinFormMouseEventFlags.MIDDLEDOWN)
                command = PInvoke_MouseEventFlags.MIDDLEDOWN;
            else if(m.msg == WinFormMouseEventFlags.MIDDLEUP)
                command = PInvoke_MouseEventFlags.MIDDLEUP;
            else if(m.msg == WinFormMouseEventFlags.XDOWN)
                command = PInvoke_MouseEventFlags.XDOWN;
            else if(m.msg == WinFormMouseEventFlags.XUP)
                command = PInvoke_MouseEventFlags.XUP;


            mouse_event(command, (uint)m.x, (uint)m.y, (uint)m.wheel_delta, 0);

            // Console.WriteLine("Received Mouse event " + command + " " + ((uint)x) + " " + ((uint)y) + " " + ((uint)wheel_delta));
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
