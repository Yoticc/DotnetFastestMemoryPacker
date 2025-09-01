using PatcherReference;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotnetFastestMemoryPacker.Internal;
[InlineAllMembers]
[StructLayout(LayoutKind.Explicit)]
unsafe struct MethodTable
{
    [FieldOffset(0x00)] public ushort ComponentSize;
    [FieldOffset(0x00)] uint flags;
    [FieldOffset(0x04)] public uint BaseSize;
    [FieldOffset(0x08)] uint flags2;
    [FieldOffset(0x10)] public MethodTable* ParentMethodTable;
    [FieldOffset(0x18)] public void* Module;
    [FieldOffset(0x20)] public MethodTableAuxiliaryData* AuxiliaryData;
    [FieldOffset(0x28)] nint canonMT;
    [FieldOffset(0x30)] public MethodTable* ElementType;

    public bool HasComponentSize => (flags & 0x80000000U) > 0U;
    public bool ContainsGCPointers => (flags & 0x1000000U) > 0U;
    public uint MultiDimensionalArrayRank => (BaseSize >> 3) - 3;
    public bool IsArray => (flags & 0xC0000U) == 0x80000U;
    public bool IsValueType => (flags & 0xC0000U) == 0x40000U;
    public bool IsCanonical => (canonMT & 1) == 0U;
    public bool HasInstantiation => (flags & 0x80000000U) == 0U && (flags & 0x30U) > 0U;

    public EEClass* Class => (EEClass*)(IsCanonical ? canonMT : ((MethodTable*)(canonMT & ~1))->canonMT);
    public MethodTable* CanonicalMethodTable => (MethodTable*)(IsCanonical ? (nint)Unsafe.AsPointer(ref this) : canonMT & ~1);

    public Type GetRuntimeType()
    {
        var type = AuxiliaryData->ExposedRuntimeType;
        if (type is null)
            type = Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr((nint)Unsafe.AsPointer(ref this)));

        return type;
    }
}