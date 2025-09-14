using PatcherReference;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker.Internal;

// the main goal of this class is to provide an implementation that can be easily ported in case of missing components.
// it is not actually needed for the latest dotnet.
[InlineAllMembers]
unsafe class ObjectAllocator
{
    static int ManagedSizeOf<T>() => typeof(T).IsValueType ? sizeof(T) : sizeof(nint);

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
        Array.Copy(array, newArray, array.Length);

        var oldSize = array.Length * ManagedSizeOf<T>();
        var newSize = newLength * ManagedSizeOf<T>();
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

    /* body: { u4 length; fixed u2[length] } */
    public static void AllocateStringFromItsBody(/*pinned*/ ref object @object, byte* bodyPointer, uint* objectSize)
    {
        var length = *(uint*)bodyPointer;
        if (length == 0)
        {
            @object = string.Empty;
            *objectSize = 4;
            return;
        }

        @object = UnsafeAccessors.AllocateUninitializedString(length);

        var byteCount = length << 1;
        var source = bodyPointer + SizeOf.StringLength;
        var destination = LoadEffectiveAddress(@object, SizeOf.MethodTable + SizeOf.StringLength);
        Unsafe.CopyBlockUnaligned(destination, source, byteCount);

        *objectSize = byteCount + SizeOf.StringLength;
    }

    public static void AllocateStringFromItsBody(/*pinned*/ ref object @object, byte* bodyPointer)
    {
        var length = *(uint*)bodyPointer;
        if (length == 0)
        {
            @object = string.Empty;
            return;
        }

        @object = UnsafeAccessors.AllocateUninitializedString(length);

        var source = bodyPointer + SizeOf.StringLength;
        var destination = GetObjectBody(@object) + SizeOf.StringLength;
        var byteCount = length << 1;
        Unsafe.CopyBlockUnaligned(destination, source, byteCount);
    }
}
