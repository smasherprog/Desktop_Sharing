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
        static unsafe void CustomCopy(void* dest, void* src, int count)
        {
            int block;

            block = count >> 3;

            long* pDest = (long*)dest;
            long* pSrc = (long*)src;

            for(int i = 0; i < block; i++)
            {
                *pDest = *pSrc;
                pDest++;
                pSrc++;
            }
            dest = pDest;
            src = pSrc;
            count = count - (block << 3);

            if(count > 0)
            {
                byte* pDestB = (byte*)dest;
                byte* pSrcB = (byte*)src;
                for(int i = 0; i < count; i++)
                {
                    *pDestB = *pSrcB;
                    pDestB++;
                    pSrcB++;
                }
            }
        }
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
                for(int y = 0; y < first.Height; y += 1)
                {
                    var linea = (int*)(pPixelsA + (bmpDataA.Stride * y));
                    var lineb = (int*)(pPixelsB + (bmpDataB.Stride * y));
                    for(int x = 0; x < bmpDataA.Width; x += pxstride)
                    {
                        for(var i = 0; i < pxstride; i++)
                        {
                            var la = linea[x + i];
                            var lb = lineb[x + i];
                            if(la != lb)
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
        public static void FastCopy(Bitmap dst, Point dst_top_left, Bitmap src)
        {
            if((src.Width > dst.Width) || (src.Height > dst.Height) || (dst.PixelFormat != src.PixelFormat))
                throw new ArgumentException("bitmaps must match format and dst cannot be smaller than the src");
            var rect = new Rectangle(dst_top_left.X, dst_top_left.Y, src.Width, src.Height);
            var bmpDatadst = dst.LockBits(rect, ImageLockMode.WriteOnly, dst.PixelFormat);
            var bmpDatasrc = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, src.PixelFormat);
            int pxstride = GetStride(dst.PixelFormat);
            unsafe
            {
                byte* pPixelsA = (byte*)bmpDatadst.Scan0.ToPointer();
                byte* pPixelsB = (byte*)bmpDatasrc.Scan0.ToPointer();

                for(int y = dst_top_left.Y, srcy = 0; y < dst_top_left.Y + src.Height; y += 1, srcy += 1)
                {
                    int* linea = (int*)(pPixelsA + (bmpDatadst.Stride * y));
                    int* lineb = (int*)(pPixelsB + (bmpDatasrc.Stride * srcy));
                    for(int x = dst_top_left.X; x < src.Width + dst_top_left.X; x++)
                    {

                    }

                        CustomCopy(linea, lineb, (src.Width - dst_top_left.X) * pxstride);
                }
            }
            dst.UnlockBits(bmpDatadst);
            src.UnlockBits(bmpDatasrc);
        }
    }

}
