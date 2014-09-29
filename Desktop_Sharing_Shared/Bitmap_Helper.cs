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

            if(first.Width != second.Width || first.Height != second.Height || first.PixelFormat != second.PixelFormat)
                throw new ArgumentException("bitmaps must match format and sizes");
            var rect = new Rectangle(0, 0, first.Width, first.Height);
            var bmpDataA = first.LockBits(rect, ImageLockMode.ReadWrite, first.PixelFormat);
            var bmpDataB = second.LockBits(rect, ImageLockMode.ReadWrite, second.PixelFormat);
            int pxstride = GetStride(first.PixelFormat);

            unsafe
            {
                byte* pPixelsA = (byte*)bmpDataA.Scan0.ToPointer();
                byte* pPixelsB = (byte*)bmpDataB.Scan0.ToPointer();
                for(int y = 0; y < first.Height; y += 1)
                {
                    int* linea = (int*)(pPixelsA + (bmpDataA.Stride * y));
                    int* lineb = (int*)(pPixelsB + (bmpDataB.Stride * y));
                    for(int x = 0; x < bmpDataA.Width; x += pxstride)
                    {
                        for(var i = 0; i < pxstride; i++)
                        {
                            if(linea[x + i] != lineb[x + i])
                            {
                                if(top == -1)
                                {
                                    top = y;
                                    if(bottom == -1)
                                        bottom = y + 1;
                                } else
                                    bottom = y + 1;
                                if(left == -1)
                                {
                                    left = x;
                                    if(right == -1)
                                        right = x + 1;
                                } else if(x < left)
                                {
                                    left = x;
                                } else if(right < x)
                                {
                                    right = x + 1;
                                }

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
