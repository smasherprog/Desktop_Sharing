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

            public static implicit operator System.Drawing.Point(POINT point)
            {
                return new System.Drawing.Point(point.X, point.Y);
            }
        }
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static System.Drawing.Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
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
        //[return: MarshalAs(UnmanagedType.Bool)]
        //[System.Runtime.ConstrainedExecution.ReliabilityContract(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.MayFail)]
        //[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        //public static extern bool CloseWindowStation(IntPtr hWinsta);
        //public sealed class SafeWindowStationHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
        //{
        //    public SafeWindowStationHandle() : base(true) { }
        //    protected override bool ReleaseHandle() { return CloseWindowStation(handle); }
        //}
        //[DllImport("user32.dll", SetLastError = true)]
        //static extern bool SetProcessWindowStation(IntPtr hWinSta);
        //[DllImport("user32.dll", SetLastError = true)]
        //static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, ACCESS_MASK dwDesiredAccess);
        //[DllImport("user32.dll", SetLastError = true)]
        //static extern bool SetThreadDesktop(IntPtr hDesktop);
        //[DllImport("user32.dll", SetLastError = true)]
        //public static extern bool CloseDesktop(IntPtr hDesktop);
        //[DllImport("user32.dll", SetLastError = true)]
        //static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, ACCESS_MASK dwDesiredAccess);
        //[DllImport("user32.dll", SetLastError = true)]
        //static extern IntPtr GetThreadDesktop(uint dwThreadId);
        //[DllImport("kernel32.dll")]
        //static extern uint GetCurrentThreadId();

        //[System.Runtime.ConstrainedExecution.ReliabilityContract(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.MayFail)]
        //[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        //public static extern SafeWindowStationHandle OpenWindowStation([MarshalAs(UnmanagedType.LPTStr)] string lpszWinSta, [MarshalAs(UnmanagedType.Bool)]  bool fInherit, ACCESS_MASK dwDesiredAccess);
        //// Switches the current thread into a different desktop, by name
        //// Calling with a valid desktop name will place the thread in that desktop.
        //// Calling with a NULL name will place the thread in the current input desktop.
        //public static bool SelectDesktop(string name, ref IntPtr new_desktop)
        //{
        //    IntPtr desktop = IntPtr.Zero;
        //    if(!string.IsNullOrEmpty(name))
        //    {
        //        // Attempt to open the named desktop
        //        desktop = OpenDesktop(name, 0, false,
        //            ACCESS_MASK.DESKTOP_CREATEMENU | ACCESS_MASK.DESKTOP_CREATEWINDOW |
        //            ACCESS_MASK.DESKTOP_ENUMERATE | ACCESS_MASK.DESKTOP_HOOKCONTROL |
        //            ACCESS_MASK.DESKTOP_WRITEOBJECTS | ACCESS_MASK.DESKTOP_READOBJECTS |
        //            ACCESS_MASK.DESKTOP_SWITCHDESKTOP | ACCESS_MASK.GENERIC_WRITE);
        //    } else
        //    {
        //        // No, so open the input desktop
        //        desktop = OpenInputDesktop(0, false,
        //                ACCESS_MASK.DESKTOP_CREATEMENU | ACCESS_MASK.DESKTOP_CREATEWINDOW |
        //            ACCESS_MASK.DESKTOP_ENUMERATE | ACCESS_MASK.DESKTOP_HOOKCONTROL |
        //            ACCESS_MASK.DESKTOP_WRITEOBJECTS | ACCESS_MASK.DESKTOP_READOBJECTS |
        //            ACCESS_MASK.DESKTOP_SWITCHDESKTOP | ACCESS_MASK.GENERIC_WRITE);
        //    }
        //    if(desktop == IntPtr.Zero)
        //        return false;
        //    if(!SetThreadDesktop(desktop))
        //    {
        //       //Failed to enter the new desktop, so free it!
        //        CloseDesktop(desktop);
        //        return false;
        //    }
           
        //    if(new_desktop != IntPtr.Zero)
        //    {
        //        CloseDesktop(new_desktop);
        //        new_desktop = desktop;
        //    }
        //    return true;
        //}
        //private static IntPtr CurrentDesktop = IntPtr.Zero;

        //public static bool SetWinSta0Desktop(string szDesktopName)
        //{
        //    var bSuccess = false;

        //    var hWinSta0 = OpenWindowStation("WinSta0", false, ACCESS_MASK.MAXIMUM_ALLOWED);
        //    if(null == hWinSta0) { Debug.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message); }

        //    bSuccess = SetProcessWindowStation(hWinSta0.DangerousGetHandle());
        //    if(!bSuccess) { Debug.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message); }
        //    if(CurrentDesktop != IntPtr.Zero)
        //        CloseDesktop(CurrentDesktop);

        //    CurrentDesktop = OpenDesktop(szDesktopName, 0, false, ACCESS_MASK.MAXIMUM_ALLOWED);
        //    if(IntPtr.Zero == CurrentDesktop) { Debug.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message); }

        //    bSuccess = SetThreadDesktop(CurrentDesktop);
        //    if(!bSuccess) { Debug.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message); }

        //    if(hWinSta0 != null) { hWinSta0.Dispose(); }

        //    return bSuccess;
        //}

        //public static bool SetWinLogin()
        //{
        //    var bSuccess = false;
        //    var old_desktop = GetThreadDesktop(GetCurrentThreadId());

        //    IntPtr hwnd = OpenInputDesktop(0, false, ACCESS_MASK.DESKTOP_SWITCHDESKTOP);
        //    if(IntPtr.Zero == hwnd) { Debug.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message); }

        //    bSuccess = SetThreadDesktop(hwnd);
        //    if(!bSuccess) { Debug.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message); } else
        //        Debug.WriteLine("Changed Successfully");
        //    if(hwnd != null) { CloseDesktop(hwnd); }

        //    bSuccess = SetThreadDesktop(old_desktop);
        //    if(!bSuccess) { Debug.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message); } else
        //        Debug.WriteLine("Changed back Successfully");

        //    return bSuccess;
        //}
        //public static IntPtr GetInputDesktop()
        //{
        //    return OpenInputDesktop(0, false, ACCESS_MASK.MAXIMUM_ALLOWED);
        //}
        //public static IntPtr SwitchToInputDesktop(IntPtr olddesktop)
        //{
        //    var inp = GetInputDesktop();
        //    if(olddesktop == inp)
        //    {
        //        CloseDesktop(inp);
        //        return olddesktop;
        //    }
        //    CloseDesktop(olddesktop);

        //    SetThreadDesktop(inp);
        //    return inp;
        //}
 


    }
}
