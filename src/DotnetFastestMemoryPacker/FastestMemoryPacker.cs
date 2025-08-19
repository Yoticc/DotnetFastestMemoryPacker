using DotnetFastestMemoryPacker.Internal;
using PatcherReference;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker;

public unsafe static class FastestMemoryPacker
{
    const int InitialObjectsCapacity = 1 << 8;
    const int InitialObjectRootsCapacity = 1 << 10;

    public static byte[] Serialize(object objectToSerialize)
    {
        if (objectToSerialize is null)
            return [];

        var objectsCount = 1;
        var objectsCapacity = InitialObjectsCapacity;
        object* objects;
        Patcher.Pinnable(out object @object);
        Patcher.Pinnable(out object fieldObject);
        Patcher.Pinnable(out object[] objectsArray);

        var objectSizes = stackalloc ushort[objectsCapacity];
        Patcher.Pinnable(out ushort[] objectSizesArray);
        Patcher.Pinnable(out ushort[] newObjectSizesArray);

        var objectRootsCount = 0;
        var objectRootsCapacity = InitialObjectRootsCapacity;
        var objectRoots = stackalloc ulong[objectRootsCapacity];
        Patcher.Pinnable(out ulong[] objectRootsArray);
        Patcher.Pinnable(out ulong[] newObjectRootsArray);

        byte* bytes;
        Patcher.Pinnable(out byte[] bytesArray);

        // stage 0. initial allocating
        objectsArray = GC.AllocateUninitializedArray<object>(objectsCapacity);
        objects = (object*)(*(nint**)&objectsArray + 2);

        // stage 1. scan objects
        var totalSize = 0;
        var objectIndex = 0;
        ushort objectSize = 0;

        objects[0] = objectToSerialize;
        for (; objectIndex < objectsCount; objectIndex++)
        {
            @object = objects[objectIndex];

            var methodTable = **(MethodTable***)&@object;
            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var objectBody = *(nint*)&@object + sizeof(MethodTable*);
                    var arrayLength = *(nint*)objectBody;
                    var componentSize = methodTable->ComponentSize;
                    var componentsSize = arrayLength * componentSize;

                    var headerSize = methodTable->BaseSize - sizeof(ObjectHeader);

                    objectSize = (ushort)(headerSize + componentsSize);
                    
                    if (methodTable->ContainsGCPointers)
                    {
                        for (var offset = headerSize; offset < objectSize; offset += sizeof(object*))
                        {
                            fieldObject = *(object*)(objectBody + offset);

                            /* ========= function #1 =========*/
                            if (fieldObject is not null)
                            {
                                var index = new Span<nint>(objects, objectsCount).IndexOf(*(nint*)&fieldObject);
                                if (index == -1)
                                {
                                    index = objectsCount;
                                    objects[objectsCount++] = fieldObject;
                                    if (objectsCount == objectsCapacity)
                                    {
                                        var newObjectsArray = GC.AllocateUninitializedArray<object>(objectsCapacity <<= 1);
                                        Array.Copy(objectsArray, newObjectsArray, objectsCount);
                                        objectsArray = newObjectsArray;
                                        objects = (object*)(*(nint**)&objectsArray + 2);

                                        newObjectSizesArray = GC.AllocateUninitializedArray<ushort>(objectsCapacity);
                                        var source = objectSizes;
                                        var destination = objectSizes = (ushort*)(*(nint**)&newObjectSizesArray + 2);
                                        Unsafe.CopyBlock(destination, source, (uint)objectsCapacity);
                                        objectSizesArray = newObjectSizesArray;
                                    }
                                }

                                // root: { i4 index; i4 offset }
                                objectRoots[objectRootsCount++] = (ulong)((uint)totalSize + (uint)offset) << 32 | (uint)index;
                                if (objectRootsCount == objectRootsCapacity)
                                {
                                    newObjectRootsArray = GC.AllocateUninitializedArray<ulong>(objectRootsCapacity <<= 1);
                                    var source = objectRoots;
                                    var destination = objectRoots = (ulong*)(*(nint**)&newObjectRootsArray + 2);
                                    Unsafe.CopyBlock(destination, source, (uint)objectRootsCount);
                                    objectRootsArray = newObjectRootsArray;
                                }
                            }
                            /* ===============================*/
                        }
                    }
                }
                else // then it is string
                {
                    var stringLength = *(int*)(*(nint**)&@object + 1);
                    objectSize = (ushort)(sizeof(StringLength) + (stringLength << 1));
                }
            }
            else
            {
                var eeClass = methodTable->Class;
                objectSize = (ushort)(methodTable->BaseSize - eeClass->BaseSizePadding);

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
                            fieldCount = eeClass->NumFields - parentClass->NumFields;
                        }
                        else
                        {
                            parentClass = null;
                            fieldCount = eeClass->NumFields;
                        }

