using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace DotnetFastestMemoryPacker.Internal;
unsafe class IntrinsicsImpl
{
    public static uint Sum(uint* array, uint length)
    {
        uint sum, offset;
        if (!Avx2.IsSupported || length <= 7U)
        {
            sum = array[0];
            for (offset = 1; offset < length; offset++)
                sum += array[offset];

            return sum;
        }

        const uint VectorSize = 8;
        const uint QuadroVectorSize = VectorSize * 4;

        var ymm0 = Avx.LoadVector256(array);
        offset = 0U;
        var limit = length & ~31U;
        if (offset < limit)
        {
            var ymm1 = Avx.LoadVector256(array + offset + VectorSize);
            var ymm2 = Avx.LoadVector256(array + offset + VectorSize * 2);
            var ymm3 = Avx.LoadVector256(array + offset + VectorSize * 3);

            for (offset = QuadroVectorSize; offset < limit; offset += QuadroVectorSize)
            {
                ymm0 = Avx2.Add(ymm0, Avx.LoadVector256(array + offset));
                ymm1 = Avx2.Add(ymm1, Avx.LoadVector256(array + offset + VectorSize));
                ymm2 = Avx2.Add(ymm2, Avx.LoadVector256(array + offset + VectorSize * 2));
                ymm3 = Avx2.Add(ymm3, Avx.LoadVector256(array + offset + VectorSize * 3));
            }

            ymm0 = Avx2.Add(ymm0, Avx2.Add(ymm1, Avx2.Add(ymm2, ymm3)));
            offset -= VectorSize;
        }
        offset += VectorSize;

        limit = length & ~7U;
        for (; offset < limit; offset += VectorSize)
            ymm0 = Avx2.Add(ymm0, Avx.LoadVector256(array + offset));

        var xmm0 = Sse2.Add(ymm0.GetLower(), ymm0.GetUpper());
        var xmm1 = Sse2.Add(xmm0, Sse2.Shuffle(xmm0, 0b_01_00_11_10));
        sum = xmm1.GetElement(0) + xmm1.GetElement(1);

        for (; offset < length; offset++)
            sum += array[offset];

        return sum;
    }
}