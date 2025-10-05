using PatcherReference;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
using u1 = byte;
using u2 = ushort;
using u4 = uint;
using u8 = ulong;

namespace DotnetFastestMemoryPacker.Internal;
unsafe static class MemoryHelpers
{
    public static void CopyA(nuint destination, nuint source, ulong length)
    {
        Uninitialized(out Vector128<byte> xmm0);
        Uninitialized(out Vector256<byte> ymm0);
        Uninitialized(out Vector256<byte> ymm1);
        Uninitialized(out Vector256<byte> ymm2);
        Uninitialized(out Vector256<byte> ymm3);

        Copy(xmm0, ymm0, ymm1, ymm2, ymm3, As<ulong>(destination), As<ulong>(source), length);
    }

    [Inline]
    public static void Copy(
        Vector128<byte> xmm0,
        Vector256<byte> ymm0,
        Vector256<byte> ymm1,
        Vector256<byte> ymm2,
        Vector256<byte> ymm3,
        ulong destination,
        ulong source,
        ulong length,
        int destinationAlignment = 1,
        int sourceAlignment = 1,
        int lengthAlignment = 1,
        bool careAboutDestinationBounce = true,
        bool careAboutSourceBounce = true,
        bool preferAvoidanceDestinationCaching = false,
        bool preferAvoidanceSourceCaching = false)
    {
        /*
        switch (length)
        {
            case 0:
                {
                    Console.WriteLine(0);
                    return;
                }
            case 1:
                {
                    Console.WriteLine(1);
                    return;
                }
            case 2:
                {
                    Console.WriteLine(2);
                    return;
                }
            case 3:
                {
                    Console.WriteLine(3);
                    return;
                }
            case 4:
                {
                    Console.WriteLine(4);
                    return;
                }
            case 5:
                {
                    Console.WriteLine(5);
                    return;
                }
            case 6:
                {
                    Console.WriteLine(6);
                    return;
                }
            case 7:
                {
                    Console.WriteLine(7);
                    return;
                }
            default:
                {
                    Console.WriteLine(7);
                    return;
                }
        }
        */

        
        switch (length)
        {
            case 0UL:
                {
                    return;
                }
            case 1UL:
                {
                    u1 u1 = *(u1*)source;

                    *(u1*)destination = u1;
                    return;
                }
            case 2UL:
                {
                    u2 u2 = *(u2*)source;

                    *(u2*)destination = u2;
                    return;
                }
            case 3UL:
                {
                    u2 u2 = *(u2*)source;
                    u1 u1 = *(u1*)(source + 2);

                    *(u2*)destination = u2;
                    *(u1*)(destination + 2) = u1;
                    return;
                }
            case 4UL:
                {
                    u4 u4 = *(u4*)source;

                    *(u4*)destination = u4;
                    return;
                }
            case 5UL:
                {
                    u4 u4 = *(u4*)source;
                    u1 u1 = *(u1*)(source + 4);

                    *(u4*)destination = u4;
                    *(u1*)(destination + 4) = u1;
                    return;
                }
            case 6UL:
                {
                    u4 u4 = *(u4*)source;
                    u2 u2 = *(u2*)(source + 4);

                    *(u4*)destination = u4;
                    *(u2*)(destination + 4) = u2;
                    return;
                }
            case 7UL:
                {
                    u4 u4 = *(u4*)source;
                    u2 u2 = *(u2*)(source + 4);
                    u1 u1 = *(u1*)(source + 4 + 2);

                    *(u4*)destination = u4;
                    *(u2*)(destination + 4) = u2;
                    *(u1*)(destination + 4 + 2) = u1;
                    return;
                }
            case 8UL:
                {
                    u8 u8 = *(u8*)source;

                    *(u8*)destination = u8;
                    return;
                }
            case 9UL:
                {
                    u8 u8 = *(u8*)source;
                    u1 u1 = *(u1*)(source + 8);

                    *(u8*)destination = u8;
                    *(u1*)(destination + 8) = u1;
                    return;
                }
            case 10UL:
                {
                    u8 u8 = *(u8*)source;
                    u2 u2 = *(u2*)(source + 8);

                    *(u8*)destination = u8;
                    *(u2*)(destination + 8) = u2;
                    return;
                }
            case 11UL:
                {
                    u8 u8 = *(u8*)source;
                    u2 u2 = *(u2*)(source + 8);
                    u1 u1 = *(u1*)(source + 8 + 2);

                    *(u8*)destination = u8;
                    *(u2*)(destination + 8) = u2;
                    *(u1*)(destination + 8 + 2) = u1;
                    return;
                }
            case 12UL:
                {
                    u8 u8 = *(u8*)source;
                    u4 u4 = *(u4*)(source + 8);

                    *(u8*)destination = u8;
                    *(u4*)(destination + 8) = u4;
                    return;
                }
            case 13UL:
                {
                    u8 u8 = *(u8*)source;
                    u4 u4 = *(u4*)(source + 8);
                    u1 u1 = *(u1*)(source + 8 + 4);

                    *(u8*)destination = u8;
                    *(u4*)(destination + 8) = u4;
                    *(u1*)(destination + 8 + 4) = u1;
                    return;
                }
            case 14UL:
                {
                    u8 u8 = *(u8*)source;
                    u4 u4 = *(u4*)(source + 8);
                    u2 u2 = *(u2*)(source + 8 + 4);

                    *(u8*)destination = u8;
                    *(u4*)(destination + 8) = u4;
                    *(u2*)(destination + 8 + 4) = u2;
                    return;
                }
            case 15UL:
                {
                    u8 u8 = *(u8*)source;
                    u4 u4 = *(u4*)(source + 8);
                    u2 u2 = *(u2*)(source + 8 + 4);
                    u1 u1 = *(u1*)(source + 8 + 4 + 2);

                    *(u8*)destination = u8;
                    *(u4*)(destination + 8) = u4;
                    *(u2*)(destination + 8 + 4) = u2;
                    *(u1*)(destination + 8 + 4 + 2) = u1;
                    return;
                }
            case 16UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);

                    Sse2.Store((u1*)destination, xmm0);
                    return;
                }
            case 17UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u1 u1 = *(u1*)(source + 16);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u1*)(destination + 16) = u1;
                    return;
                }
            case 18UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u2 u2 = *(u2*)(source + 16);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u2*)(destination + 16) = u2;
                    return;
                }
            case 19UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u2 u2 = *(u2*)(source + 16);
                    u1 u1 = *(u1*)(source + 16 + 2);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u2*)(destination + 16) = u2;
                    *(u1*)(destination + 16 + 2) = u1;
                    return;
                }
            case 20UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u4 u4 = *(u4*)(source + 16);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u4*)(destination + 16) = u4;
                    return;
                }
            case 21UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u4 u4 = *(u4*)(source + 16);
                    u1 u1 = *(u1*)(source + 16 + 4);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u4*)(destination + 16) = u4;
                    *(u1*)(destination + 16 + 4) = u1;
                    return;
                }
            case 22UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u4 u4 = *(u4*)(source + 16);
                    u2 u2 = *(u2*)(source + 16 + 4);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u4*)(destination + 16) = u4;
                    *(u2*)(destination + 16 + 4) = u2;
                    return;
                }
            case 23UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u4 u4 = *(u4*)(source + 16);
                    u2 u2 = *(u2*)(source + 16 + 4);
                    u1 u1 = *(u1*)(source + 16 + 4 + 2);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u4*)(destination + 16) = u4;
                    *(u2*)(destination + 16 + 4) = u2;
                    *(u1*)(destination + 16 + 4 + 2) = u1;
                    return;
                }
            case 24UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u8 u8 = *(u8*)(source + 16);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u8*)(destination + 16) = u8;
                    return;
                }
            case 25UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u8 u8 = *(u8*)(source + 16);
                    u1 u1 = *(u1*)(source + 16 + 8);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u8*)(destination + 16) = u8;
                    *(u1*)(destination + 16 + 8) = u1;
                    return;
                }
            case 26UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u8 u8 = *(u8*)(source + 16);
                    u2 u2 = *(u2*)(source + 16 + 8);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u8*)(destination + 16) = u8;
                    *(u2*)(destination + 16 + 8) = u2;
                    return;
                }
            case 27UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u8 u8 = *(u8*)(source + 16);
                    u2 u2 = *(u2*)(source + 16 + 8);
                    u1 u1 = *(u1*)(source + 16 + 8 + 2);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u8*)(destination + 16) = u8;
                    *(u2*)(destination + 16 + 8) = u2;
                    *(u1*)(destination + 16 + 8 + 2) = u1;
                    return;
                }
            case 28UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u8 u8 = *(u8*)(source + 16);
                    u4 u4 = *(u4*)(source + 16 + 8);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u8*)(destination + 16) = u8;
                    *(u4*)(destination + 16 + 8) = u4;
                    return;
                }
            case 29UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u8 u8 = *(u8*)(source + 16);
                    u4 u4 = *(u4*)(source + 16 + 8);
                    u1 u1 = *(u1*)(source + 16 + 8 + 4);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u8*)(destination + 16) = u8;
                    *(u4*)(destination + 16 + 8) = u4;
                    *(u1*)(destination + 16 + 8 + 4) = u1;
                    return;
                }
            case 30UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u8 u8 = *(u8*)(source + 16);
                    u4 u4 = *(u4*)(source + 16 + 8);
                    u2 u2 = *(u2*)(source + 16 + 8 + 4);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u8*)(destination + 16) = u8;
                    *(u4*)(destination + 16 + 8) = u4;
                    *(u2*)(destination + 16 + 8 + 4) = u2;
                    return;
                }
            case 31UL:
                {
                    xmm0 = Sse2.LoadVector128((u1*)source);
                    u8 u8 = *(u8*)(source + 16);
                    u4 u4 = *(u4*)(source + 16 + 8);
                    u2 u2 = *(u2*)(source + 16 + 8 + 4);
                    u1 u1 = *(u1*)(source + 16 + 8 + 4 + 2);

                    Sse2.Store((u1*)destination, xmm0);
                    *(u8*)(destination + 16) = u8;
                    *(u4*)(destination + 16 + 8) = u4;
                    *(u2*)(destination + 16 + 8 + 4) = u2;
                    *(u1*)(destination + 16 + 8 + 4 + 2) = u1;
                    return;
                }
            case 32UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);

                    Avx.Store((u1*)destination, ymm0);
                    return;
                }
            case 33UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u1 u1 = *(u1*)(source + 32);

                    Avx.Store((u1*)destination, ymm0);
                    *(u1*)(destination + 32) = u1;
                    return;
                }
            case 34UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u2 u2 = *(u2*)(source + 32);

                    Avx.Store((u1*)destination, ymm0);
                    *(u2*)(destination + 32) = u2;
                    return;
                }
            case 35UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u2 u2 = *(u2*)(source + 32);
                    u1 u1 = *(u1*)(source + 32 + 2);

                    Avx.Store((u1*)destination, ymm0);
                    *(u2*)(destination + 32) = u2;
                    *(u1*)(destination + 32 + 2) = u1;
                    return;
                }
            case 36UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u4 u4 = *(u4*)(source + 32);

                    Avx.Store((u1*)destination, ymm0);
                    *(u4*)(destination + 32) = u4;
                    return;
                }
            case 37UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u4 u4 = *(u4*)(source + 32);
                    u1 u1 = *(u1*)(source + 32 + 4);

                    Avx.Store((u1*)destination, ymm0);
                    *(u4*)(destination + 32) = u4;
                    *(u1*)(destination + 32 + 4) = u1;
                    return;
                }
            case 38UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u4 u4 = *(u4*)(source + 32);
                    u2 u2 = *(u2*)(source + 32 + 4);

                    Avx.Store((u1*)destination, ymm0);
                    *(u4*)(destination + 32) = u4;
                    *(u2*)(destination + 32 + 4) = u2;
                    return;
                }
            case 39UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u4 u4 = *(u4*)(source + 32);
                    u2 u2 = *(u2*)(source + 32 + 4);
                    u1 u1 = *(u1*)(source + 32 + 4 + 2);

                    Avx.Store((u1*)destination, ymm0);
                    *(u4*)(destination + 32) = u4;
                    *(u2*)(destination + 32 + 4) = u2;
                    *(u1*)(destination + 32 + 4 + 2) = u1;
                    return;
                }
            case 40UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u8 u8 = *(u8*)(source + 32);

                    Avx.Store((u1*)destination, ymm0);
                    *(u8*)(destination + 32) = u8;
                    return;
                }
            case 41UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u8 u8 = *(u8*)(source + 32);
                    u1 u1 = *(u1*)(source + 32 + 8);

                    Avx.Store((u1*)destination, ymm0);
                    *(u8*)(destination + 32) = u8;
                    *(u1*)(destination + 32 + 8) = u1;
                    return;
                }
            case 42UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u8 u8 = *(u8*)(source + 32);
                    u2 u2 = *(u2*)(source + 32 + 8);

                    Avx.Store((u1*)destination, ymm0);
                    *(u8*)(destination + 32) = u8;
                    *(u2*)(destination + 32 + 8) = u2;
                    return;
                }
            case 43UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u8 u8 = *(u8*)(source + 32);
                    u2 u2 = *(u2*)(source + 32 + 8);
                    u1 u1 = *(u1*)(source + 32 + 8 + 2);

                    Avx.Store((u1*)destination, ymm0);
                    *(u8*)(destination + 32) = u8;
                    *(u2*)(destination + 32 + 8) = u2;
                    *(u1*)(destination + 32 + 8 + 2) = u1;
                    return;
                }
            case 44UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u8 u8 = *(u8*)(source + 32);
                    u4 u4 = *(u4*)(source + 32 + 8);

                    Avx.Store((u1*)destination, ymm0);
                    *(u8*)(destination + 32) = u8;
                    *(u4*)(destination + 32 + 8) = u4;
                    return;
                }
            case 45UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u8 u8 = *(u8*)(source + 32);
                    u4 u4 = *(u4*)(source + 32 + 8);
                    u1 u1 = *(u1*)(source + 32 + 8 + 4);

                    Avx.Store((u1*)destination, ymm0);
                    *(u8*)(destination + 32) = u8;
                    *(u4*)(destination + 32 + 8) = u4;
                    *(u1*)(destination + 32 + 8 + 4) = u1;
                    return;
                }
            case 46UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u8 u8 = *(u8*)(source + 32);
                    u4 u4 = *(u4*)(source + 32 + 8);
                    u2 u2 = *(u2*)(source + 32 + 8 + 4);

                    Avx.Store((u1*)destination, ymm0);
                    *(u8*)(destination + 32) = u8;
                    *(u4*)(destination + 32 + 8) = u4;
                    *(u2*)(destination + 32 + 8 + 4) = u2;
                    return;
                }
            case 47UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    u8 u8 = *(u8*)(source + 32);
                    u4 u4 = *(u4*)(source + 32 + 8);
                    u2 u2 = *(u2*)(source + 32 + 8 + 4);
                    u1 u1 = *(u1*)(source + 32 + 8 + 4 + 2);

                    Avx.Store((u1*)destination, ymm0);
                    *(u8*)(destination + 32) = u8;
                    *(u4*)(destination + 32 + 8) = u4;
                    *(u2*)(destination + 32 + 8 + 4) = u2;
                    *(u1*)(destination + 32 + 8 + 4 + 2) = u1;
                    return;
                }
            case 48UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    return;
                }
            case 49UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u1 u1 = *(u1*)(source + 32 + 16);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u1*)(destination + 32 + 16) = u1;
                    return;
                }
            case 50UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u2 u2 = *(u2*)(source + 32 + 16);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u2*)(destination + 32 + 16) = u2;
                    return;
                }
            case 51UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u2 u2 = *(u2*)(source + 32 + 16);
                    u1 u1 = *(u1*)(source + 32 + 16 + 2);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u2*)(destination + 32 + 16) = u2;
                    *(u1*)(destination + 32 + 16 + 2) = u1;
                    return;
                }
            case 52UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u4 u4 = *(u4*)(source + 32 + 16);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u4*)(destination + 32 + 16) = u4;
                    return;
                }
            case 53UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u4 u4 = *(u4*)(source + 32 + 16);
                    u1 u1 = *(u1*)(source + 32 + 16 + 4);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u4*)(destination + 32 + 16) = u4;
                    *(u1*)(destination + 32 + 16 + 4) = u1;
                    return;
                }
            case 54UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u4 u4 = *(u4*)(source + 32 + 16);
                    u2 u2 = *(u2*)(source + 32 + 16 + 4);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u4*)(destination + 32 + 16) = u4;
                    *(u2*)(destination + 32 + 16 + 4) = u2;
                    return;
                }
            case 55UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u4 u4 = *(u4*)(source + 32 + 16);
                    u2 u2 = *(u2*)(source + 32 + 16 + 4);
                    u1 u1 = *(u1*)(source + 32 + 16 + 4 + 2);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u4*)(destination + 32 + 16) = u4;
                    *(u2*)(destination + 32 + 16 + 4) = u2;
                    *(u1*)(destination + 32 + 16 + 4 + 2) = u1;
                    return;
                }
            case 56UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u8 u8 = *(u8*)(source + 32 + 16);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u8*)(destination + 32 + 16) = u8;
                    return;
                }
            case 57UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u8 u8 = *(u8*)(source + 32 + 16);
                    u1 u1 = *(u1*)(source + 32 + 16 + 8);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u8*)(destination + 32 + 16) = u8;
                    *(u1*)(destination + 32 + 16 + 8) = u1;
                    return;
                }
            case 58UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u8 u8 = *(u8*)(source + 32 + 16);
                    u2 u2 = *(u2*)(source + 32 + 16 + 8);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u8*)(destination + 32 + 16) = u8;
                    *(u2*)(destination + 32 + 16 + 8) = u2;
                    return;
                }
            case 59UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u8 u8 = *(u8*)(source + 32 + 16);
                    u2 u2 = *(u2*)(source + 32 + 16 + 8);
                    u1 u1 = *(u1*)(source + 32 + 16 + 8 + 2);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u8*)(destination + 32 + 16) = u8;
                    *(u2*)(destination + 32 + 16 + 8) = u2;
                    *(u1*)(destination + 32 + 16 + 8 + 2) = u1;
                    return;
                }
            case 60UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u8 u8 = *(u8*)(source + 32 + 16);
                    u4 u4 = *(u4*)(source + 32 + 16 + 8);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u8*)(destination + 32 + 16) = u8;
                    *(u4*)(destination + 32 + 16 + 8) = u4;
                    return;
                }
            case 61UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u8 u8 = *(u8*)(source + 32 + 16);
                    u4 u4 = *(u4*)(source + 32 + 16 + 8);
                    u1 u1 = *(u1*)(source + 32 + 16 + 8 + 4);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u8*)(destination + 32 + 16) = u8;
                    *(u4*)(destination + 32 + 16 + 8) = u4;
                    *(u1*)(destination + 32 + 16 + 8 + 4) = u1;
                    return;
                }
            case 62UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u8 u8 = *(u8*)(source + 32 + 16);
                    u4 u4 = *(u4*)(source + 32 + 16 + 8);
                    u2 u2 = *(u2*)(source + 32 + 16 + 8 + 4);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u8*)(destination + 32 + 16) = u8;
                    *(u4*)(destination + 32 + 16 + 8) = u4;
                    *(u2*)(destination + 32 + 16 + 8 + 4) = u2;
                    return;
                }
            case 63UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    xmm0 = Sse2.LoadVector128((u1*)source + 32);
                    u8 u8 = *(u8*)(source + 32 + 16);
                    u4 u4 = *(u4*)(source + 32 + 16 + 8);
                    u2 u2 = *(u2*)(source + 32 + 16 + 8 + 4);
                    u1 u1 = *(u1*)(source + 32 + 16 + 8 + 4 + 2);

                    Avx.Store((u1*)destination, ymm0);
                    Sse2.Store((u1*)destination + 32, xmm0);
                    *(u8*)(destination + 32 + 16) = u8;
                    *(u4*)(destination + 32 + 16 + 8) = u4;
                    *(u2*)(destination + 32 + 16 + 8 + 4) = u2;
                    *(u1*)(destination + 32 + 16 + 8 + 4 + 2) = u1;
                    return;
                }
            case 64UL:
                {
                    ymm0 = Avx.LoadVector256((u1*)source);
                    ymm1 = Avx.LoadVector256((u1*)source + 32);

                    Avx.Store((u1*)destination, ymm0);
                    Avx.Store((u1*)destination + 32, ymm1);
                    return;
                }
            default:
                {
                    var offset = source & 31UL;
                    switch (offset)
                    {
                        case 0:
                            {
                                goto DO_NOT_CALC_REMINDER;
                            }
                        case 1:
                            {
                                u1 u1 = *(u1*)source;
                                u2 u2 = *(u2*)(source + 1);
                                u4 u4 = *(u4*)(source + 1 + 2);
                                u8 u8 = *(u8*)(source + 1 + 2 + 4);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 1 + 2 + 4 + 8);

                                *(u1*)destination = u1;
                                *(u2*)(destination + 1) = u2;
                                *(u4*)(destination + 1 + 2) = u4;
                                *(u8*)(destination + 1 + 2 + 4) = u8;
                                Sse2.Store((u1*)destination + 1 + 2 + 4 + 8, xmm0);
                                offset = 31;
                                break;
                            }
                        case 2:
                            {
                                u2 u2 = *(u2*)source;
                                u4 u4 = *(u4*)(source + 2);
                                u8 u8 = *(u8*)(source + 2 + 4);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 2 + 4 + 8);

                                *(u2*)destination = u2;
                                *(u4*)(destination + 2) = u4;
                                *(u8*)(destination + 2 + 4) = u8;
                                Sse2.Store((u1*)destination + 2 + 4 + 8, xmm0);
                                offset = 30;
                                break;
                            }
                        case 3:
                            {
                                u1 u1 = *(u1*)source;
                                u4 u4 = *(u4*)(source + 1);
                                u8 u8 = *(u8*)(source + 1 + 4);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 1 + 4 + 8);

                                *(u1*)destination = u1;
                                *(u4*)(destination + 1) = u4;
                                *(u8*)(destination + 1 + 4) = u8;
                                Sse2.Store((u1*)destination + 1 + 4 + 8, xmm0);
                                offset = 29;
                                break;
                            }
                        case 4:
                            {
                                u4 u4 = *(u4*)source;
                                u8 u8 = *(u8*)(source + 4);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 4 + 8);

                                *(u4*)destination = u4;
                                *(u8*)(destination + 4) = u8;
                                Sse2.Store((u1*)destination + 4 + 8, xmm0);
                                offset = 28;
                                break;
                            }
                        case 5:
                            {
                                u1 u1 = *(u1*)source;
                                u2 u2 = *(u2*)(source + 1);
                                u8 u8 = *(u8*)(source + 1 + 2);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 1 + 2 + 8);

                                *(u1*)destination = u1;
                                *(u2*)(destination + 1) = u2;
                                *(u8*)(destination + 1 + 2) = u8;
                                Sse2.Store((u1*)destination + 1 + 2 + 8, xmm0);
                                offset = 27;
                                break;
                            }
                        case 6:
                            {
                                u2 u2 = *(u2*)source;
                                u8 u8 = *(u8*)(source + 2);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 2 + 8);

                                *(u2*)destination = u2;
                                *(u8*)(destination + 2) = u8;
                                Sse2.Store((u1*)destination + 2 + 8, xmm0);
                                offset = 26;
                                break;
                            }
                        case 7:
                            {
                                u1 u1 = *(u1*)source;
                                u8 u8 = *(u8*)(source + 1);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 1 + 8);

                                *(u1*)destination = u1;
                                *(u8*)(destination + 1) = u8;
                                Sse2.Store((u1*)destination + 1 + 8, xmm0);
                                offset = 25;
                                break;
                            }
                        case 8:
                            {
                                u8 u8 = *(u8*)source;
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 8);

                                *(u8*)destination = u8;
                                Sse2.Store((u1*)destination + 8, xmm0);
                                offset = 24;
                                break;
                            }
                        case 9:
                            {
                                u1 u1 = *(u1*)source;
                                u2 u2 = *(u2*)(source + 1);
                                u4 u4 = *(u4*)(source + 1 + 2);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 1 + 2 + 4);

                                *(u1*)destination = u1;
                                *(u2*)(destination + 1) = u2;
                                *(u4*)(destination + 1 + 2) = u4;
                                Sse2.Store((u1*)destination + 1 + 2 + 4, xmm0);
                                offset = 23;
                                break;
                            }
                        case 10:
                            {
                                u2 u2 = *(u2*)source;
                                u4 u4 = *(u4*)(source + 2);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 2 + 4);

                                *(u2*)destination = u2;
                                *(u4*)(destination + 2) = u4;
                                Sse2.Store((u1*)destination + 2 + 4, xmm0);
                                offset = 22;
                                break;
                            }
                        case 11:
                            {
                                u1 u1 = *(u1*)source;
                                u4 u4 = *(u4*)(source + 1);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 1 + 4);

                                *(u1*)destination = u1;
                                *(u4*)(destination + 1) = u4;
                                Sse2.Store((u1*)destination + 1 + 4, xmm0);
                                offset = 21;
                                break;
                            }
                        case 12:
                            {
                                u4 u4 = *(u4*)source;
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 4);

                                *(u4*)destination = u4;
                                Sse2.Store((u1*)destination + 4, xmm0);
                                offset = 20;
                                break;
                            }
                        case 13:
                            {
                                u1 u1 = *(u1*)source;
                                u2 u2 = *(u2*)(source + 1);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 1 + 2);

                                *(u1*)destination = u1;
                                *(u2*)(destination + 1) = u2;
                                Sse2.Store((u1*)destination + 1 + 2, xmm0);
                                offset = 19;
                                break;
                            }
                        case 14:
                            {
                                u2 u2 = *(u2*)source;
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 2);

                                *(u2*)destination = u2;
                                Sse2.Store((u1*)destination + 2, xmm0);
                                offset = 18;
                                break;
                            }
                        case 15:
                            {
                                u1 u1 = *(u1*)source;
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 1);

                                *(u1*)destination = u1;
                                Sse2.Store((u1*)destination + 1, xmm0);
                                offset = 17;
                                break;
                            }
                        case 16:
                            {
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);

                                Sse2.Store((u1*)destination, xmm0);
                                offset = 16;
                                break;
                            }
                        case 17:
                            {
                                u1 u1 = *(u1*)source;
                                u2 u2 = *(u2*)(source + 1);
                                u4 u4 = *(u4*)(source + 1 + 2);
                                u8 u8 = *(u8*)(source + 1 + 2 + 4);

                                *(u1*)destination = u1;
                                *(u2*)(destination + 1) = u2;
                                *(u4*)(destination + 1 + 2) = u4;
                                *(u8*)(destination + 1 + 2 + 4) = u8;
                                offset = 15;
                                break;
                            }
                        case 18:
                            {
                                u1 u1 = *(u1*)source;
                                u4 u4 = *(u4*)(source + 1);
                                u8 u8 = *(u8*)(source + 1 + 4);

                                *(u1*)destination = u1;
                                *(u4*)(destination + 1) = u4;
                                *(u8*)(destination + 1 + 4) = u8;
                                offset = 14;
                                break;
                            }
                        case 19:
                            {
                                u1 u1 = *(u1*)source;
                                u4 u4 = *(u4*)(source + 1);
                                u8 u8 = *(u8*)(source + 1 + 4);

                                *(u1*)destination = u1;
                                *(u4*)(destination + 1) = u4;
                                *(u8*)(destination + 1 + 4) = u8;
                                offset = 13;
                                break;
                            }
                        case 20:
                            {
                                u4 u4 = *(u4*)source;
                                u8 u8 = *(u8*)(source + 4);

                                *(u4*)destination = u4;
                                *(u8*)(destination + 4) = u8;
                                offset = 12;
                                break;
                            }
                        case 21:
                            {
                                u1 u1 = *(u1*)source;
                                u2 u2 = *(u2*)(source + 1);
                                u8 u8 = *(u8*)(source + 1 + 2);

                                *(u1*)destination = u1;
                                *(u2*)(destination + 1) = u2;
                                *(u8*)(destination + 1 + 2) = u8;
                                offset = 11;
                                break;
                            }
                        case 22:
                            {
                                u2 u2 = *(u2*)source;
                                u8 u8 = *(u8*)(source + 2);

                                *(u2*)destination = u2;
                                *(u8*)(destination + 2) = u8;
                                offset = 10;
                                break;
                            }
                        case 23:
                            {
                                u1 u1 = *(u1*)source;
                                u8 u8 = *(u8*)(source + 1);

                                *(u1*)destination = u1;
                                *(u8*)(destination + 1) = u8;
                                offset = 9;
                                break;
                            }
                        case 24:
                            {
                                u8 u8 = *(u8*)source;

                                *(u8*)destination = u8;
                                offset = 8;
                                break;
                            }
                        case 25:
                            {
                                u1 u1 = *(u1*)source;
                                u2 u2 = *(u2*)(source + 1);
                                u4 u4 = *(u4*)(source + 1 + 2);

                                *(u1*)destination = u1;
                                *(u2*)(destination + 1) = u2;
                                *(u4*)(destination + 1 + 2) = u4;
                                offset = 7;
                                break;
                            }
                        case 26:
                            {
                                u2 u2 = *(u2*)source;
                                u4 u4 = *(u4*)(source + 2);

                                *(u2*)destination = u2;
                                *(u4*)(destination + 2) = u4;
                                offset = 6;
                                break;
                            }
                        case 27:
                            {
                                u1 u1 = *(u1*)source;
                                u4 u4 = *(u4*)(source + 1);

                                *(u1*)destination = u1;
                                *(u4*)(destination + 1) = u4;
                                offset = 5;
                                break;
                            }
                        case 28:
                            {
                                u4 u4 = *(u4*)source;

                                *(u4*)destination = u4;
                                offset = 4;
                                break;
                            }
                        case 29:
                            {
                                u1 u1 = *(u1*)source;
                                u2 u2 = *(u2*)(source + 1);

                                *(u1*)destination = u1;
                                *(u2*)(destination + 1) = u2;
                                offset = 3;
                                break;
                            }
                        case 30:
                            {
                                u2 u2 = *(u2*)source;

                                *(u2*)destination = u2;
                                offset = 2;
                                break;
                            }
                        default:
                            {
                                u1 u1 = *(u1*)source;

                                *(u1*)destination = u1;
                                offset = 1;
                                break;
                            }
                    }

                    length -= offset;

                DO_NOT_CALC_REMINDER:
                    for (var bunchLength = length & ~127U; offset < bunchLength; offset += 128)
                    {
                        ymm0 = Avx.LoadAlignedVector256((u1*)source + offset);
                        ymm1 = Avx.LoadAlignedVector256((u1*)source + offset + 32U);
                        ymm2 = Avx.LoadAlignedVector256((u1*)source + offset + 32U * 2);
                        ymm3 = Avx.LoadAlignedVector256((u1*)source + offset + 32U * 3);

                        Avx.Store((u1*)destination + offset, ymm0);
                        Avx.Store((u1*)destination + offset + 32U, ymm1);
                        Avx.Store((u1*)destination + offset + 32U * 2, ymm2);
                        Avx.Store((u1*)destination + offset + 32U * 3, ymm3);
                    }

                    switch (length & 127U)
                    {
                        case 0:
                            {
                                return;
                            }
                        case 1:
                            {
                                u1 u1 = *(u1*)(source + offset);

                                *(u1*)(destination + offset) = u1;
                                return;
                            }
                        case 2:
                            {
                                u2 u2 = *(u2*)(source + offset);

                                *(u2*)(destination + offset) = u2;
                                return;
                            }
                        case 3:
                            {
                                source += offset;
                                destination += offset;

                                u2 u2 = *(u2*)source;
                                u1 u1 = *(u1*)(source + 2);

                                *(u2*)destination = u2;
                                *(u1*)(destination + 2) = u1;
                                return;
                            }
                        case 4:
                            {
                                u4 u4 = *(u4*)(source + offset);

                                *(u4*)(destination + offset) = u4;
                                return;
                            }
                        case 5:
                            {
                                source += offset;
                                destination += offset;

                                u4 u4 = *(u4*)source;
                                u1 u1 = *(u1*)(source + 4);

                                *(u4*)destination = u4;
                                *(u1*)(destination + 4) = u1;
                                return;
                            }
                        case 6:
                            {
                                source += offset;
                                destination += offset;

                                u4 u4 = *(u4*)source;
                                u2 u2 = *(u2*)(source + 4);

                                *(u4*)destination = u4;
                                *(u2*)(destination + 4) = u2;
                                return;
                            }
                        case 7:
                            {
                                source += offset;
                                destination += offset;

                                u4 u4 = *(u4*)source;
                                u2 u2 = *(u2*)(source + 4);
                                u1 u1 = *(u1*)(source + 4 + 2);

                                *(u4*)destination = u4;
                                *(u2*)(destination + 4) = u2;
                                *(u1*)(destination + 4 + 2) = u1;
                                return;
                            }
                        case 8:
                            {
                                u8 u8 = *(u8*)(source + offset);

                                *(u8*)(destination + offset) = u8;
                                return;
                            }
                        case 9:
                            {
                                source += offset;
                                destination += offset;

                                u8 u8 = *(u8*)source;
                                u1 u1 = *(u1*)(source + 8);

                                *(u8*)destination = u8;
                                *(u1*)(destination + 8) = u1;
                                return;
                            }
                        case 10:
                            {
                                source += offset;
                                destination += offset;

                                u8 u8 = *(u8*)source;
                                u2 u2 = *(u2*)(source + 8);

                                *(u8*)destination = u8;
                                *(u2*)(destination + 8) = u2;
                                return;
                            }
                        case 11:
                            {
                                source += offset;
                                destination += offset;

                                u8 u8 = *(u8*)source;
                                u2 u2 = *(u2*)(source + 8);
                                u1 u1 = *(u1*)(source + 8 + 2);

                                *(u8*)destination = u8;
                                *(u2*)(destination + 8) = u2;
                                *(u1*)(destination + 8 + 2) = u1;
                                return;
                            }
                        case 12:
                            {
                                source += offset;
                                destination += offset;

                                u8 u8 = *(u8*)source;
                                u4 u4 = *(u4*)(source + 8);

                                *(u8*)destination = u8;
                                *(u4*)(destination + 8) = u4;
                                return;
                            }
                        case 13:
                            {
                                source += offset;
                                destination += offset;

                                u8 u8 = *(u8*)source;
                                u4 u4 = *(u4*)(source + 8);
                                u1 u1 = *(u1*)(source + 8 + 4);

                                *(u8*)destination = u8;
                                *(u4*)(destination + 8) = u4;
                                *(u1*)(destination + 8 + 4) = u1;
                                return;
                            }
                        case 14:
                            {
                                source += offset;
                                destination += offset;

                                u8 u8 = *(u8*)source;
                                u4 u4 = *(u4*)(source + 8);
                                u2 u2 = *(u2*)(source + 8 + 4);

                                *(u8*)destination = u8;
                                *(u4*)(destination + 8) = u4;
                                *(u2*)(destination + 8 + 4) = u2;
                                return;
                            }
                        case 15:
                            {
                                source += offset;
                                destination += offset;

                                u8 u8 = *(u8*)source;
                                u4 u4 = *(u4*)(source + 8);
                                u2 u2 = *(u2*)(source + 8 + 4);
                                u1 u1 = *(u1*)(source + 8 + 4 + 2);

                                *(u8*)destination = u8;
                                *(u4*)(destination + 8) = u4;
                                *(u2*)(destination + 8 + 4) = u2;
                                *(u1*)(destination + 8 + 4 + 2) = u1;
                                return;
                            }
                        case 16:
                            {
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + offset);

                                Sse2.Store((u1*)destination + offset, xmm0);
                                return;
                            }
                        case 17:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u1 u1 = *(u1*)(source + 16);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u1*)(destination + 16) = u1;
                                return;
                            }
                        case 18:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u2 u2 = *(u2*)(source + 16);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u2*)(destination + 16) = u2;
                                return;
                            }
                        case 19:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u2 u2 = *(u2*)(source + 16);
                                u1 u1 = *(u1*)(source + 16 + 2);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u2*)(destination + 16) = u2;
                                *(u1*)(destination + 16 + 2) = u1;
                                return;
                            }
                        case 20:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u4 u4 = *(u4*)(source + 16);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u4*)(destination + 16) = u4;
                                return;
                            }
                        case 21:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u4 u4 = *(u4*)(source + 16);
                                u1 u1 = *(u1*)(source + 16 + 4);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u4*)(destination + 16) = u4;
                                *(u1*)(destination + 16 + 4) = u1;
                                return;
                            }
                        case 22:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u4 u4 = *(u4*)(source + 16);
                                u2 u2 = *(u2*)(source + 16 + 4);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u4*)(destination + 16) = u4;
                                *(u2*)(destination + 16 + 4) = u2;
                                return;
                            }
                        case 23:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u4 u4 = *(u4*)(source + 16);
                                u2 u2 = *(u2*)(source + 16 + 4);
                                u1 u1 = *(u1*)(source + 16 + 4 + 2);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u4*)(destination + 16) = u4;
                                *(u2*)(destination + 16 + 4) = u2;
                                *(u1*)(destination + 16 + 4 + 2) = u1;
                                return;
                            }
                        case 24:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u8 u8 = *(u8*)(source + 16);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u8*)(destination + 16) = u8;
                                return;
                            }
                        case 25:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u8 u8 = *(u8*)(source + 16);
                                u1 u1 = *(u1*)(source + 16 + 8);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u8*)(destination + 16) = u8;
                                *(u1*)(destination + 16 + 8) = u1;
                                return;
                            }
                        case 26:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u8 u8 = *(u8*)(source + 16);
                                u2 u2 = *(u2*)(source + 16 + 8);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u8*)(destination + 16) = u8;
                                *(u2*)(destination + 16 + 8) = u2;
                                return;
                            }
                        case 27:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u8 u8 = *(u8*)(source + 16);
                                u2 u2 = *(u2*)(source + 16 + 8);
                                u1 u1 = *(u1*)(source + 16 + 8 + 2);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u8*)(destination + 16) = u8;
                                *(u2*)(destination + 16 + 8) = u2;
                                *(u1*)(destination + 16 + 8 + 2) = u1;
                                return;
                            }
                        case 28:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u8 u8 = *(u8*)(source + 16);
                                u4 u4 = *(u4*)(source + 16 + 8);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u8*)(destination + 16) = u8;
                                *(u4*)(destination + 16 + 8) = u4;
                                return;
                            }
                        case 29:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u8 u8 = *(u8*)(source + 16);
                                u4 u4 = *(u4*)(source + 16 + 8);
                                u1 u1 = *(u1*)(source + 16 + 8 + 4);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u8*)(destination + 16) = u8;
                                *(u4*)(destination + 16 + 8) = u4;
                                *(u1*)(destination + 16 + 8 + 4) = u1;
                                return;
                            }
                        case 30:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u8 u8 = *(u8*)(source + 16);
                                u4 u4 = *(u4*)(source + 16 + 8);
                                u2 u2 = *(u2*)(source + 16 + 8 + 4);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u8*)(destination + 16) = u8;
                                *(u4*)(destination + 16 + 8) = u4;
                                *(u2*)(destination + 16 + 8 + 4) = u2;
                                return;
                            }
                        case 31:
                            {
                                source += offset;
                                destination += offset;

                                xmm0 = Sse2.LoadAlignedVector128((u1*)source);
                                u8 u8 = *(u8*)(source + 16);
                                u4 u4 = *(u4*)(source + 16 + 8);
                                u2 u2 = *(u2*)(source + 16 + 8 + 4);
                                u1 u1 = *(u1*)(source + 16 + 8 + 4 + 2);

                                Sse2.Store((u1*)destination, xmm0);
                                *(u8*)(destination + 16) = u8;
                                *(u4*)(destination + 16 + 8) = u4;
                                *(u2*)(destination + 16 + 8 + 4) = u2;
                                *(u1*)(destination + 16 + 8 + 4 + 2) = u1;
                                return;
                            }
                        case 32:
                            {
                                ymm0 = Avx.LoadAlignedVector256((u1*)source + offset);

                                Avx.Store((u1*)destination + offset, ymm0);
                                return;
                            }
                        case 33:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u1 u1 = *(u1*)(source + 32);

                                Avx.Store((u1*)destination, ymm0);
                                *(u1*)(destination + 32) = u1;
                                return;
                            }
                        case 34:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u2 u2 = *(u2*)(source + 32);

                                Avx.Store((u1*)destination, ymm0);
                                *(u2*)(destination + 32) = u2;
                                return;
                            }
                        case 35:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u2 u2 = *(u2*)(source + 32);
                                u1 u1 = *(u1*)(source + 32 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                *(u2*)(destination + 32) = u2;
                                *(u1*)(destination + 32 + 2) = u1;
                                return;
                            }
                        case 36:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u4 u4 = *(u4*)(source + 32);

                                Avx.Store((u1*)destination, ymm0);
                                *(u4*)(destination + 32) = u4;
                                return;
                            }
                        case 37:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u4 u4 = *(u4*)(source + 32);
                                u1 u1 = *(u1*)(source + 32 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                *(u4*)(destination + 32) = u4;
                                *(u1*)(destination + 32 + 4) = u1;
                                return;
                            }
                        case 38:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u4 u4 = *(u4*)(source + 32);
                                u2 u2 = *(u2*)(source + 32 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                *(u4*)(destination + 32) = u4;
                                *(u2*)(destination + 32 + 4) = u2;
                                return;
                            }
                        case 39:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u4 u4 = *(u4*)(source + 32);
                                u2 u2 = *(u2*)(source + 32 + 4);
                                u1 u1 = *(u1*)(source + 32 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                *(u4*)(destination + 32) = u4;
                                *(u2*)(destination + 32 + 4) = u2;
                                *(u1*)(destination + 32 + 4 + 2) = u1;
                                return;
                            }
                        case 40:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u8 u8 = *(u8*)(source + 32);

                                Avx.Store((u1*)destination, ymm0);
                                *(u8*)(destination + 32) = u8;
                                return;
                            }
                        case 41:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u8 u8 = *(u8*)(source + 32);
                                u1 u1 = *(u1*)(source + 32 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                *(u8*)(destination + 32) = u8;
                                *(u1*)(destination + 32 + 8) = u1;
                                return;
                            }
                        case 42:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u8 u8 = *(u8*)(source + 32);
                                u2 u2 = *(u2*)(source + 32 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                *(u8*)(destination + 32) = u8;
                                *(u2*)(destination + 32 + 8) = u2;
                                return;
                            }
                        case 43:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u8 u8 = *(u8*)(source + 32);
                                u2 u2 = *(u2*)(source + 32 + 8);
                                u1 u1 = *(u1*)(source + 32 + 8 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                *(u8*)(destination + 32) = u8;
                                *(u2*)(destination + 32 + 8) = u2;
                                *(u1*)(destination + 32 + 8 + 2) = u1;
                                return;
                            }
                        case 44:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u8 u8 = *(u8*)(source + 32);
                                u4 u4 = *(u4*)(source + 32 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                *(u8*)(destination + 32) = u8;
                                *(u4*)(destination + 32 + 8) = u4;
                                return;
                            }
                        case 45:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u8 u8 = *(u8*)(source + 32);
                                u4 u4 = *(u4*)(source + 32 + 8);
                                u1 u1 = *(u1*)(source + 32 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                *(u8*)(destination + 32) = u8;
                                *(u4*)(destination + 32 + 8) = u4;
                                *(u1*)(destination + 32 + 8 + 4) = u1;
                                return;
                            }
                        case 46:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u8 u8 = *(u8*)(source + 32);
                                u4 u4 = *(u4*)(source + 32 + 8);
                                u2 u2 = *(u2*)(source + 32 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                *(u8*)(destination + 32) = u8;
                                *(u4*)(destination + 32 + 8) = u4;
                                *(u2*)(destination + 32 + 8 + 4) = u2;
                                return;
                            }
                        case 47:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                u8 u8 = *(u8*)(source + 32);
                                u4 u4 = *(u4*)(source + 32 + 8);
                                u2 u2 = *(u2*)(source + 32 + 8 + 4);
                                u1 u1 = *(u1*)(source + 32 + 8 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                *(u8*)(destination + 32) = u8;
                                *(u4*)(destination + 32 + 8) = u4;
                                *(u2*)(destination + 32 + 8 + 4) = u2;
                                *(u1*)(destination + 32 + 8 + 4 + 2) = u1;
                                return;
                            }
                        case 48:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                return;
                            }
                        case 49:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u1 u1 = *(u1*)(source + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u1*)(destination + 32 + 16) = u1;
                                return;
                            }
                        case 50:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u2 u2 = *(u2*)(source + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u2*)(destination + 32 + 16) = u2;
                                return;
                            }
                        case 51:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u2 u2 = *(u2*)(source + 32 + 16);
                                u1 u1 = *(u1*)(source + 32 + 16 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u2*)(destination + 32 + 16) = u2;
                                *(u1*)(destination + 32 + 16 + 2) = u1;
                                return;
                            }
                        case 52:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u4 u4 = *(u4*)(source + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u4*)(destination + 32 + 16) = u4;
                                return;
                            }
                        case 53:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u4 u4 = *(u4*)(source + 32 + 16);
                                u1 u1 = *(u1*)(source + 32 + 16 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u4*)(destination + 32 + 16) = u4;
                                *(u1*)(destination + 32 + 16 + 4) = u1;
                                return;
                            }
                        case 54:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u4 u4 = *(u4*)(source + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 16 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u4*)(destination + 32 + 16) = u4;
                                *(u2*)(destination + 32 + 16 + 4) = u2;
                                return;
                            }
                        case 55:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u4 u4 = *(u4*)(source + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 16 + 4);
                                u1 u1 = *(u1*)(source + 32 + 16 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u4*)(destination + 32 + 16) = u4;
                                *(u2*)(destination + 32 + 16 + 4) = u2;
                                *(u1*)(destination + 32 + 16 + 4 + 2) = u1;
                                return;
                            }
                        case 56:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u8*)(destination + 32 + 16) = u8;
                                return;
                            }
                        case 57:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 16);
                                u1 u1 = *(u1*)(source + 32 + 16 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u8*)(destination + 32 + 16) = u8;
                                *(u1*)(destination + 32 + 16 + 8) = u1;
                                return;
                            }
                        case 58:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 16 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u8*)(destination + 32 + 16) = u8;
                                *(u2*)(destination + 32 + 16 + 8) = u2;
                                return;
                            }
                        case 59:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 16 + 8);
                                u1 u1 = *(u1*)(source + 32 + 16 + 8 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u8*)(destination + 32 + 16) = u8;
                                *(u2*)(destination + 32 + 16 + 8) = u2;
                                *(u1*)(destination + 32 + 16 + 8 + 2) = u1;
                                return;
                            }
                        case 60:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 16 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u8*)(destination + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 16 + 8) = u4;
                                return;
                            }
                        case 61:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 16 + 8);
                                u1 u1 = *(u1*)(source + 32 + 16 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u8*)(destination + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 16 + 8) = u4;
                                *(u1*)(destination + 32 + 16 + 8 + 4) = u1;
                                return;
                            }
                        case 62:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 16 + 8);
                                u2 u2 = *(u2*)(source + 32 + 16 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u8*)(destination + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 16 + 8) = u4;
                                *(u2*)(destination + 32 + 16 + 8 + 4) = u2;
                                return;
                            }
                        case 63:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 16 + 8);
                                u2 u2 = *(u2*)(source + 32 + 16 + 8 + 4);
                                u1 u1 = *(u1*)(source + 32 + 16 + 8 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Sse2.Store((u1*)destination + 32, xmm0);
                                *(u8*)(destination + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 16 + 8) = u4;
                                *(u2*)(destination + 32 + 16 + 8 + 4) = u2;
                                *(u1*)(destination + 32 + 16 + 8 + 4 + 2) = u1;
                                return;
                            }
                        case 64:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                return;
                            }
                        case 65:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u1 u1 = *(u1*)(source + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u1*)(destination + 32 + 32) = u1;
                                return;
                            }
                        case 66:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u2 u2 = *(u2*)(source + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u2*)(destination + 32 + 32) = u2;
                                return;
                            }
                        case 67:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u2 u2 = *(u2*)(source + 32 + 32);
                                u1 u1 = *(u1*)(source + 32 + 32 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u2*)(destination + 32 + 32) = u2;
                                *(u1*)(destination + 32 + 32 + 2) = u1;
                                return;
                            }
                        case 68:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u4 u4 = *(u4*)(source + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u4*)(destination + 32 + 32) = u4;
                                return;
                            }
                        case 69:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u4 u4 = *(u4*)(source + 32 + 32);
                                u1 u1 = *(u1*)(source + 32 + 32 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u4*)(destination + 32 + 32) = u4;
                                *(u1*)(destination + 32 + 32 + 4) = u1;
                                return;
                            }
                        case 70:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u4 u4 = *(u4*)(source + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u4*)(destination + 32 + 32) = u4;
                                *(u2*)(destination + 32 + 32 + 4) = u2;
                                return;
                            }
                        case 71:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u4 u4 = *(u4*)(source + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 4);
                                u1 u1 = *(u1*)(source + 32 + 32 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u4*)(destination + 32 + 32) = u4;
                                *(u2*)(destination + 32 + 32 + 4) = u2;
                                *(u1*)(destination + 32 + 32 + 4 + 2) = u1;
                                return;
                            }
                        case 72:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u8*)(destination + 32 + 32) = u8;
                                return;
                            }
                        case 73:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 32);
                                u1 u1 = *(u1*)(source + 32 + 32 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u8*)(destination + 32 + 32) = u8;
                                *(u1*)(destination + 32 + 32 + 8) = u1;
                                return;
                            }
                        case 74:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u8*)(destination + 32 + 32) = u8;
                                *(u2*)(destination + 32 + 32 + 8) = u2;
                                return;
                            }
                        case 75:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 8);
                                u1 u1 = *(u1*)(source + 32 + 32 + 8 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u8*)(destination + 32 + 32) = u8;
                                *(u2*)(destination + 32 + 32 + 8) = u2;
                                *(u1*)(destination + 32 + 32 + 8 + 2) = u1;
                                return;
                            }
                        case 76:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u8*)(destination + 32 + 32) = u8;
                                *(u4*)(destination + 32 + 32 + 8) = u4;
                                return;
                            }
                        case 77:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 8);
                                u1 u1 = *(u1*)(source + 32 + 32 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u8*)(destination + 32 + 32) = u8;
                                *(u4*)(destination + 32 + 32 + 8) = u4;
                                *(u1*)(destination + 32 + 32 + 8 + 4) = u1;
                                return;
                            }
                        case 78:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 8);
                                u2 u2 = *(u2*)(source + 32 + 32 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u8*)(destination + 32 + 32) = u8;
                                *(u4*)(destination + 32 + 32 + 8) = u4;
                                *(u2*)(destination + 32 + 32 + 8 + 4) = u2;
                                return;
                            }
                        case 79:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                u8 u8 = *(u8*)(source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 8);
                                u2 u2 = *(u2*)(source + 32 + 32 + 8 + 4);
                                u1 u1 = *(u1*)(source + 32 + 32 + 8 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                *(u8*)(destination + 32 + 32) = u8;
                                *(u4*)(destination + 32 + 32 + 8) = u4;
                                *(u2*)(destination + 32 + 32 + 8 + 4) = u2;
                                *(u1*)(destination + 32 + 32 + 8 + 4 + 2) = u1;
                                return;
                            }
                        case 80:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                return;
                            }
                        case 81:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u1 u1 = *(u1*)(source + 32 + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u1*)(destination + 32 + 32 + 16) = u1;
                                return;
                            }
                        case 82:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u2*)(destination + 32 + 32 + 16) = u2;
                                return;
                            }
                        case 83:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 16);
                                u1 u1 = *(u1*)(source + 32 + 32 + 16 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u2*)(destination + 32 + 32 + 16) = u2;
                                *(u1*)(destination + 32 + 32 + 16 + 2) = u1;
                                return;
                            }
                        case 84:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u4*)(destination + 32 + 32 + 16) = u4;
                                return;
                            }
                        case 85:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 16);
                                u1 u1 = *(u1*)(source + 32 + 32 + 16 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u4*)(destination + 32 + 32 + 16) = u4;
                                *(u1*)(destination + 32 + 32 + 16 + 4) = u1;
                                return;
                            }
                        case 86:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 32 + 16 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u4*)(destination + 32 + 32 + 16) = u4;
                                *(u2*)(destination + 32 + 32 + 16 + 4) = u2;
                                return;
                            }
                        case 87:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 32 + 16 + 4);
                                u1 u1 = *(u1*)(source + 32 + 32 + 16 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u4*)(destination + 32 + 32 + 16) = u4;
                                *(u2*)(destination + 32 + 32 + 16 + 4) = u2;
                                *(u1*)(destination + 32 + 32 + 16 + 4 + 2) = u1;
                                return;
                            }
                        case 88:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 16) = u8;
                                return;
                            }
                        case 89:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 16);
                                u1 u1 = *(u1*)(source + 32 + 32 + 16 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 16) = u8;
                                *(u1*)(destination + 32 + 32 + 16 + 8) = u1;
                                return;
                            }
                        case 90:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 32 + 16 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 16) = u8;
                                *(u2*)(destination + 32 + 32 + 16 + 8) = u2;
                                return;
                            }
                        case 91:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 32 + 16 + 8);
                                u1 u1 = *(u1*)(source + 32 + 32 + 16 + 8 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 16) = u8;
                                *(u2*)(destination + 32 + 32 + 16 + 8) = u2;
                                *(u1*)(destination + 32 + 32 + 16 + 8 + 2) = u1;
                                return;
                            }
                        case 92:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 32 + 16 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 32 + 16 + 8) = u4;
                                return;
                            }
                        case 93:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 32 + 16 + 8);
                                u1 u1 = *(u1*)(source + 32 + 32 + 16 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 32 + 16 + 8) = u4;
                                *(u1*)(destination + 32 + 32 + 16 + 8 + 4) = u1;
                                return;
                            }
                        case 94:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 32 + 16 + 8);
                                u2 u2 = *(u2*)(source + 32 + 32 + 16 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 32 + 16 + 8) = u4;
                                *(u2*)(destination + 32 + 32 + 16 + 8 + 4) = u2;
                                return;
                            }
                        case 95:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 32 + 16 + 8);
                                u2 u2 = *(u2*)(source + 32 + 32 + 16 + 8 + 4);
                                u1 u1 = *(u1*)(source + 32 + 32 + 16 + 8 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Sse2.Store((u1*)destination + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 32 + 16 + 8) = u4;
                                *(u2*)(destination + 32 + 32 + 16 + 8 + 4) = u2;
                                *(u1*)(destination + 32 + 32 + 16 + 8 + 4 + 2) = u1;
                                return;
                            }
                        case 96:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                return;
                            }
                        case 97:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u1*)(destination + 32 + 32 + 32) = u1;
                                return;
                            }
                        case 98:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u2*)(destination + 32 + 32 + 32) = u2;
                                return;
                            }
                        case 99:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u2*)(destination + 32 + 32 + 32) = u2;
                                *(u1*)(destination + 32 + 32 + 32 + 2) = u1;
                                return;
                            }
                        case 100:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u4*)(destination + 32 + 32 + 32) = u4;
                                return;
                            }
                        case 101:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u4*)(destination + 32 + 32 + 32) = u4;
                                *(u1*)(destination + 32 + 32 + 32 + 4) = u1;
                                return;
                            }
                        case 102:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u4*)(destination + 32 + 32 + 32) = u4;
                                *(u2*)(destination + 32 + 32 + 32 + 4) = u2;
                                return;
                            }
                        case 103:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 4);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u4*)(destination + 32 + 32 + 32) = u4;
                                *(u2*)(destination + 32 + 32 + 32 + 4) = u2;
                                *(u1*)(destination + 32 + 32 + 32 + 4 + 2) = u1;
                                return;
                            }
                        case 104:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u8*)(destination + 32 + 32 + 32) = u8;
                                return;
                            }
                        case 105:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u8*)(destination + 32 + 32 + 32) = u8;
                                *(u1*)(destination + 32 + 32 + 32 + 8) = u1;
                                return;
                            }
                        case 106:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u8*)(destination + 32 + 32 + 32) = u8;
                                *(u2*)(destination + 32 + 32 + 32 + 8) = u2;
                                return;
                            }
                        case 107:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 8);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 8 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u8*)(destination + 32 + 32 + 32) = u8;
                                *(u2*)(destination + 32 + 32 + 32 + 8) = u2;
                                *(u1*)(destination + 32 + 32 + 32 + 8 + 2) = u1;
                                return;
                            }
                        case 108:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u8*)(destination + 32 + 32 + 32) = u8;
                                *(u4*)(destination + 32 + 32 + 32 + 8) = u4;
                                return;
                            }
                        case 109:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 8);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u8*)(destination + 32 + 32 + 32) = u8;
                                *(u4*)(destination + 32 + 32 + 32 + 8) = u4;
                                *(u1*)(destination + 32 + 32 + 32 + 8 + 4) = u1;
                                return;
                            }
                        case 110:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 8);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u8*)(destination + 32 + 32 + 32) = u8;
                                *(u4*)(destination + 32 + 32 + 32 + 8) = u4;
                                *(u2*)(destination + 32 + 32 + 32 + 8 + 4) = u2;
                                return;
                            }
                        case 111:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 8);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 8 + 4);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 8 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                *(u8*)(destination + 32 + 32 + 32) = u8;
                                *(u4*)(destination + 32 + 32 + 32 + 8) = u4;
                                *(u2*)(destination + 32 + 32 + 32 + 8 + 4) = u2;
                                *(u1*)(destination + 32 + 32 + 32 + 8 + 4 + 2) = u1;
                                return;
                            }
                        case 112:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                return;
                            }
                        case 113:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u1*)(destination + 32 + 32 + 32 + 16) = u1;
                                return;
                            }
                        case 114:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u2*)(destination + 32 + 32 + 32 + 16) = u2;
                                return;
                            }
                        case 115:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 16);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 16 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u2*)(destination + 32 + 32 + 32 + 16) = u2;
                                *(u1*)(destination + 32 + 32 + 32 + 16 + 2) = u1;
                                return;
                            }
                        case 116:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u4*)(destination + 32 + 32 + 32 + 16) = u4;
                                return;
                            }
                        case 117:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 16);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 16 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u4*)(destination + 32 + 32 + 32 + 16) = u4;
                                *(u1*)(destination + 32 + 32 + 32 + 16 + 4) = u1;
                                return;
                            }
                        case 118:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 16 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u4*)(destination + 32 + 32 + 32 + 16) = u4;
                                *(u2*)(destination + 32 + 32 + 32 + 16 + 4) = u2;
                                return;
                            }
                        case 119:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 16 + 4);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 16 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u4*)(destination + 32 + 32 + 32 + 16) = u4;
                                *(u2*)(destination + 32 + 32 + 32 + 16 + 4) = u2;
                                *(u1*)(destination + 32 + 32 + 32 + 16 + 4 + 2) = u1;
                                return;
                            }
                        case 120:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32 + 16);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 32 + 16) = u8;
                                return;
                            }
                        case 121:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32 + 16);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 16 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 32 + 16) = u8;
                                *(u1*)(destination + 32 + 32 + 32 + 16 + 8) = u1;
                                return;
                            }
                        case 122:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 16 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 32 + 16) = u8;
                                *(u2*)(destination + 32 + 32 + 32 + 16 + 8) = u2;
                                return;
                            }
                        case 123:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32 + 16);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 16 + 8);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 16 + 8 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 32 + 16) = u8;
                                *(u2*)(destination + 32 + 32 + 32 + 16 + 8) = u2;
                                *(u1*)(destination + 32 + 32 + 32 + 16 + 8 + 2) = u1;
                                return;
                            }
                        case 124:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 16 + 8);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 32 + 32 + 16 + 8) = u4;
                                return;
                            }
                        case 125:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 16 + 8);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 16 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 32 + 32 + 16 + 8) = u4;
                                *(u1*)(destination + 32 + 32 + 32 + 16 + 8 + 4) = u1;
                                return;
                            }
                        case 126:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 16 + 8);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 16 + 8 + 4);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 32 + 32 + 16 + 8) = u4;
                                *(u2*)(destination + 32 + 32 + 32 + 16 + 8 + 4) = u2;
                                return;
                            }
                        default:
                            {
                                source += offset;
                                destination += offset;

                                ymm0 = Avx.LoadAlignedVector256((u1*)source);
                                ymm1 = Avx.LoadAlignedVector256((u1*)source + 32);
                                ymm2 = Avx.LoadAlignedVector256((u1*)source + 32 + 32);
                                xmm0 = Sse2.LoadAlignedVector128((u1*)source + 32 + 32 + 32);
                                u8 u8 = *(u8*)(source + 32 + 32 + 32 + 16);
                                u4 u4 = *(u4*)(source + 32 + 32 + 32 + 16 + 8);
                                u2 u2 = *(u2*)(source + 32 + 32 + 32 + 16 + 8 + 4);
                                u1 u1 = *(u1*)(source + 32 + 32 + 32 + 16 + 8 + 4 + 2);

                                Avx.Store((u1*)destination, ymm0);
                                Avx.Store((u1*)destination + 32, ymm1);
                                Avx.Store((u1*)destination + 32 + 32, ymm2);
                                Sse2.Store((u1*)destination + 32 + 32 + 32, xmm0);
                                *(u8*)(destination + 32 + 32 + 32 + 16) = u8;
                                *(u4*)(destination + 32 + 32 + 32 + 16 + 8) = u4;
                                *(u2*)(destination + 32 + 32 + 32 + 16 + 8 + 4) = u2;
                                *(u1*)(destination + 32 + 32 + 32 + 16 + 8 + 4 + 2) = u1;
                                return;
                            }
                    }
                }
        }
        
    }
}