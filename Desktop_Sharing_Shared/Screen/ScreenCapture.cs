using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        public Raw_Image GetScreen(Size sz)
        {
            IntPtr hOldBmp = IntPtr.Zero;
            Raw_Image raw = new Raw_Image();
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
                    nBmp = PInvoke.CreateCompatibleBitmap(nSrce, sz.Width, sz.Height);
                }
                PreviousSize = sz;
                hOldBmp = PInvoke.SelectObject(nDest, nBmp);

                bool b = PInvoke.BitBlt(nDest, 0, 0, sz.Width, sz.Height, nSrce, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

                Desktop_Sharing_Shared.Screen.PInvoke.BITMAPINFO bi = new PInvoke.BITMAPINFO();

                bi.bmiHeader.biSize = 40;
                bi.bmiHeader.biWidth = sz.Width;
                bi.bmiHeader.biHeight = sz.Height;
                bi.bmiHeader.biPlanes = 1;
                bi.bmiHeader.biBitCount = 32;
                bi.bmiHeader.biCompression = Desktop_Sharing_Shared.Screen.PInvoke.BitmapCompressionMode.BI_RGB;
                bi.bmiHeader.biSizeImage = 0;
                bi.bmiHeader.biXPelsPerMeter = 0;
                bi.bmiHeader.biYPelsPerMeter = 0;
                bi.bmiHeader.biClrUsed = 0;
                bi.bmiHeader.biClrImportant = 0;
                var dwBmpSize = ((sz.Width * bi.bmiHeader.biBitCount + 31) / 32) * 4 * sz.Height;
                raw.Data = new byte[dwBmpSize];
                raw.Dimensions = new Rectangle(0, 0, sz.Width, sz.Height);

                Desktop_Sharing_Shared.Screen.PInvoke.GetDIBits(nSrce, nBmp, 0, Convert.ToUInt32(sz.Height), raw.Data, ref bi, PInvoke.DIB_Color_Mode.DIB_RGB_COLORS);

                //Bitmap bmp = Bitmap.FromHbitmap(nBmp);
                PInvoke.SelectObject(nDest, hOldBmp);

               // return bmp;
            } catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
           // return new Bitmap(sz.Width, sz.Height);
            return raw;
        }
        //Raw_Image WriteBitmapFile(string filename, int width, int height, byte[] imageData)
        //{
        //    var stride = width * 4;
        //    unsafe
        //    {
        //        fixed(byte* ptr = imageData)
        //        {
        //            var p = ptr + stride * (height - 1);
        //            using(Bitmap image = new Bitmap(width, height, -stride, PixelFormat.Format32bppRgb, new IntPtr(p)))
        //            {
        //                image.Save(filename);
        //            }
        //        }
        //    }

        //    return 1;
        //}

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
