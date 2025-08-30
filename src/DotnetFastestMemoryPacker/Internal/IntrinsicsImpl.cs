using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace DotnetFastestMemoryPacker.Internal;
unsafe class IntrinsicsImpl
{
    public static uint Sum(uint* array, uint length)
    {
        uint i = 0;
        uint totalSum = 0;

        if (Avx2.IsSupported)
        {
            const uint VectorSize = 8;
            var limit = length - (length & (VectorSize * 4) - 1);

            if (i < limit)
            {
                var sum0 = Avx.LoadVector256(array + i);
                var sum1 = Avx.LoadVector256(array + i + VectorSize);
                var sum2 = Avx.LoadVector256(array + i + VectorSize * 2);
                var sum3 = Avx.LoadVector256(array + i + VectorSize * 3);

                for (i += VectorSize * 4; i < limit; i += VectorSize * 4)
                {
                    sum0 = Avx2.Add(sum0, Avx.LoadVector256(array + i));
                    sum1 = Avx2.Add(sum1, Avx.LoadVector256(array + i + VectorSize));
                    sum2 = Avx2.Add(sum2, Avx.LoadVector256(array + i + VectorSize * 2));
                    sum3 = Avx2.Add(sum3, Avx.LoadVector256(array + i + VectorSize * 3));
                }

                totalSum += HorizontalSum(Avx2.Add(Avx2.Add(sum0, sum1), Avx2.Add(sum2, sum3)));
            }

            for (i = limit; i <= length - VectorSize; i += VectorSize)
                totalSum += HorizontalSum(Avx.LoadVector256(array + i));

            for (i = length - (length & VectorSize - 1); i < length; i++)
                totalSum += array[i];
        }
        else
        {
            for (; i < length; i++)
                totalSum += array[i];
        }

        return totalSum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint HorizontalSum(Vector256<uint> vector) => HorizontalSum(Sse2.Add(vector.GetLower(), vector.GetUpper()));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint HorizontalSum(Vector128<uint> vector)
    {
        var sum = Sse2.Add(vector, Sse2.Shuffle(vector, 0b_01_00_11_10));
        return sum.GetElement(0) + sum.GetElement(1);
    }
}