using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Mouse
{

    public class DeviceHandle : SafeHandle
    {

        public IntPtr Handle { get; private set; }
        public DeviceHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            Handle = handle;
        }
        public static DeviceHandle CreateCompatibleDC(Graphics graphics)
        {
            if(graphics == null)
                throw new ArgumentNullException("graphics");

            IntPtr source = graphics.GetHdc();

            IntPtr handle = PInvoke.CreateCompatibleDC(source);
            DeviceHandle dc = new DeviceHandle(handle);

            graphics.ReleaseHdc(source);

            return dc;
        }
        public override bool IsInvalid
        {
            get { return Handle == IntPtr.Zero; }
        }
        protected override bool ReleaseHandle()
        {
            return PInvoke.DeleteDC(Handle);
        }
    }
}
