using System.Runtime.InteropServices;

namespace DotnetFastestMemoryPacker.Internal;

[StructLayout(LayoutKind.Explicit)]
unsafe struct FieldDesc
{
    [FieldOffset(0x00)] MethodTable* DeclaringMethodTable;
    [FieldOffset(0x08)] uint dword1;
    [FieldOffset(0x0C)] uint dword2;

    public bool IsStatic => (dword1 & 1 << 24) > 0;

    public uint Offset => dword2 & (1 << 21) - 1;
    public CorElementType Type => (CorElementType)((dword2 >> 27) & (1 << 5) - 1);
}

enum CorElementType : byte
{
    End = 0,
    Void = 1,
    Boolean = 2,
    Char = 3,
    I1 = 4,
    U1 = 5,
    I2 = 6,
    U2 = 7,
    I4 = 8,
    U4 = 9,
    I8 = 10,
    U8 = 11,
    R4 = 12,
    R8 = 13,
    String = 14,
    Pointer = 15,
    ByRef = 16,
    ValueType = 17,
    Class = 18,
    Var = 19,
    Array = 20,
    GenericInst = 21,
    TypedByRef = 20,
    I = 24,
    U = 25,
    FnPtr = 27,
    Object = 28,
    SZArray = 29,
    MVar = 30,
    CModReqd = 31,
    CModOpt = 32,
    Internal = 33,
    Max = 34,
    Modifier = 64,
    Sentinel = 65,
    Pinned = 69
}