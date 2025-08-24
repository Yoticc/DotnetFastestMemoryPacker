using DotnetFastestMemoryPacker.Internal;
using System.Runtime.CompilerServices;
using PatcherReference;
using System.Reflection;

[module: SkipLocalsInit]

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker;
public unsafe static class FastestMemoryPacker
{
    const uint InitialObjectsCapacity = 1 << 8;
    const uint InitialObjectRootsCapacity = 1 << 9;

    public static byte[] Serialize(object objectToSerialize)
    {
        if (objectToSerialize is null)
            return [];

        var methodTable = **(MethodTable***)&objectToSerialize;

        Patcher.Pinnable(out object @object);
        Patcher.Pinnable(out byte[] bytesArray);

        if (!methodTable->ContainsGCPointers)
        {
            @object = objectToSerialize;

            uint bytesCount;
            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var arrayLength = *(uint*)(*(nint*)&@object + SizeOf.MethodTable);
                    var componentsSize = arrayLength * methodTable->ComponentSize;
                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;
                    bytesCount = headerSize + componentsSize;
                }
                else
                {
                    var stringLength = *(uint*)(*(nint*)&@object + SizeOf.MethodTable);
                    bytesCount = SizeOf.StringLength + (stringLength << 1);
                }
            }
            else
            {
                var eeClass = methodTable->Class;
                bytesCount = methodTable->BaseSize - eeClass->BaseSizePadding;
            }

            bytesCount += SizeOf.PackedHeader;
            bytesArray = GC.AllocateUninitializedArray<byte>((int)bytesCount);

            var destination = (byte*)(*(nint**)&bytesArray + 2) + SizeOf.PackedHeader;
            var source = (void*)(*(nint*)&@object + SizeOf.MethodTable);
            Unsafe.CopyBlock(destination, source, bytesCount);

