using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Mouse
{

    public class MouseCapture : IDisposable
    {
        private DeviceHandle mask;
        private int cursorInfoSize;
        private IntPtr LastCursor = IntPtr.Zero;//dont need to dispose, just a copy of a pointer

        public delegate void MousePositionChangedHandler(Point tl);
        public event MousePositionChangedHandler MousePositionChangedEvent;

        public delegate void MouseImageChangedHandler(Point tl, byte[] data);
        public event MouseImageChangedHandler MouseImageChangedEvent;

        public MouseCapture()
        {
            IntPtr desk = PInvoke.GetDesktopWindow();
            using(Graphics desktop = Graphics.FromHwnd(desk))
                mask = DeviceHandle.CreateCompatibleDC(desktop);

            cursorInfoSize = Marshal.SizeOf(typeof(CURSORINFO));

        }

        public void Update()
        {

            CURSORINFO cursorInfo;
            cursorInfo.cbSize = cursorInfoSize;

            if(!PInvoke.GetCursorInfo(out cursorInfo))
                return;

            if(cursorInfo.flags != PInvoke.CURSOR_SHOWING)// same cursor as previous
                return;

            using(IconHandle hicon = IconHandle.GetCursorIcon(cursorInfo))
            using(IconHandleInfo iconInfo = IconHandleInfo.GetIconInfo(hicon))
            {
                if(iconInfo == null)
                    return;
                var tmpx = _Position.X;
                var tmpy = _Position.Y;
                _Position.X = cursorInfo.ptScreenPos.x - iconInfo.Hotspot.X;
                _Position.Y = cursorInfo.ptScreenPos.y - iconInfo.Hotspot.Y;
                if(LastCursor == cursorInfo.hCursor)
                {
                    if(MousePositionChangedEvent != null && tmpx != _Position.X && tmpy != _Position.Y)
                    {
                        MousePositionChangedEvent(_Position);
                    }
                    return;//no more work here, the cursor is the same
                }
                LastCursor = cursorInfo.hCursor;//just a copy
                Bitmap resultBitmap = null;

                using(Bitmap bitmapMask = Bitmap.FromHbitmap(iconInfo.MaskBitmap.Handle))
                {
                    // Here we have to determine if the current cursor is monochrome in order
                    // to do a proper processing. If we just extracted the cursor icon from
                    // the icon handle, monochrome cursors would appear garbled.

                    if(bitmapMask.Height == bitmapMask.Width * 2)
                    {
                        // Yes, this is a monochrome cursor. We will have to manually copy
                        // the bitmap and the bitmak layers of the cursor into the bitmap.

                        resultBitmap = new Bitmap(bitmapMask.Width, bitmapMask.Width);
                        PInvoke.SelectObject(mask.Handle, iconInfo.MaskBitmap.Handle);

                        using(Graphics resultGraphics = Graphics.FromImage(resultBitmap))
                        {
                            IntPtr resultHandle = resultGraphics.GetHdc();

                            // These two operation will result in a black cursor over a white background. Later
                            //   in the code, a call to MakeTransparent() will get rid of the white background.
                            PInvoke.BitBlt(resultHandle, 0, 0, 32, 32, mask.Handle, 0, 32, CopyPixelOperation.SourceCopy);
                            PInvoke.BitBlt(resultHandle, 0, 0, 32, 32, mask.Handle, 0, 0, CopyPixelOperation.SourceInvert);

                            resultGraphics.ReleaseHdc(resultHandle);
                        }

                        // Remove the white background from the BitBlt calls,
                        // resulting in a black cursor over a transparent background.
                        resultBitmap.MakeTransparent(Color.White);
                    } else
                    {
                        // This isn't a monochrome cursor.
                        using(Icon icon = Icon.FromHandle(hicon.Handle))
                            resultBitmap = icon.ToBitmap();
                    }
                }
                _Cursor = resultBitmap;
                if(MouseImageChangedEvent != null)
                {
                    using(var ms = new MemoryStream())
                    {
                        _Cursor.Save(ms, ImageFormat.Png);//png to preserve the transparency
                        MouseImageChangedEvent(_Position, ms.ToArray());
                    }
                }
            }
        }

        private Bitmap _Cursor = null;
        private Point _Position = new Point();

        public void Draw(Graphics graphics)
        {
            if(graphics == null)
                throw new ArgumentNullException("graphics");

            Bitmap cursor = _Cursor;

            if(cursor != null)
                graphics.DrawImage(cursor, _Position);
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
                mask.Dispose();
                mask = null;
            }
        }
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach(ImageCodecInfo codec in codecs)
            {
                if(codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

    }
}

