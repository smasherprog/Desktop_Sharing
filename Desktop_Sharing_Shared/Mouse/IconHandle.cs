using Desktop_Sharing_Shared.Mouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared
{

    public class IconHandle : SafeHandle
    {
        public IntPtr Handle { get; private set; }
        public IconHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
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
        public override bool IsInvalid
        {
            get { return Handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            return PInvoke.DestroyIcon(Handle);
        }
    }
}
