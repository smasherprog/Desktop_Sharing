using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Screen
{
    public class ScreenCapture : IDisposable
    {
        public ImageCodecInfo _jgpEncoder;
        public EncoderParameters _myEncoderParameters;
        IntPtr nDesk = IntPtr.Zero;
        IntPtr nSrce = IntPtr.Zero;
        IntPtr nDest = IntPtr.Zero;
        IntPtr nBmp = IntPtr.Zero;
        private Size PreviousSize = new Size(0, 0);

        public ScreenCapture(long jpgquality = 60L)
        {
            _jgpEncoder = GetEncoder(ImageFormat.Jpeg);
            var myEncoder = System.Drawing.Imaging.Encoder.Quality;
            var myEncoderParameter = new EncoderParameter(myEncoder, jpgquality);
            _myEncoderParameters = new EncoderParameters(1);
            _myEncoderParameters.Param[0] = myEncoderParameter;

        }
        public void Dispose()
        {
            if(nBmp != IntPtr.Zero)
                PInvoke.DeleteObject(nBmp);
            if(nDest != IntPtr.Zero)
                PInvoke.DeleteDC(nDest);
            PInvoke.ReleaseDC(nDesk, nSrce);
            nDesk = IntPtr.Zero;
            nSrce = IntPtr.Zero;
            nDest = IntPtr.Zero;
            nBmp = IntPtr.Zero;
        }
        public void ReleaseHandles()
        {
            Dispose();
        }
        public Bitmap GetScreen(Size sz)
        {
            IntPtr hOldBmp = IntPtr.Zero;

            try
            {
                var dt = DateTime.Now;

                if(nDesk == IntPtr.Zero)
                    nDesk = PInvoke.GetDesktopWindow();
                if(nSrce == IntPtr.Zero)
                    nSrce = PInvoke.GetWindowDC(nDesk);
                if(nDest == IntPtr.Zero)
                    nDest = PInvoke.CreateCompatibleDC(nSrce);
                if(nBmp == IntPtr.Zero)
                    nBmp = PInvoke.CreateCompatibleBitmap(nSrce, sz.Width, sz.Height);
                else if(sz.Height != PreviousSize.Height || sz.Width != PreviousSize.Width)
                {// user changed resolution.. get new bitmap
                    PInvoke.DeleteObject(nBmp);
                    nBmp  = PInvoke.CreateCompatibleBitmap(nSrce, sz.Width, sz.Height);
                }
                PreviousSize = sz;
                hOldBmp = PInvoke.SelectObject(nDest, nBmp);

                bool b = PInvoke.BitBlt(nDest, 0, 0, sz.Width, sz.Height, nSrce, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
                Bitmap bmp = Bitmap.FromHbitmap(nBmp);
                PInvoke.SelectObject(nDest, hOldBmp);

                return bmp;
            } catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            return new Bitmap(sz.Height, sz.Width);
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
