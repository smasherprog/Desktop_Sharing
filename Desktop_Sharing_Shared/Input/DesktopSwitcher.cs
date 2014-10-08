using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Input
{
    public class DesktopSwitcher
    {
        [Flags]
        private enum ACCESS_MASK : uint
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            SYNCHRONIZE = 0x00100000,

            STANDARD_RIGHTS_REQUIRED = 0x000F0000,

            STANDARD_RIGHTS_READ = 0x00020000,
            STANDARD_RIGHTS_WRITE = 0x00020000,
            STANDARD_RIGHTS_EXECUTE = 0x00020000,

            STANDARD_RIGHTS_ALL = 0x001F0000,

            SPECIFIC_RIGHTS_ALL = 0x0000FFFF,

            ACCESS_SYSTEM_SECURITY = 0x01000000,

            MAXIMUM_ALLOWED = 0x02000000,

            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000,

            DESKTOP_READOBJECTS = 0x00000001,
            DESKTOP_CREATEWINDOW = 0x00000002,
            DESKTOP_CREATEMENU = 0x00000004,
            DESKTOP_HOOKCONTROL = 0x00000008,
            DESKTOP_JOURNALRECORD = 0x00000010,
            DESKTOP_JOURNALPLAYBACK = 0x00000020,
            DESKTOP_ENUMERATE = 0x00000040,
            DESKTOP_WRITEOBJECTS = 0x00000080,
            DESKTOP_SWITCHDESKTOP = 0x00000100,

            WINSTA_ENUMDESKTOPS = 0x00000001,
            WINSTA_READATTRIBUTES = 0x00000002,
            WINSTA_ACCESSCLIPBOARD = 0x00000004,
            WINSTA_CREATEDESKTOP = 0x00000008,
            WINSTA_WRITEATTRIBUTES = 0x00000010,
            WINSTA_ACCESSGLOBALATOMS = 0x00000020,
            WINSTA_EXITWINDOWS = 0x00000040,
            WINSTA_ENUMERATE = 0x00000100,
            WINSTA_READSCREEN = 0x00000200,

            WINSTA_ALL_ACCESS = 0x0000037F
        }
        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetProcessWindowStation();
        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();
        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenWindowStation(string lpszWinSta, bool fInherit, ACCESS_MASK dwDesiredAccess);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessWindowStation(IntPtr hWinSta);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, ACCESS_MASK dwDesiredAccess);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr hDesktop);
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CloseWindowStation(IntPtr hWinsta);

        private IntPtr m_hCurWinsta = IntPtr.Zero;
        private IntPtr m_hCurDesktop = IntPtr.Zero;
        private IntPtr m_hWinsta = IntPtr.Zero;
        private IntPtr m_hDesk = IntPtr.Zero;

        public DesktopSwitcher()
        {
            m_hCurWinsta = IntPtr.Zero;
            m_hCurDesktop = IntPtr.Zero;
            m_hWinsta = IntPtr.Zero;
            m_hDesk = IntPtr.Zero;
        }
        public bool BeginInteraction()
        {
            using(var f = System.IO.File.CreateText(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\log.txt"))
            {
                try
                {


                    f.WriteLine("BeginInteraction");

                    EndInteraction(f);

                    m_hCurWinsta = GetProcessWindowStation();
                    if(m_hCurWinsta == IntPtr.Zero)
                    {
                        f.WriteLine("GetProcessWindowStation");
                        f.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                        return false;
                    }


                    m_hCurDesktop = GetDesktopWindow();
                    if(m_hCurDesktop == IntPtr.Zero)
                    {
                        f.WriteLine("GetDesktopWindow");
                        f.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                        return false;
                    }

                    m_hWinsta = OpenWindowStation("winsta0", false,
                        ACCESS_MASK.WINSTA_ENUMDESKTOPS |
                        ACCESS_MASK.WINSTA_READATTRIBUTES |
                        ACCESS_MASK.WINSTA_ACCESSCLIPBOARD |
                        ACCESS_MASK.WINSTA_CREATEDESKTOP |
                        ACCESS_MASK.WINSTA_WRITEATTRIBUTES |
                        ACCESS_MASK.WINSTA_ACCESSGLOBALATOMS |
                        ACCESS_MASK.WINSTA_EXITWINDOWS |
                        ACCESS_MASK.WINSTA_ENUMERATE |
                        ACCESS_MASK.WINSTA_READSCREEN);
                    if(m_hWinsta == IntPtr.Zero)
                    {
                        f.WriteLine("OpenWindowStation");
                        f.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                        return false;
                    }

                    if(!SetProcessWindowStation(m_hWinsta))
                    {
                        f.WriteLine("SetProcessWindowStation");
                        f.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                        return false;
                    }


                    m_hDesk = OpenDesktop("default", 0, false,
                            ACCESS_MASK.DESKTOP_CREATEMENU |
                            ACCESS_MASK.DESKTOP_CREATEWINDOW |
                            ACCESS_MASK.DESKTOP_ENUMERATE |
                            ACCESS_MASK.DESKTOP_HOOKCONTROL |
                            ACCESS_MASK.DESKTOP_JOURNALPLAYBACK |
                            ACCESS_MASK.DESKTOP_JOURNALRECORD |
                            ACCESS_MASK.DESKTOP_READOBJECTS |
                            ACCESS_MASK.DESKTOP_SWITCHDESKTOP |
                            ACCESS_MASK.DESKTOP_WRITEOBJECTS
                            );
                    if(m_hDesk == IntPtr.Zero)
                    {
                        f.WriteLine("OpenDesktop");
                        f.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                        return false;
                    }
                    if(!SetThreadDesktop(m_hDesk))
                    {
                        f.WriteLine("SetThreadDesktop");
                        f.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                        return false;
                    }
                   
                } catch(Exception e)
                {
                    f.WriteLine(e.Message);
                    return false;
                }
                return true;
            }
        }
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SwitchDesktop(IntPtr hDesktop);       
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, ACCESS_MASK dwDesiredAccess);
        public bool IsWorkstationLocked()
        {
            IntPtr hwnd = OpenInputDesktop(0, false, ACCESS_MASK.DESKTOP_SWITCHDESKTOP);

            if(hwnd == IntPtr.Zero)
            {
                // Could not get the input desktop, might be locked already?
                hwnd = OpenDesktop("Default", 0, false, ACCESS_MASK.DESKTOP_SWITCHDESKTOP);
            }

            // Can we switch the desktop?
            if(hwnd != IntPtr.Zero)
            {
                if(SwitchDesktop(hwnd))
                {
                    // Workstation is NOT LOCKED.
                    CloseDesktop(hwnd);
                } else
                {
                    CloseDesktop(hwnd);
                    // Workstation is LOCKED.
                    return true;
                }
            }

            return false;
        }
        public bool SelectDesktop(string name)
        {
            IntPtr desktop = IntPtr.Zero;
            if(!string.IsNullOrEmpty(name))
            {
                // Attempt to open the named desktop
                desktop = OpenDesktop(name, 0, false,
                    ACCESS_MASK.DESKTOP_CREATEMENU | ACCESS_MASK.DESKTOP_CREATEWINDOW |
                    ACCESS_MASK.DESKTOP_ENUMERATE | ACCESS_MASK.DESKTOP_HOOKCONTROL |
                    ACCESS_MASK.DESKTOP_WRITEOBJECTS | ACCESS_MASK.DESKTOP_READOBJECTS |
                    ACCESS_MASK.DESKTOP_SWITCHDESKTOP | ACCESS_MASK.GENERIC_WRITE);
            } else
            {
                // No, so open the input desktop
                desktop = OpenInputDesktop(0, false,
                        ACCESS_MASK.DESKTOP_CREATEMENU | ACCESS_MASK.DESKTOP_CREATEWINDOW |
                    ACCESS_MASK.DESKTOP_ENUMERATE | ACCESS_MASK.DESKTOP_HOOKCONTROL |
                    ACCESS_MASK.DESKTOP_WRITEOBJECTS | ACCESS_MASK.DESKTOP_READOBJECTS |
                    ACCESS_MASK.DESKTOP_SWITCHDESKTOP | ACCESS_MASK.GENERIC_WRITE);
            }
            if(desktop == IntPtr.Zero)
                return false;
            if(!SetThreadDesktop(desktop))
            {
                //Failed to enter the new desktop, so free it!
                CloseDesktop(desktop);
                return false;
            }

            if(m_hDesk != IntPtr.Zero)
            {
                CloseDesktop(m_hDesk);
                m_hDesk = desktop;
            }
            return true;
        }
        public void EndInteraction(StreamWriter f)
        {
            f.WriteLine("EndInteraction");
            if(m_hCurWinsta != IntPtr.Zero)
                SetProcessWindowStation(m_hCurWinsta);
            f.WriteLine("SetProcessWindowStation");
            if(m_hCurDesktop != IntPtr.Zero)
                SetThreadDesktop(m_hCurDesktop);
            f.WriteLine("SetThreadDesktop");
            if(m_hWinsta != IntPtr.Zero)
                CloseWindowStation(m_hWinsta);
            f.WriteLine("CloseWindowStation");
            if(m_hDesk != IntPtr.Zero)
                CloseDesktop(m_hDesk);
            f.WriteLine("CloseDesktop");
        }
    }
}
