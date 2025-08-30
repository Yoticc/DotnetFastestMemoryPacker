using PatcherReference;

namespace DotnetFastestMemoryPacker.Internal;

[ShouldBeTrimmed]
struct CorElementType
{
    public const byte End = 0;
    public const byte Void = 1;
    public const byte Boolean = 2;
    public const byte Char = 3;
    public const byte I1 = 4;
    public const byte U1 = 5;
    public const byte I2 = 6;
    public const byte U2 = 7;
    public const byte I4 = 8;
    public const byte U4 = 9;
    public const byte I8 = 10;
    public const byte U8 = 11;
    public const byte R4 = 12;
    public const byte R8 = 13;
    public const byte String = 14;
    public const byte Pointer = 15;
    public const byte ByRef = 16;
    public const byte ValueType = 17;
    public const byte Class = 18;
    public const byte Var = 19;
    public const byte Array = 20;
    public const byte GenericInst = 21;
    public const byte TypedByRef = 20;
    public const byte I = 24;
    public const byte U = 25;
    public const byte FnPtr = 27;
    public const byte Object = 28;
    public const byte SZArray = 29;
    public const byte MVar = 30;
    public const byte CModReqd = 31;
    public const byte CModOpt = 32;
    public const byte Internal = 33;
    public const byte Max = 34;
    public const byte Modifier = 64;
    public const byte Sentinel = 65;
    public const byte Pinned = 69;
}