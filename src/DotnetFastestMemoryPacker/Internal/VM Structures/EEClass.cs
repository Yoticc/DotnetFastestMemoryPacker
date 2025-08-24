using System.Runtime.InteropServices;

namespace DotnetFastestMemoryPacker.Internal;
[StructLayout(LayoutKind.Explicit)]
unsafe struct EEClass
{
    [FieldOffset(0x10)] public MethodTable* MethodTable;
    [FieldOffset(0x18)] public FieldDesc* FieldDesc;
    [FieldOffset(0x40)] public byte NormType;
    [FieldOffset(0x41)] public byte BaseSizePadding;
    [FieldOffset(0x42)] public short NumInstanceFields;

    public bool IsArray => NormType == CorElementType.Array || NormType == CorElementType.SZArray;
    public bool IsValueType => NormType == CorElementType.ValueType;
}