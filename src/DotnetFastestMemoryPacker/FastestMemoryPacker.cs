using DotnetFastestMemoryPacker.Internal;
using System.Buffers;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker;
public unsafe static class FastestMemoryPacker
{
    static int IndexOf(List<object> objects, object objectToSearch)
    {
        /*pinned*/ object @object = objectToSearch;

        var objectsArray = *(object[]*)(*(nint**)&objects + 1);
        fixed (object* objectPointer = objectsArray) /* checks removed */
        {
            var span = new Span<nint>(objectPointer, objects.Count);
            var index = span.IndexOf(*(nint*)&@object);
            return index;
        }
    }

    public static byte[] Pack(object objectToPack)
    {
        /*pinned*/ object @object;

        // stage 1. scan objects
        var objects = new List<object>(1 << 8);
        var objectRoots = new List<long>(1 << 9);
        var totalSize = 0;
        var objectIndex = 0;

        objects.Add(objectToPack);
        while (objectIndex < objects.Count)
        {
            @object = objects[objectIndex];

            var methodTable = **(MethodTable***)&@object;
            var eeClass = methodTable->Class;
            var objectSize = methodTable->BaseSize;
            var actualObjectSize = objectSize - eeClass->BaseSizePadding;

            while (true)
            {
                var parentMethodTable = methodTable->ParentMethodTable;
                if (parentMethodTable is null) // it means that current methodTable is object
                    break;

                var fieldCount = eeClass->NumFields;
                if (parentMethodTable is not null)
                {
                    var parentMethodTableClass = parentMethodTable->Class;
                    fieldCount -= parentMethodTableClass->NumFields;
                }

                var fieldCounter = 0;
                for (var fieldDesc = eeClass->FieldDesc; fieldCounter < fieldCount; fieldDesc++)
                {
                    if (fieldDesc->IsStatic)
                        continue;
                     
                    if (fieldDesc->Type is CorElementType.Class)
                    {
                        var offset = fieldDesc->Offset;
                        var fieldObject = *(object*)(*(nint*)&@object + sizeof(nint) + offset);
                        if (fieldObject is not null)
                        {
                            var index = IndexOf(objects, fieldObject);
                            if (index == -1)
                            {
                                index = objects.Count;
                                objects.Add(fieldObject);
                            }

                            // root: { u4 index; u4 offset }
                            objectRoots.Add((long)index | ((long)(totalSize + offset) << 32));
                        }
                    }

                    fieldCounter++;
                }

                methodTable = parentMethodTable;
                eeClass = methodTable->Class;
            }

            totalSize += (int)actualObjectSize;

            objectIndex++;
        }

        // stage 2. copy all object in one blob
        var bytes = GC.AllocateUninitializedArray<byte>(totalSize);
        fixed (byte* pointer = bytes)
        {
            var offset = 0;

            var objectsCount = objects.Count;
            for (objectIndex = 0; objectIndex < objectsCount; objectIndex++)
            {
                @object = objects[objectIndex];
                var objectPointer = *(void**)&@object;
                var methodTable = *(MethodTable**)objectPointer;
                var eeClass = methodTable->Class;
                var objectSize = methodTable->BaseSize;
                var actualObjectSize = objectSize - eeClass->BaseSizePadding;

                NativeMemory.Copy((byte*)objectPointer + sizeof(nint), pointer + offset, (nuint)actualObjectSize);
                offset += (int)actualObjectSize;
            }

            // stage 3. change all references to object indices
            var objectRootsCount = objectRoots.Count;
            var objectRootsArray = *(long[]*)(*(nint**)&objectRoots + 1);
            @object = objectRootsArray;
            var objectRootsPointer = (long*)(*(nint**)&@object + 2);

            for (var rootIndex = 0; rootIndex < objectRootsCount; rootIndex++)
            {
                var root = objectRootsPointer[rootIndex];
                // pointer + offset = index + 1
                *(long*)(pointer + (root >> 32)) = (root & ~0U) + 1;
            }
        }

        return bytes;
    }

    // use for implementation on pure methodTables
    //static MethodTable* GetMethodTable<T>() => (MethodTable*)typeof(T).TypeHandle.Value;

    public static T UnPack<T>(byte[] bytes) 
        where T : class
    {
        /*pinned*/ object @object;

        var rootType = typeof(T);
        var rootObject = RuntimeHelpers.GetUninitializedObject(rootType);
        var methodTable = **(MethodTable***)&rootObject;



        return (T)rootObject;
    }
}