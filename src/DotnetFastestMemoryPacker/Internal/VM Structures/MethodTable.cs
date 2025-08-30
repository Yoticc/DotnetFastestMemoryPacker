using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotnetFastestMemoryPacker.Internal;
[StructLayout(LayoutKind.Explicit)]
unsafe struct MethodTable
{
    [FieldOffset(0x00)] public ushort ComponentSize;
    [FieldOffset(0x00)] uint flags;
    [FieldOffset(0x04)] public uint BaseSize;
    [FieldOffset(0x08)] uint flags2;
    [FieldOffset(0x10)] public MethodTable* ParentMethodTable;
    [FieldOffset(0x28)] nint canonMT;
    [FieldOffset(0x30)] public MethodTable* ElementType;

    public bool HasComponentSize => (flags & 0x80000000U) > 0U;
    public bool ContainsGCPointers => (flags & 0x1000000U) > 0U;
    public uint MultiDimensionalArrayRank => (BaseSize >> 3) - 3;
    public bool IsArray => (flags & 0xC0000) == 0x80000;
    public bool IsValueType => (flags & 786432U) == 262144U;

    public EEClass* Class
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (EEClass*)((canonMT & 1) == 0 ? canonMT : ((MethodTable*)(canonMT & ~1))->canonMT);
    }

    public MethodTable* CanonicalMethodTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (MethodTable*)((canonMT & 1) == 0 ? (nint)Unsafe.AsPointer(ref this) : canonMT & ~1);
    }
}