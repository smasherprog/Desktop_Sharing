using Desktop_Sharing_Shared.Screen;
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
        public static Rectangle Get_Diff(Bitmap first, Bitmap second, int horizontal_pixel_jump = 4)
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
                var even = (bmpDataA.Width % horizontal_pixel_jump);
                var totalwidth = bmpDataA.Width - even; //subtract any extra bits to ensure I dont go over the array bounds

                for(int y = 0; y < first.Height; y++)
                {
                    var linea = (int*)(pPixelsA + (bmpDataA.Stride * y));
                    var lineb = (int*)(pPixelsB + (bmpDataB.Stride * y));
                    var multiplier = 2;
                    for(int x = 0; x < totalwidth; x += horizontal_pixel_jump)
                    {

                        var la = linea[x] + linea[x + 1];
                        var lb = lineb[x] + lineb[x + 1];
                        if(la != lb)
                        {
                            var tmpx = x;
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

        public static Rectangle Get_Diff(Raw_Image first, Raw_Image second, int horizontal_pixel_jump = 4)
        {
            int top = -1;
            int bottom = -1;
            int left = -1;
            int right = -1;

            unsafe
            {
                fixed(byte* pPixelsA = first.Data, pPixelsB = second.Data)
                {
                    var even = (first.Dimensions.Width % horizontal_pixel_jump);// 16 bytes per operation
                    var totalwidth = first.Dimensions.Width - even; //subtract any extra bits to ensure I dont go over the array bounds
                    var rowstride = first.Dimensions.Width * 4;
                    for(int y = 0; y < first.Dimensions.Height; y++)
                    {
                        var linea = (long*)(pPixelsA + (rowstride * y));
                        var lineb = (long*)(pPixelsB + (rowstride * y));
                        var multiplier = 2;
                        for(int x = 0; x < totalwidth / 2; x += horizontal_pixel_jump)
                        {

                            var la = linea[x];
                            var lb = lineb[x];
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
            }
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
            if(right < first.Dimensions.Width - 1)
                right += 1;
            if(right < first.Dimensions.Width - 1)
                right += 1;
            if(right < first.Dimensions.Width - 1)
                right += 1;
            if(bottom < first.Dimensions.Height - 1)
                bottom += 1;
            return new Rectangle(left, top, right - left, bottom - top);
        }
        public static void Copy(Bitmap src, Bitmap dst, Point tl)
        {
            var srcrect = new Rectangle(0, 0, src.Width, src.Height);
            var dstrect = new Rectangle(0, 0, dst.Width, dst.Height);

            var srcDataA = src.LockBits(srcrect, ImageLockMode.ReadOnly, src.PixelFormat);
            var dstDataB = dst.LockBits(dstrect, ImageLockMode.WriteOnly, dst.PixelFormat);

            unsafe
            {
                byte* pPixelsA = (byte*)srcDataA.Scan0.ToPointer();
                byte* pPixelsB = (byte*)dstDataB.Scan0.ToPointer();

                for(int y = 0; y < srcrect.Height; y++)
                {
                    var srca = (int*)(pPixelsA + (srcDataA.Stride * y));
                    var dstb = (int*)(pPixelsB + (dstDataB.Stride * (y + tl.Y)));

                    for(int x = 0; x < srcrect.Width; x++)
                    {
                        dstb[x + tl.X] = srca[x];
                    }
                }
            }
            src.UnlockBits(srcDataA);
            dst.UnlockBits(dstDataB);
        }
        public static void Copy(List<Raw_Image> src_imgs, Bitmap dst)
        {
            if(!src_imgs.Any())
                return;
            try
            {
          
                var dstrect = new Rectangle(0, 0, dst.Width, dst.Height);
                var dstDataB = dst.LockBits(dstrect, ImageLockMode.WriteOnly, dst.PixelFormat);
          
                unsafe
                {
                    foreach(var item in src_imgs)
                    {

                        fixed(byte* src = item.Data)
                        {
                            var srcrowstride = Convert.ToUInt64(item.Dimensions.Width * 4);

                            byte* pPixelsB = (byte*)dstDataB.Scan0.ToPointer();
                            for(int y = 0; y < item.Dimensions.Height; y++)
                            {
                                var srcrow = src + (item.Dimensions.Width * 4 * y);

                                var dstrow = (pPixelsB + (dstDataB.Stride * (y + item.Dimensions.Top))) + (item.Dimensions.Left * 4);

                                Desktop_Sharing_Shared.Utilities.PInvoke.CopyMemory(dstrow, srcrow, srcrowstride);
                            }
                        }
                    }
                }
              
                dst.UnlockBits(dstDataB);
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        public static byte[] Copy(Raw_Image src_img, Rectangle r)
        {
            var arr = new byte[r.Width * r.Height * 4];
            unsafe
            {
                fixed(byte* src = src_img.Data, dst = arr)
                {
                    var srcrowstride = src_img.Dimensions.Width * 4;
                    var dstrowstride = Convert.ToUInt64(r.Width * 4);
                    for(int y = 0; y < r.Height; y++)
                    {
                        var srcrow = (int*)(src + (srcrowstride * (y + r.Top)));
                        srcrow += r.Left;
                        var dstrow = dst + (r.Width * 4 * y);
                        Desktop_Sharing_Shared.Utilities.PInvoke.CopyMemory(dstrow, srcrow, dstrowstride);
                    }
                }
            }
            return arr;
        }
        public static void Fill(Bitmap dst, Point sz, byte[] m)
        {
            try
            {
                var dstrect = new Rectangle(0, 0, dst.Width, dst.Height);
                var dstDataB = dst.LockBits(dstrect, ImageLockMode.WriteOnly, dst.PixelFormat);
    
                unsafe
                {
                    fixed(byte* src = m)
                    {
                        var srcrowstride = sz.X * 4;
                        byte* pPixelsB = (byte*)dstDataB.Scan0.ToPointer();
                        for(int y = 0; y < sz.Y; y++)
                        {
                            var srcrow = src + (srcrowstride * y);
                            var dstrow = pPixelsB +( dstDataB.Stride *y);
                            Desktop_Sharing_Shared.Utilities.PInvoke.CustomCopy(dstrow, srcrow, srcrowstride);
                        }

                    }
                }

                dst.UnlockBits(dstDataB);
       
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }

}
