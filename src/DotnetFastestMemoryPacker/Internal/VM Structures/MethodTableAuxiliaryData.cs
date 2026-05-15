using System.Runtime.InteropServices;

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