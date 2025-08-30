using System.Runtime.CompilerServices;

namespace DotnetFastestMemoryPacker.Internal;

// the main goal of this class is to provide an implementation that can be easily ported in case of missing components
// it is not actually needed for the latest dotnet
unsafe class Allocator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] AllocatePinnedArray<T>(int length)
    {
        return GC.AllocateArray<T>(length, pinned: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] AllocatePinnedUninitializedArray<T>(int length)
    {
        return GC.AllocateUninitializedArray<T>(length, pinned: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResizePinnedArrayForGCElements<T>(ref T[] array, int newSize)
    {
        var oldSize = array.Length;
        var newArray = AllocatePinnedUninitializedArray<T>(newSize);
        Array.Copy(array, newArray, oldSize);
        Unsafe.InitBlockUnaligned(GetArrayPointer(newArray) + newSize, 0, (uint)(newSize - oldSize));

        array = newArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResizePinnedArrayForNonGCElements<T>(ref T[] array, int newSize)
    {
        var oldSize = array.Length;
        var newArray = AllocatePinnedUninitializedArray<T>(newSize);
        Unsafe.CopyBlock(GetArrayPointer(newArray), GetArrayPointer(array), (uint)oldSize);
        Unsafe.InitBlockUnaligned(GetArrayPointer(newArray) + newSize, 0, (uint)(newSize - oldSize));

        array = newArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResizePinnedUninitializedArrayForGCElements<T>(ref T[] array, int newSize)
    {
        var oldSize = array.Length;
        var newArray = AllocatePinnedUninitializedArray<T>(newSize);
        Array.Copy(array, newArray, oldSize);

        array = newArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResizePinnedUninitializedArrayForNonGCElements<T>(ref T[] array, int newSize)
    {
        var oldSize = array.Length;
        var newArray = AllocatePinnedUninitializedArray<T>(newSize);
        Unsafe.CopyBlock(GetArrayPointer(newArray), GetArrayPointer(array), (uint)oldSize);

        array = newArray;
    }
}