                        var fieldIndex = 0;
                        for (var fieldDesc = eeClass->FieldDesc; fieldIndex < fieldCount; fieldDesc++)
                        {
                            if (fieldDesc->IsStatic)
                                continue;

                            fieldIndex++;

                            if (fieldDesc->Type is not CorElementType.Class)
                                continue;

                            var offset = fieldDesc->Offset;
                            fieldObject = *(object*)(*(nint*)&@object + sizeof(MethodTable*) + offset);

                            /* ========= function #1 =========*/
                            if (fieldObject is not null)
                            {
                                var index = new Span<nint>(objects, objectsCount).IndexOf(*(nint*)&fieldObject);
                                if (index == -1)
                                {
                                    index = objectsCount;
                                    objects[objectsCount++] = fieldObject;
                                    if (objectsCount == objectsCapacity)
                                    {
                                        var newObjectsArray = GC.AllocateUninitializedArray<object>(objectsCapacity <<= 1);
                                        Array.Copy(objectsArray, newObjectsArray, objectsCount);
                                        objectsArray = newObjectsArray;
                                        objects = (object*)(*(nint**)&objectsArray + 2);

                                        newObjectSizesArray = GC.AllocateUninitializedArray<ushort>(objectsCapacity);
                                        var source = objectSizes;
                                        var destination = objectSizes = (ushort*)(*(nint**)&newObjectSizesArray + 2);
                                        Unsafe.CopyBlock(destination, source, (uint)objectsCapacity);
                                        objectSizesArray = newObjectSizesArray;
                                    }
                                }

                                // root: { i4 index; i4 offset }
                                objectRoots[objectRootsCount++] = (ulong)((uint)totalSize + (uint)offset) << 32 | (uint)index;
                                if (objectRootsCount == objectRootsCapacity)
                                {
                                    newObjectRootsArray = GC.AllocateUninitializedArray<ulong>(objectRootsCapacity <<= 1);
                                    var source = objectRoots;
                                    var destination = objectRoots = (ulong*)(*(nint**)&newObjectRootsArray + 2);
                                    Unsafe.CopyBlock(destination, source, (uint)objectRootsCount);
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

        // stage 2. copy all object in one blob
        bytesArray = GC.AllocateUninitializedArray<byte>(sizeof(int) + sizeof(int) + totalSize);
        bytes = (byte*)(*(nint**)&bytesArray + 2);

        *(int*)bytes = objectsCount;
        bytes += sizeof(int);

        *(int*)bytes = objectRootsCount;
        bytes += sizeof(int);

        var bytesOffset = 0;
        for (objectIndex = 0; objectIndex < objectsCount; objectIndex++)
        {
            @object = objectsArray[objectIndex];
            var size = objectSizes[objectIndex];

            Unsafe.CopyBlockUnaligned(bytes + bytesOffset, *(byte**)&@object + sizeof(MethodTable*), size);
            bytesOffset += size;
        }

        // stage 3. change all references to object indices
        for (var rootIndex = 0; rootIndex < objectRootsCount; rootIndex++)
        {
            var root = objectRoots[rootIndex];
            // bytes + root.offset = root.index + 1
            *(ulong*)(bytes + (root >> 32)) = (root & ~0U) + 1;
        }

        return bytesArray;
    }

    // use for implementation on pure MethodTables. patcher should change this code to loadtoken T; ret; also maybe add ldind.i; to unpack MethodTable** to MethodTable*
    //static MethodTable* GetMethodTable<T>() => (MethodTable*)typeof(T).TypeHandle.Value;
    
    public static T Deserialize<T>(byte[] bytesToDeserialize) 
        where T : class
    {
        var totalSize = bytesToDeserialize.Length;

        if (totalSize == 0)
            return default!;

        totalSize -= sizeof(int) + sizeof(int);

        var objectRootsCount = 0;
        Patcher.Pinnable(out ulong[] objectRootsArray);

        object* objects;
        Patcher.Pinnable(out object[] objectsArray);

        byte* input;
        Patcher.Pinnable(out byte[] inputArray);

        Patcher.Pinnable(out MethodTable*[] objectMethodTablesArray);

        Patcher.Pinnable(out object @object);
        Patcher.Pinnable(out object fieldObject);

        // stage 0. allocating
        inputArray = bytesToDeserialize;
        input = (byte*)(*(nint**)&inputArray + 2);

        var objectsCount = *(int*)input;
        input += sizeof(int);

        var objectRootsCapacity = *(int*)input;
        input += sizeof(int);

        objectsArray = GC.AllocateUninitializedArray<object>(objectsCount);
        objects = (object*)(*(nint**)&objectsArray + 2);

        var objectRoots = stackalloc ulong[InitialObjectRootsCapacity];
        if (objectRootsCapacity > InitialObjectRootsCapacity)
        {
            objectRootsArray = GC.AllocateUninitializedArray<ulong>(objectRootsCapacity);
            objectRoots = (ulong*)(*(nint**)&objectRootsArray + 2);
        }

        var objectMethodTables = stackalloc MethodTable*[InitialObjectsCapacity];
        if (objectsCount > InitialObjectsCapacity)
        {
            objectMethodTablesArray = new MethodTable*[objectsCount];
            objectMethodTables = (MethodTable**)(*(nint**)&objectMethodTablesArray + 2);
        }
        else
        {
            Unsafe.InitBlock(objectMethodTables, 0, (uint)(objectsCount * sizeof(MethodTable*)));
        }

        // stage 1. scan bytes
        var bytesOffset = 0;
        var objectSize = 0;

        objectMethodTables[0] = (MethodTable*)typeof(T).TypeHandle.Value;
        for (var objectIndex = 0; objectIndex < objectsCount; objectIndex++)
        {
            var methodTable = objectMethodTables[objectIndex];
            var cmt = methodTable->CanonicalMethodTable;

            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var arrayLength = *(nint*)(input + bytesOffset);    
                    var componentSize = methodTable->ComponentSize;
                    var componentsSize = arrayLength * componentSize;

                    var headerSize = methodTable->BaseSize - sizeof(ObjectHeader);
                    objectSize = (ushort)(headerSize + componentsSize);

                    objects[objectIndex] = @object = GC.AllocateUninitializedArray<byte>(objectSize - sizeof(ArrayLength));
                    **(MethodTable***)&@object = methodTable;

                    if (methodTable->ContainsGCPointers)
                    {
                        Unsafe.CopyBlockUnaligned(*(byte**)&@object + sizeof(MethodTable*), input + bytesOffset, (uint)headerSize);

                        var elementMethodTable = methodTable->ElementType;

                        var objectBody = input + bytesOffset;
                        for (var offset = headerSize; offset < objectSize; offset += sizeof(nint))
                        {
                            var elementIdentifier = *(int*)(objectBody + offset);
                            if (elementIdentifier > 0)
                            {
                                elementIdentifier--;
                                objectMethodTables[elementIdentifier] = elementMethodTable;

                                // root: { i4 index; i4 offset }
                                objectRoots[objectRootsCount++] = (ulong)(offset + sizeof(MethodTable*)) << 32 | (uint)objectIndex;
                            }
                            else
                            {
                                *(nint*)(objectBody + offset) = default;
                            }
                        }
                    }
                    else
                    {
                        Unsafe.CopyBlockUnaligned(*(byte**)&@object + sizeof(MethodTable*), input + bytesOffset, (uint)objectSize);
                    }
                }
                else
                {
                    var stringLength = *(int*)(input + bytesOffset);
                    objectSize = sizeof(StringLength) + (stringLength << 1);

                    var chars = (char*)(input + bytesOffset + sizeof(StringLength));
                    objects[objectIndex] = new string(chars, 0, stringLength);
                }
            }
            else
            {
                objects[objectIndex] = @object = RuntimeHelpers.GetUninitializedObject(Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr((nint)methodTable))!);

                var eeClass = methodTable->Class;
                objectSize = (ushort)(methodTable->BaseSize - eeClass->BaseSizePadding);

                Unsafe.CopyBlockUnaligned(*(byte**)&@object + sizeof(MethodTable*), input + bytesOffset, (uint)objectSize);

                if (methodTable->ContainsGCPointers)
                {
                    var objectBody = input + bytesOffset;
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
                            fieldCount = eeClass->NumFields - parentClass->NumFields;
                        }
                        else
                        {
                            parentClass = null;
                            fieldCount = eeClass->NumFields;
                        }
                                                
                        var fieldIndex = 0;
                        for (var fieldDesc = eeClass->FieldDesc; fieldIndex < fieldCount; fieldDesc++)
                        {
                            if (fieldDesc->IsStatic)
                                continue;

                            fieldIndex++;

                            if (fieldDesc->Type is not CorElementType.Class)
                                continue;

                            var offset = fieldDesc->Offset;
                            var objectIdentifier = *(int*)(objectBody + offset);
                            if (objectIdentifier != 0)
                            {
                                objectIdentifier--;

                                if (objectMethodTables[objectIdentifier] is null)
                                {
                                    var declaringTypeHandle = RuntimeTypeHandle.FromIntPtr((nint)methodTable);
                                    var fieldHandle = RuntimeFieldHandle.FromIntPtr((nint)fieldDesc);
                                    var field = FieldInfo.GetFieldFromHandle(fieldHandle, declaringTypeHandle);

                                    var fieldMethodTable = (MethodTable*)field.FieldType.TypeHandle.Value;
                                    objectMethodTables[objectIdentifier] = fieldMethodTable;
                                }

                                // root: { i4 index; i4 offset }
                                objectRoots[objectRootsCount++] = (ulong)(offset + sizeof(MethodTable*)) << 32 | (uint)objectIndex;
                            }
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }
            }

            bytesOffset += objectSize;
        }

        for (var rootIndex = 0; rootIndex < objectRootsCount; rootIndex++)
        {
            var root = objectRoots[rootIndex];

            @object = objects[root & ~0U];
            var pointer = (nint*)(*(byte**)&@object + (root >> 32));
            *(object*)pointer = objects[*(int*)pointer - 1];
        }

        return *(T*)objects;
    }
}