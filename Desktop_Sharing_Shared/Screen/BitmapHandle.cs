using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Screen
{

    public class BitmapHandle : IDisposable
    {
        public IntPtr Handle { get; private set; }
        public BitmapHandle(IntPtr handle)
        {
            Handle = handle;
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
                PInvoke.DeleteObject(Handle);
            }
        }

    }
}
