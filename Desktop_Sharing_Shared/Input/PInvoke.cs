using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Input
{
    public static class PInvoke
    {
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
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;

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

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public delegate void MouseEventHandler(WinFormMouseEventFlags msg, int x, int y, int wheel_delta);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(PInvoke_MouseEventFlags dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

        public static void SendMouseEvent(WinFormMouseEventFlags msg, int x, int y, int wheel_delta)
        {

            var command = PInvoke_MouseEventFlags.LEFTDOWN;
            if(msg == WinFormMouseEventFlags.MOVE)
                command = PInvoke_MouseEventFlags.MOVE | PInvoke_MouseEventFlags.ABSOLUTE;
            else if(msg == WinFormMouseEventFlags.LEFTDOWN)
                command = PInvoke_MouseEventFlags.LEFTDOWN;
            else if(msg == WinFormMouseEventFlags.LEFTUP)
                command = PInvoke_MouseEventFlags.LEFTUP;
            else if(msg == WinFormMouseEventFlags.WHEEL)
                command = PInvoke_MouseEventFlags.WHEEL;
            else if(msg == WinFormMouseEventFlags.RIGHTUP)
                command = PInvoke_MouseEventFlags.RIGHTUP;
            else if(msg == WinFormMouseEventFlags.RIGHTDOWN)
                command = PInvoke_MouseEventFlags.RIGHTDOWN;
            else if(msg == WinFormMouseEventFlags.MIDDLEDOWN)
                command = PInvoke_MouseEventFlags.MIDDLEDOWN;
            else if(msg == WinFormMouseEventFlags.MIDDLEUP)
                command = PInvoke_MouseEventFlags.MIDDLEUP;
            else if(msg == WinFormMouseEventFlags.XDOWN)
                command = PInvoke_MouseEventFlags.XDOWN;
            else if(msg == WinFormMouseEventFlags.XUP)
                command = PInvoke_MouseEventFlags.XUP;


            mouse_event(command, (uint)x, (uint)y, (uint)wheel_delta, 0);

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
        public delegate void FileReceivedHandler(string filename, byte[] file);
        public static void FileEvent(string filename, byte[] file)
        {
            try
            {
                System.IO.File.WriteAllBytes(filename, file);
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }

        }
        public delegate void FolderReceivedHandler(string relativefolderpath);
        public static void FolderEvent(string relativefolderpath)
        {
            try
            {
                System.IO.Directory.CreateDirectory(relativefolderpath);
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }

        }

    }
}
