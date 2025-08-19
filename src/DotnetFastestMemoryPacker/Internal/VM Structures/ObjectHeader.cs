namespace DotnetFastestMemoryPacker.Internal;
unsafe struct ObjectHeader
{
    GCHeader gcHeader;
    MethodTable* methodTable;
}