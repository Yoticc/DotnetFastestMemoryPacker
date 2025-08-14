using DotnetFastestMemoryPacker.Internal;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker;
public unsafe static class FastestMemoryPacker
{
    public static byte[] Pack(object objectToPack)
    {
        object @object;

        var objects = new List<object>(1 << 8);
        var totalSize = 0;
        var objectIndex = 0;

        objects[0] = objectToPack;
        while (objectIndex < objects.Count)
        {
            @object = objects[objectIndex];
            var methodTable = *(MethodTable**)&@object;


            totalSize += 0;


            objectIndex++;
        }

        var bytes = GC.AllocateUninitializedArray<byte>(totalSize);

        return bytes;
    }
}