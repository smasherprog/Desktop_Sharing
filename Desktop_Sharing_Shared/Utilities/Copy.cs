using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Desktop_Sharing_Shared.Utilities
{
    public static class PInvoke
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false), SuppressUnmanagedCodeSecurity]
        public static unsafe extern void* CopyMemory(void* dest, void* src, ulong byte_count);
        public static unsafe void CustomCopy(void* dest, void* src, int count)
        {
            int block;

            block = count >> 3;

            long* pDest = (long*)dest;
            long* pSrc = (long*)src;

            for(int i = 0; i < block; i++)
            {
                var s = *pSrc;

                *pDest = s;
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
    }
}
