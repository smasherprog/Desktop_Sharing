using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace Desktop_Sharing_Shared
{
    public static class Bitmap_Helper
    {
        public static Rectangle Get_Diff(Bitmap first, Bitmap second)
        {
            int top = -1;
            int bottom = -1;
            int left = -1;
            int right = -1;

            if((first.Width != second.Width) || (first.Height != second.Height) || (first.PixelFormat != second.PixelFormat))
                throw new ArgumentException("bitmaps must match format and sizes");
            var rect = new Rectangle(0, 0, first.Width, first.Height);
            var bmpDataA = first.LockBits(rect, ImageLockMode.ReadOnly, first.PixelFormat);
            var bmpDataB = second.LockBits(rect, ImageLockMode.ReadOnly, second.PixelFormat);
            int pxstride = GetStride(first.PixelFormat);
            if(pxstride != 4)
                throw new ArgumentException("bitmaps must be of a 4 byte stride, i.e. 32 bits!");
            unsafe
            {
                byte* pPixelsA = (byte*)bmpDataA.Scan0.ToPointer();
                byte* pPixelsB = (byte*)bmpDataB.Scan0.ToPointer();
                var even = (bmpDataA.Width % 4);// 16 bytes per operation
                var totalwidth = bmpDataA.Width - even; //subtract any extra bits to ensure I dont go over the array bounds

                for(int y = 0; y < first.Height; y += 4)//go 4 rows at a time, no need to check every pixel
                {
                    var linea = (long*)(pPixelsA + (bmpDataA.Stride * y));
                    var lineb = (long*)(pPixelsB + (bmpDataB.Stride * y));
                    var multiplier = 2;
                    for(int x = 0; x < totalwidth/2; x+=2)//divide by 2 because I am reading logn, which is 8 bytes of data at a time. The x+=2 is because I am reading the next array
                    {

                        var la = linea[x] + linea[x + 1];
                        var lb = lineb[x] + lineb[x + 1];
                        if(la != lb)
                        {
                            var tmpx = x * multiplier;
                            if(top == -1)
                            {
                                top = y;
                                if(bottom == -1)
                                    bottom = y + 1;
                            } else
                                bottom = y + 1;
                            if(left == -1)
                            {
                                left = tmpx;
                                if(right == -1)
                                    right = tmpx + 1;
                            } else if(tmpx < left)
                            {
                                left = tmpx;
                            } else if(right < tmpx)
                            {
                                right = tmpx + 1;
                            }

                        }

                    }
                }

            }
            first.UnlockBits(bmpDataA);
            second.UnlockBits(bmpDataB);
            if((right - left <= 0) || (bottom - top <= 0))
                return new Rectangle(0, 0, 0, 0);
            if(left > 0)
                left -= 1;
            if(top > 0)
                top -= 1;
            if(top > 0)
                top -= 1;
            if(top > 0)
                top -= 1;
            if(right < second.Width - 1)
                right += 1;
            if(right < second.Width - 1)
                right += 1;
            if(right < second.Width - 1)
                right += 1;
            if(bottom < second.Height - 1)
                bottom += 1;
            return new Rectangle(left, top, right - left, bottom - top);
        }
        private static int GetStride(PixelFormat p)
        {
            if(p == PixelFormat.Format24bppRgb)
                return 3;
            else if(p == PixelFormat.Format32bppArgb ||
                p == PixelFormat.Format32bppPArgb ||
                p == PixelFormat.Format32bppRgb)
                return 4;
            throw new ArgumentException("Pixel Format Not Correct!");
        }

    }

}
