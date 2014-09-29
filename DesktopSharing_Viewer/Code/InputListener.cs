using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace DesktopSharing_Viewer.Code
{
    public class InputListener : System.Windows.Forms.IMessageFilter
    {
        public event Desktop_Sharing_Shared.Input.PInvoke.MouseEventHandler InputMouseEvent;
        public event Desktop_Sharing_Shared.Input.PInvoke.KeyEventHandler InputKeyEvent;
        private DateTime SecondCounter = DateTime.Now;

        private const int InputPerSec = 30;

        private IntPtr Handle;
        public InputListener(IntPtr handle)
        {
            Handle = handle;
        }

        public bool PreFilterMessage(ref System.Windows.Forms.Message m)
        {

            if(Handle == m.HWnd)
            {
                if(m.Msg == Desktop_Sharing_Shared.Input.PInvoke.WM_KEYDOWN || m.Msg == Desktop_Sharing_Shared.Input.PInvoke.WM_KEYUP)
                {
                    if(InputKeyEvent != null)
                    {
                        if((DateTime.Now - SecondCounter).TotalMilliseconds < InputPerSec)
                            return false;
                        else
                            SecondCounter = DateTime.Now;
                        InputKeyEvent(unchecked(IntPtr.Size == 8 ? (int)m.WParam.ToInt64() : (int)m.WParam.ToInt32()), m.Msg == Desktop_Sharing_Shared.Input.PInvoke.WM_KEYDOWN ? Desktop_Sharing_Shared.Input.PInvoke.PInvoke_KeyState.DOWN : Desktop_Sharing_Shared.Input.PInvoke.PInvoke_KeyState.UP);
                        return false;
                    }
                }
                if(InputMouseEvent != null && ((int[])Enum.GetValues(typeof(Desktop_Sharing_Shared.Input.PInvoke.WinFormMouseEventFlags))).Contains(m.Msg))
                {
                    if(m.Msg == Desktop_Sharing_Shared.Input.PInvoke.WM_MOUSEMOVE)
                    {
                        if((DateTime.Now - SecondCounter).TotalMilliseconds < InputPerSec)
                            return false;
                        else
                            SecondCounter = DateTime.Now;
                    }
                    var p = GetPoint(m.LParam);
                    var wheel = 0;
                    if(m.Msg == Desktop_Sharing_Shared.Input.PInvoke.WM_MOUSEWHEEL)
                    {
                        uint xy = unchecked(IntPtr.Size == 8 ? (uint)m.WParam.ToInt64() : (uint)m.WParam.ToInt32());
                        wheel = unchecked((short)(xy >> 16));
                    }
                    InputMouseEvent((Desktop_Sharing_Shared.Input.PInvoke.WinFormMouseEventFlags)m.Msg, p.X, p.Y, wheel);
                }
                // Debug.WriteLine("Mouse Event");
            }
            return false;
        }
        private int GetInt(IntPtr ptr)
        {
            return IntPtr.Size == 8 ? unchecked((int)ptr.ToInt64()) : ptr.ToInt32();
        }
        private Point GetPoint(IntPtr _xy)
        {
            uint xy = unchecked(IntPtr.Size == 8 ? (uint)_xy.ToInt64() : (uint)_xy.ToInt32());
            int x = unchecked((short)xy);
            int y = unchecked((short)(xy >> 16));
            return new Point(x, y);
        }
    }
}
