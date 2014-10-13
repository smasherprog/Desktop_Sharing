using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Mouse
{

    public class BitmapHandle : SafeHandle
    {
        public IntPtr Handle { get; private set; }
        public BitmapHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            Handle = handle;
        }
        public override bool IsInvalid
        {
            get { return Handle == IntPtr.Zero; }
        }
        protected override bool ReleaseHandle()
        {
            return PInvoke.DeleteObject(Handle);
        }
    }
}
