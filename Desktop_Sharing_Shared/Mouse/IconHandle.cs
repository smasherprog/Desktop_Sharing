using Desktop_Sharing_Shared.Mouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared
{

    public class IconHandle : IDisposable
    {
        public IntPtr Handle { get; private set; }
        public IconHandle(IntPtr handle)
        {
            Handle = handle;
        }
        public static IconHandle GetCursorIcon(CURSORINFO cursor)
        {
            IntPtr hIcon = PInvoke.CopyIcon(cursor.hCursor);

            if(hIcon == IntPtr.Zero)
                return null;

            return new IconHandle(hIcon);
        }
        public bool IsInvalid
        {
            get { return Handle == IntPtr.Zero; }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                PInvoke.DestroyIcon(Handle);
            }
        }

    }
}
