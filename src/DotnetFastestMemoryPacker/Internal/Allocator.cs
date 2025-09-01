using PatcherReference;
using System.Runtime.CompilerServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker.Internal;

// the main goal of this class is to provide an implementation that can be easily ported in case of missing components.
// it is not actually needed for the latest dotnet.
[InlineAllMembers]
unsafe class Allocator
{
    static int SizeOf<T>() => typeof(T).IsValueType ? sizeof(T) : sizeof(nint);

    public static T[] AllocatePinnedArray<T>(int length)
    {
        return GC.AllocateArray<T>(length, pinned: true);
    }

    public static T[] AllocatePinnedUninitializedArray<T>(int length)
    {
        return GC.AllocateUninitializedArray<T>(length, pinned: true);
    }

    public static void ResizePinnedArrayForGCElements<T>(ref T[] array, int newLength)
    {
        var newArray = AllocatePinnedUninitializedArray<T>(newLength);
        // slow, heavy and safe way, but it is supposed to be called rarely so it is not a problem
        Array.Copy(array, newArray, array.Length);

        var oldSize = array.Length * SizeOf<T>();
        var newSize = newLength * SizeOf<T>();
        Unsafe.InitBlockUnaligned((byte*)GetArrayBody(newArray) + oldSize, 0, (uint)(newSize - oldSize));

        array = newArray;
    }

    public static void ResizePinnedArrayForNonGCElements<T>(ref T[] array, int newElementsCount) where T : unmanaged
    {
        var oldSize = array.Length * sizeof(T);
        var newSize = newElementsCount * sizeof(T);
        var newArray = AllocatePinnedUninitializedArray<T>(newElementsCount);
        var newArrayPointer = (byte*)GetArrayBody(newArray);
        Unsafe.CopyBlock(newArrayPointer, GetArrayBody(array), (uint)oldSize);
        Unsafe.InitBlockUnaligned(newArrayPointer + oldSize, 0, (uint)(newSize - oldSize));

        array = newArray;
    }

    public static void ResizePinnedUninitializedArrayForGCElements<T>(ref T[] array, int newElementsCount)
    {
        var newArray = AllocatePinnedUninitializedArray<T>(newElementsCount);
        Array.Copy(array, newArray, newElementsCount);
        array = newArray;
    }

    public static void ResizePinnedUninitializedArrayForNonGCElements<T>(ref T[] array, int newElementsCount) where T : unmanaged
    {
        var oldSize = array.Length * sizeof(T);
        var newArray = AllocatePinnedUninitializedArray<T>(newElementsCount);
        Unsafe.CopyBlock(GetArrayBody(newArray), GetArrayBody(array), (uint)oldSize);
        array = newArray;
    }
}
