using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker.Internal;
[StructLayout(LayoutKind.Explicit)]
unsafe struct MethodTableAuxiliaryData
{
    [FieldOffset(0x10)] nint exposedRuntimeType; // may be null

    public Type ExposedRuntimeType
    {
        get 
        {
            var address = exposedRuntimeType;
            return *(Type/*RuntimeType*/*)&address;
        }
    }
}