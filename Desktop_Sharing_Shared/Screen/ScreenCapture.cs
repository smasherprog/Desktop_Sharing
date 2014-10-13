using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desktop_Sharing_Shared.Screen
{
    public class ScreenCapture: IDisposable
    {

        private ImageCodecInfo _jgpEncoder;
        private EncoderParameters _myEncoderParameters;

        public ImageCodecInfo jgpEncoder { get { return _jgpEncoder; } }
        public EncoderParameters EncoderParameters { get { return _myEncoderParameters; } }

        Desktop_Sharing_Shared.Mouse.MouseCapture _MouseCapture;
 

        public ScreenCapture(long jpgquality=60L)
        {
            _jgpEncoder = GetEncoder(ImageFormat.Jpeg);
            var myEncoder = System.Drawing.Imaging.Encoder.Quality;
            var myEncoderParameter = new EncoderParameter(myEncoder, jpgquality);
            _myEncoderParameters = new EncoderParameters(1);
            _myEncoderParameters.Param[0] = myEncoderParameter;
            _MouseCapture = new Mouse.MouseCapture();
            
        }
        public Bitmap GetScreen(Size sz)
        {
            _MouseCapture.Update();
            IntPtr hDesk, hSrce, hDest, hBmp, hOldBmp;
            hDesk = hSrce = hDest = hBmp = hOldBmp = IntPtr.Zero;
  
            try
            {
                hDesk = PInvoke.GetDesktopWindow();
                hSrce = PInvoke.GetWindowDC(hDesk);
                hDest = PInvoke.CreateCompatibleDC(hSrce);
                hBmp = PInvoke.CreateCompatibleBitmap(hSrce, sz.Width, sz.Height);
                hOldBmp = PInvoke.SelectObject(hDest, hBmp);
                bool b = PInvoke.BitBlt(hDest, 0, 0, sz.Width, sz.Height, hSrce, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
                Bitmap bmp = Bitmap.FromHbitmap(hBmp);
                PInvoke.SelectObject(hDest, hOldBmp);
                using(var g = Graphics.FromImage(bmp)) _MouseCapture.Draw(g);
                
                return bmp;
            } catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            } finally
            {
                if(hBmp != IntPtr.Zero)
                    PInvoke.DeleteObject(hBmp);
                if(hDest != IntPtr.Zero)
                    PInvoke.DeleteDC(hDest);
                PInvoke.ReleaseDC(hDesk, hSrce);
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
        public void Dispose()
        {
            _MouseCapture.Dispose();
        }
    }
}
