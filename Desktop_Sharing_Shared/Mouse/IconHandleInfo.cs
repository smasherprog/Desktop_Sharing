using Desktop_Sharing_Shared.Screen;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Desktop_Sharing_Shared.Mouse
{

    public class IconHandleInfo : IDisposable
    {
        public bool IsIcon { get; private set; }
        public Point Hotspot { get; private set; }
        public BitmapHandle MaskBitmap { get; private set; }
        public BitmapHandle ColorBitmap { get; private set; }

        internal IconHandleInfo(ICONINFO info)
        {
            this.IsIcon = info.fIcon;
            this.Hotspot = new Point(info.xHotspot, info.yHotspot);
            this.MaskBitmap = new BitmapHandle(info.hbmMask);
            this.ColorBitmap = new BitmapHandle(info.hbmColor);
        }

        public static IconHandleInfo GetIconInfo(IconHandle iconHandle)
        {
            if(iconHandle == null)
                return null;

            ICONINFO iconInfo;
            if(!PInvoke.GetIconInfo(iconHandle.Handle, out iconInfo))
                return null;
            return new IconHandleInfo(iconInfo);
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
                // free managed resources
                MaskBitmap.Dispose();
                ColorBitmap.Dispose();
            }
        }

    }
}
