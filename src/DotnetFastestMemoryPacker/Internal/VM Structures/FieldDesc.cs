namespace DotnetFastestMemoryPacker.Internal;
unsafe struct FieldDesc
{
    MethodTable* methodTableOfEnclosingClass;
    int dword1;
    int dword2;

    public MethodTable* DefinedType => methodTableOfEnclosingClass;

    public int RID => dword1 & (1 << 24) - 1;
    public bool IsStatic => (dword1 & 1 << 24) > 0;
    public int Protection => (dword2 >> 3) & (1 << 3) - 1;

    public int Offset => dword2 & (1 << 21) - 1;
    public CorElementType Type => (CorElementType)((dword2 >> 27) & (1 << 5) - 1);

    public int Token => RID | 0x4000000;
}
