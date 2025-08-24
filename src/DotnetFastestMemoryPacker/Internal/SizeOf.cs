using PatcherReference;

namespace DotnetFastestMemoryPacker.Internal;

[ShouldBeTrimmed]
class SizeOf
{
    public const uint PackedObjectsCount = 4/*sizeof(int)*/;
    public const uint PackedObjectRootsCount = 4/*sizeof(int)*/;
    public const uint PackedHeader = PackedObjectsCount + PackedObjectRootsCount;
    public const uint MethodTable = 8/*sizeof(MethodTable*)*/;
    public const uint GCHeader = 8/*sizeof(int) + sizeof(int)*/;
    public const uint ObjectHeader = GCHeader + MethodTable;
    public const uint ArrayLength = 8/*sizeof(nint)*/;
    public const uint StringLength = 4/*sizeof(int)*/;
    public const uint Reference = 8/*sizeof(void*)*/;
}