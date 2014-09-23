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
        public event Desktop_Sharing_Shared.Input.Constants.MouseEventHandler InputMouseEvent;
        private IntPtr Handle;
        public InputListener(IntPtr handle)
        {
            Handle = handle;
        }

        public bool PreFilterMessage(ref System.Windows.Forms.Message m)
        {
       
            if(Handle == m.HWnd && ((int[])Enum.GetValues(typeof(Desktop_Sharing_Shared.Input.Constants.WinFormMouseEventFlags))).Contains(m.Msg))
            {
                if(InputMouseEvent != null)
                {
                    var p = GetPoint(m.LParam);
                    var wheel= 0;
                    if(m.Msg == Desktop_Sharing_Shared.Input.Constants.WM_MOUSEWHEEL)
                    {
                        uint xy = unchecked(IntPtr.Size == 8 ? (uint)m.WParam.ToInt64() : (uint)m.WParam.ToInt32());
                        wheel = unchecked((short)(xy >> 16));
                    }
                    InputMouseEvent((Desktop_Sharing_Shared.Input.Constants.WinFormMouseEventFlags)m.Msg, p.X, p.Y, wheel);
                }
                Debug.WriteLine("Mouse Event");
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