            return bytesArray;
        }

        byte* bytes;
        uint objectsCount = 1U;
        uint objectsCapacity = InitialObjectsCapacity;
        object* objects;
        Patcher.Pinnable(out object fieldObject);
        Patcher.Pinnable(out object[] objectsArray);

        var objectSizes = stackalloc uint[(int)objectsCapacity];
        Patcher.Pinnable(out uint[] objectSizesArray);
        Patcher.Pinnable(out uint[] newObjectSizesArray);

        var objectRootsCount = 0U;
        var objectRootsCapacity = InitialObjectRootsCapacity;
        var objectRoots = stackalloc ulong[(int)objectRootsCapacity];
        Patcher.Pinnable(out ulong[] objectRootsArray);
        Patcher.Pinnable(out ulong[] newObjectRootsArray);

        var stackallocatedObjects = stackalloc byte[(int)(objectsCapacity * sizeof(object) + SizeOf.ObjectHeader)];
        stackallocatedObjects += SizeOf.GCHeader;
        *(MethodTable**)stackallocatedObjects = (MethodTable*)typeof(object[]).TypeHandle.Value;
        *(nuint*)(stackallocatedObjects + SizeOf.MethodTable) = objectsCapacity;
        objects = (object*)(stackallocatedObjects + SizeOf.MethodTable + sizeof(nuint));
        objectsArray = *(object[]*)&stackallocatedObjects;

        var totalSize = 0UL;

        objects[0] = objectToSerialize;
        for (uint objectIndex = 0; objectIndex < objectsCount; objectIndex++)
        {
            var objectSize = 0U;
            @object = objects[objectIndex];

            methodTable = **(MethodTable***)&@object;
            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var objectBody = *(nint*)&@object + SizeOf.MethodTable;
                    var arrayLength = *(uint*)objectBody;
                    var componentSize = methodTable->ComponentSize;
                    var componentsSize = arrayLength * componentSize;
                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;

                    objectSize = headerSize + componentsSize;

                    if (methodTable->ContainsGCPointers)
                    {
                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            fieldObject = *(object*)(objectBody + offset);

                            /* ========= function #1 =========*/
                            if (fieldObject is not null)
                            {
                                var index = new Span<nint>(objects, (int)objectsCount).IndexOf(*(nint*)&fieldObject);
                                if (index == -1)
                                {
                                    index = (int)objectsCount;
                                    objects[objectsCount++] = fieldObject;
                                    if (objectsCount == objectsCapacity)
                                    {
                                        var newObjectsArray = GC.AllocateUninitializedArray<object>((int)(objectsCapacity <<= 1));
                                        Array.Copy(objectsArray, newObjectsArray, objectsCount);
                                        objectsArray = newObjectsArray;
                                        objects = (object*)(*(nint**)&objectsArray + 2);

                                        newObjectSizesArray = GC.AllocateUninitializedArray<uint>((int)objectsCapacity);
                                        var source = objectSizes;
                                        var destination = objectSizes = (uint*)(*(nint**)&newObjectSizesArray + 2);
                                        Unsafe.CopyBlock(destination, source, objectsCapacity);
                                        objectSizesArray = newObjectSizesArray;
                                    }
                                }

                                // root: { i4 index; i4 offset }
                                objectRoots[objectRootsCount++] = totalSize + offset << 32 | (uint)index;
                                if (objectRootsCount == objectRootsCapacity)
                                {
                                    newObjectRootsArray = GC.AllocateUninitializedArray<ulong>((int)(objectRootsCapacity <<= 1));
                                    var source = objectRoots;
                                    var destination = objectRoots = (ulong*)(*(nint**)&newObjectRootsArray + 2);
                                    Unsafe.CopyBlock(destination, source, objectRootsCount);
                                    objectRootsArray = newObjectRootsArray;
                                }
                            }
                            /* ===============================*/
                        }
                    }
                }
                else
                {
                    var stringLength = *(uint*)(*(nint*)&@object + SizeOf.MethodTable);
                    objectSize = SizeOf.StringLength + (stringLength << 1);
                }
            }
            else
            {
                var eeClass = methodTable->Class;
                objectSize = methodTable->BaseSize - eeClass->BaseSizePadding;

                if (methodTable->ContainsGCPointers)
                {
                    while (true)
                    {
                        var parentMethodTable = methodTable->ParentMethodTable;
                        if (parentMethodTable is null) // it means that current methodTable is object
                            break;

                        EEClass* parentClass;
                        int fieldCount;
                        if (parentMethodTable is not null)
                        {
                            parentClass = parentMethodTable->Class;
                            fieldCount = eeClass->NumInstanceFields - parentClass->NumInstanceFields;
                        }
                        else
                        {
                            parentClass = null;
                            fieldCount = eeClass->NumInstanceFields;
                        }

                        var fieldIndex = 0U;
                        for (var fieldDesc = eeClass->FieldDesc; fieldIndex < fieldCount; fieldDesc++)
                        {
                            if (fieldDesc->IsStatic)
                                continue;

                            fieldIndex++;

                            if (fieldDesc->Type != CorElementType.Class)
                                continue;

                            var offset = fieldDesc->Offset;
                            fieldObject = *(object*)(*(nint*)&@object + SizeOf.MethodTable + offset);

                            /* ========= function #1 =========*/
                            if (fieldObject is not null)
                            {
                                var index = new Span<nint>(objects, (int)objectsCount).IndexOf(*(nint*)&fieldObject);
                                if (index == -1)
                                {
                                    index = (int)objectsCount;
                                    objects[objectsCount++] = fieldObject;
                                    if (objectsCount == objectsCapacity)
                                    {
                                        var newObjectsArray = GC.AllocateUninitializedArray<object>((int)(objectsCapacity <<= 1));
                                        Array.Copy(objectsArray, newObjectsArray, objectsCount);
                                        objectsArray = newObjectsArray;
                                        objects = (object*)(*(nint**)&objectsArray + 2);

                                        newObjectSizesArray = GC.AllocateUninitializedArray<uint>((int)objectsCapacity);
                                        var source = objectSizes;
                                        var destination = objectSizes = (uint*)(*(nint**)&newObjectSizesArray + 2);
                                        Unsafe.CopyBlock(destination, source, objectsCapacity);
                                        objectSizesArray = newObjectSizesArray;
                                    }
                                }

                                // root: { i4 index; i4 offset }
                                objectRoots[objectRootsCount++] = totalSize + offset << 32 | (uint)index;
                                if (objectRootsCount == objectRootsCapacity)
                                {
                                    newObjectRootsArray = GC.AllocateUninitializedArray<ulong>((int)(objectRootsCapacity <<= 1));
                                    var source = objectRoots;
                                    var destination = objectRoots = (ulong*)(*(nint**)&newObjectRootsArray + 2);
                                    Unsafe.CopyBlock(destination, source, objectRootsCount);
                                    objectRootsArray = newObjectRootsArray;
                                }
                            }
                            /* ===============================*/
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }
            }

            objectSizes[objectIndex] = objectSize;
            totalSize += objectSize;
        }

        bytesArray = GC.AllocateUninitializedArray<byte>((int)(SizeOf.PackedHeader + totalSize));
        bytes = (byte*)(*(nint**)&bytesArray + 2);

        *(ulong*)bytes = objectsCount | (ulong)objectRootsCount << 32;
        bytes += SizeOf.PackedHeader;

        for (uint objectIndex = 0, bytesOffset = 0; objectIndex < objectsCount; objectIndex++)
        {
            @object = objects[objectIndex];
            var size = objectSizes[objectIndex];

            Unsafe.CopyBlockUnaligned(bytes + bytesOffset, *(byte**)&@object + SizeOf.MethodTable, size);
            bytesOffset += size;
        }

        for (var rootIndex = 0; rootIndex < objectRootsCount; rootIndex++)
        {
            var root = objectRoots[rootIndex];
            // bytes + root.offset = root.index + 1
            *(ulong*)(bytes + (root >> 32)) = (root & ~0U) + 1;
        }

        return bytesArray;
    }

    public static T Deserialize<T>(byte[] bytesToDeserialize)
    {
        if (bytesToDeserialize.Length == 0)
            return default!;

        var type = typeof(T);
        var methodTable = (MethodTable*)type.TypeHandle.Value;

        Patcher.Pinnable(out object @object);
        Patcher.Pinnable(out byte[] inputArray);
        inputArray = bytesToDeserialize;

        var input = (byte*)(*(nint**)&inputArray + 2);

        if (!methodTable->ContainsGCPointers)
        {
            input += SizeOf.PackedHeader;

            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var arrayLength = *(uint*)input;
                    var componentSize = methodTable->ComponentSize;
                    var componentsSize = arrayLength * componentSize;
                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;
                    var objectSize = headerSize + componentsSize;

                    @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));
                    **(MethodTable***)&@object = methodTable;

                    // only for x64. use CopyBlockUnaligned for x32
                    Unsafe.CopyBlock(*(byte**)&@object + SizeOf.MethodTable, input, objectSize);
                }
                else
                {
                    var length = *(uint*)input;
                    if (length <= 2)
                    {
                        if (length == 0)
                            @object = string.Empty;
                        else @object = new string((char*)(input + SizeOf.StringLength), 0, (int)length);
                    }
                    else
                    {
                        var objectSize = SizeOf.StringLength + (length << 1);
                        @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));

                        **(nint**)&@object = typeof(string).TypeHandle.Value;
                        Unsafe.CopyBlock(*(nint**)&@object + 1, input, objectSize);
                    }
                }
            }
            else
            {
                @object = RuntimeHelpers.GetUninitializedObject(type);

                var eeClass = methodTable->Class;
                var objectSize = methodTable->BaseSize - eeClass->BaseSizePadding;
                Unsafe.CopyBlock(*(byte**)&@object + SizeOf.MethodTable, input, objectSize);
            }
        }
        else
        {
            @object = Deserialize(input, inputArray.Length, methodTable);
        }

        return (T)@object;
    }

    static object Deserialize(byte* input, int inputLength, MethodTable* objectMethodTable)
    {
        var objectRootsCount = 0;
        Patcher.Pinnable(out ulong[] objectRootsArray);

        object* objects;
        Patcher.Pinnable(out object[] objectsArray);

        Patcher.Pinnable(out MethodTable*[] objectMethodTablesArray);

        Patcher.Pinnable(out object @object);
        Patcher.Pinnable(out object fieldObject);

        var packedHeader = *(ulong*)input;
        input += SizeOf.PackedHeader;
        var objectsCount = (uint)(packedHeader & ~0u);
        var objectRootsCapacity = (uint)(packedHeader << 32);
        
        var stackallocatedObjects = stackalloc byte[(int)(InitialObjectsCapacity * sizeof(object) + SizeOf.ObjectHeader)];
        if (objectsCount > InitialObjectsCapacity)
        {
            objectsArray = GC.AllocateUninitializedArray<object>((int)objectsCount);
            objects = (object*)(*(nint**)&objectsArray + 2);
        }
        else
        {
            stackallocatedObjects += SizeOf.GCHeader;
            *(MethodTable**)stackallocatedObjects = (MethodTable*)typeof(object[]).TypeHandle.Value;
            *(nuint*)(stackallocatedObjects + SizeOf.MethodTable) = objectsCount;
            objects = (object*)(stackallocatedObjects + SizeOf.MethodTable + sizeof(nuint));
            objectsArray = *(object[]*)&stackallocatedObjects;
        }

        var objectRoots = stackalloc ulong[(int)InitialObjectRootsCapacity];
        if (objectRootsCapacity > InitialObjectRootsCapacity)
        {
            objectRootsArray = GC.AllocateUninitializedArray<ulong>((int)objectRootsCapacity);
            objectRoots = (ulong*)(*(nint**)&objectRootsArray + 2);
        }

        var objectMethodTables = stackalloc MethodTable*[(int)InitialObjectsCapacity];
        if (objectsCount > InitialObjectsCapacity)
        {
            objectMethodTablesArray = new MethodTable*[objectsCount];
            objectMethodTables = (MethodTable**)(*(nint**)&objectMethodTablesArray + 2);
        }
        else
        {
            Unsafe.InitBlock(objectMethodTables, 0, objectsCount * SizeOf.MethodTable);
        }

        objectMethodTables[0] = objectMethodTable;
        for (var objectIndex = 0; objectIndex < objectsCount; objectIndex++)
        {
            var methodTable = objectMethodTables[objectIndex];
            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var arrayLength = *(uint*)input;
                    var componentSize = methodTable->ComponentSize;
                    var componentsSize = arrayLength * componentSize;

                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;
                    var objectSize = headerSize + componentsSize;

                    objects[objectIndex] = @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));
                    **(MethodTable***)&@object = methodTable;

                    Unsafe.CopyBlockUnaligned(*(byte**)&@object + SizeOf.MethodTable, input, objectSize);
                    if (methodTable->ContainsGCPointers)
                    {
                        var elementMethodTable = methodTable->ElementType;

                        var objectBody = input;
                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            var elementIdentifier = *(int*)(objectBody + offset);
                            if (elementIdentifier > 0)
                            {
                                elementIdentifier--;
                                objectMethodTables[elementIdentifier] = elementMethodTable;

                                // root: { i4 index; i4 offset }
                                objectRoots[objectRootsCount++] = (ulong)(offset + SizeOf.MethodTable) << 32 | (uint)objectIndex;
                            }
                        }
                    }

                    input += objectSize;
                }
                else
                {
                    var length = *(uint*)input;
                    var objectSize = SizeOf.StringLength + (length << 1);
                    if (length <= 2)
                    {
                        if (length == 0)
                            objects[objectIndex] = string.Empty;
                        else objects[objectIndex] = new string((char*)(input + SizeOf.StringLength), 0, (int)length);
                    }
                    else
                    {
                        objects[objectIndex] = @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));
                        **(nint**)&@object = typeof(string).TypeHandle.Value;
                        Unsafe.CopyBlockUnaligned(*(nint**)&@object + 1, input, objectSize);
                    }

                    input += objectSize;
                }
            }
            else
            {
                objects[objectIndex] = @object = RuntimeHelpers.GetUninitializedObject(Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr((nint)methodTable)));

                var eeClass = methodTable->Class;
                var objectSize = methodTable->BaseSize - eeClass->BaseSizePadding;

                Unsafe.CopyBlockUnaligned(*(byte**)&@object + SizeOf.MethodTable, input, objectSize);

                if (methodTable->ContainsGCPointers)
                {
                    var objectBody = input;
                    while (true)
                    {
                        var parentMethodTable = methodTable->ParentMethodTable;
                        if (parentMethodTable is null) // it means that current methodTable is object
                            break;

                        EEClass* parentClass;
                        int fieldCount;
                        if (parentMethodTable is not null)
                        {
                            parentClass = parentMethodTable->Class;
                            fieldCount = eeClass->NumInstanceFields - parentClass->NumInstanceFields;
                        }
                        else
                        {
                            parentClass = null;
                            fieldCount = eeClass->NumInstanceFields;
                        }

                        var fieldIndex = 0;
                        for (var fieldDesc = eeClass->FieldDesc; fieldIndex < fieldCount; fieldDesc++)
                        {
                            if (fieldDesc->IsStatic)
                                continue;

                            fieldIndex++;

                            if (fieldDesc->Type != CorElementType.Class)
                                continue;

                            var offset = fieldDesc->Offset;
                            var objectIdentifier = *(uint*)(objectBody + offset);
                            if (objectIdentifier != 0)
                            {
                                objectIdentifier--;

                                if (objectMethodTables[objectIdentifier] is null)
                                {
                                    var fieldHandle = RuntimeFieldHandle.FromIntPtr((nint)fieldDesc);
                                    var declaringTypeHandle = RuntimeTypeHandle.FromIntPtr((nint)methodTable);
                                    var field = FieldInfo.GetFieldFromHandle(fieldHandle, declaringTypeHandle);

                                    var fieldMethodTable = (MethodTable*)field.FieldType.TypeHandle.Value;
                                    objectMethodTables[objectIdentifier] = fieldMethodTable;
                                }

                                // root: { i4 index; i4 offset }
                                objectRoots[objectRootsCount++] = (ulong)(offset + SizeOf.MethodTable) << 32 | (uint)objectIndex;
                            }
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }

                input += objectSize;
            }
        }

        for (var rootIndex = 0; rootIndex < objectRootsCount; rootIndex++)
        {
            var root = objectRoots[rootIndex];

            @object = objects[root & ~0U];
            var pointer = (nint*)(*(byte**)&@object + (root >> 32));
            *(object*)pointer = objects[*(uint*)pointer - 1];
        }

        return objects[0];
    }

    public static byte[] SerializeUnmanaged<T>(T value) where T : unmanaged
    {
        Patcher.Pinnable(out byte[] bytesArray);
        bytesArray = GC.AllocateUninitializedArray<byte>(sizeof(T));
        var bytes = (byte*)(*(nint**)&bytesArray + 2);

        Unsafe.Write(bytes, value);

        return bytesArray;
    }

    public static T DeserializeUnmanaged<T>(byte[] bytes) where T : unmanaged
    {
        Patcher.Pinnable(out byte[] bytesArray);
        bytesArray = bytes;

        return Unsafe.Read<T>((byte*)(*(nint**)&bytesArray + 2));
    }

    public static void DeserializeUnmanaged<T>(byte[] bytes, T* pvalue) where T : unmanaged
    {
        Patcher.Pinnable(out byte[] bytesArray);
        bytesArray = bytes;

        Unsafe.Copy(ref Unsafe.AsRef<T>(pvalue), (byte*)(*(nint**)&bytesArray + 2));
    }
}