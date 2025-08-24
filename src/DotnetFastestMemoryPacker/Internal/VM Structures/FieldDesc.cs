using System.Runtime.InteropServices;

namespace DotnetFastestMemoryPacker.Internal;

[StructLayout(LayoutKind.Explicit)]
unsafe struct FieldDesc
{
    [FieldOffset(0x08)] uint dword1;
    [FieldOffset(0x0C)] uint dword2;

    public bool IsStatic => (dword1 & 1 << 24) > 0;

    public uint Offset => dword2 & (1 << 21) - 1;
    public byte Type => (byte)((dword2 >> 27) & (1 << 5) - 1);
}
