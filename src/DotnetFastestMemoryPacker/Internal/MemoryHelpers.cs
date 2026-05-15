using PatcherReference;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public unsafe static partial class MemoryHelpers
{
    [Inline]
    public static void Copy(void* destination, void* source, uint length)
    {
        if (length < 0x78)
        {
            if ((length & 0x40) != 0)
            {
                var (xmm0, xmm1) = (*(Vector256<long>*)source, *((Vector256<long>*)source + 1));
                *(Vector256<long>*)destination = xmm0;
                *((Vector256<long>*)destination + 1) = xmm1;
            }

            if ((length & 0x20) != 0)
            {
                *(Vector256<long>*)destination = *(Vector256<long>*)source;
            }

            if ((length & 0x10) != 0)
            {
                *(Vector128<long>*)destination = *(Vector128<long>*)source;
            }

            if ((length & 0x8) != 0)
            {
                *(long*)destination = *(long*)source;
            }
        }
        else
        {
            Unsafe.CopyBlock(source, destination, length);
        }
    }
}