global using static DotnetFastestMemoryPacker.Internal.ExtrinsicsImpl;
global using static PatcherReference.Extrinsics;
using DotnetFastestMemoryPacker.Internal;
using PatcherReference;
using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

// it was relevant before, but i was denied the use of stackalloc due to problems with jit
[module: SkipLocalsInit]

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker;
public unsafe static class FastestMemoryPacker
{
    public static byte[] Serialize<T>(in T objectToSerialize)
    {
        Pinnable(out byte[] bytesArray);
        Pinnable(out object @object);

        // value type is the only instantiation that can be handled by jit as a constraintable branch
        if (typeof(T).IsValueType)
        {
            bytesArray = GC.AllocateUninitializedArray<byte>(sizeof(T));
            Unsafe.Write(GetArrayPointer(bytesArray), objectToSerialize);
            return bytesArray;
        }
        
        @object = objectToSerialize;
        var methodTable = GetMethodTable(@object);
        if (methodTable->ContainsGCPointers)
        {
            if (@object is null)
                return [];

            return SerializeWithGCPointers(methodTable, objectToSerialize);
        }

        uint length;
        if (methodTable->HasComponentSize)
        {
            length = LoadEffectiveValue<uint>(@object, SizeOf.MethodTable);
            if (methodTable->IsArray)
            {
                var componentsSize = length * methodTable->ComponentSize;
                var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;
                length = componentsSize + headerSize;
            }
            else
            {
                length = (length << 1) + SizeOf.StringLength;
            }
        }
        else
        {
            length = methodTable->BaseSize - methodTable->Class->BaseSizePadding;
        }

        bytesArray = GC.AllocateUninitializedArray<byte>((int)(length + SizeOf.PackedHeader));
        
        var destination = GetArrayPointer(bytesArray) + SizeOf.PackedHeader;
        var source = LoadEffectiveAddress(@object, SizeOf.MethodTable);
        Unsafe.CopyBlock(destination, source, length);

        return bytesArray;
    }


    public static byte[] SerializeWithObjectIdentify<T>(in T objectToSerialize)
    {
        var methodTable = GetMethodTable<T>();
        if (methodTable->ContainsGCPointers)
        {
            if (objectToSerialize is null)
                return [];

            return SerializeWithGCPointersWithObjectIdentify(methodTable, objectToSerialize);
        }

        return Serialize(objectToSerialize);
    }

    static byte[] SerializeWithGCPointers(MethodTable* methodTable, object objectToSerialize)
    {
        Pinnable(out object @object);
        Pinnable(out object innerObject);
        Pinnable(out byte[] bytesArray);

        nint* objects;
        uint* sizes;
        uint objectsCapacity;
        uint sizesCapacity;

        uint objectsCount = 1;
        ulong totalSize = 0;

        var buffers = SerializationBuffers.ThreadLocal;
        buffers.EnterObjectsContext(&objects, &objectsCapacity);
        buffers.EnterSizesContext(&sizes, &sizesCapacity);

        @object = objectToSerialize;
        objects[0] = *(nint*)&@object;

        var objectIndex = 0U;
        while (true)
        {
            methodTable = **(MethodTable***)&@object;
            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var bodyPointer = *(byte**)&@object + SizeOf.MethodTable;
                    var arrayLength = *(uint*)bodyPointer;
                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;

                    if (methodTable->ContainsGCPointers)
                    {
                        buffers.EnsureObjectsCapacity(objectsCount + arrayLength, ref objects, ref objectsCapacity);
                        buffers.EnsureSizesCapacity(objectsCount + arrayLength, ref sizes, ref sizesCapacity);

                        var componentsSize = arrayLength << 3;
                        var objectSize = sizes[objectIndex] = headerSize + componentsSize;
                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            innerObject = Unsafe.AsRef<object>(bodyPointer + offset);
                            if (innerObject is null)
                                continue;

                            buffers.AddObject(innerObject, objects, ref objectsCount);
                        }

                        totalSize += objectSize;
                    }
                    else
                    {
                        var componentsSize = arrayLength * methodTable->ComponentSize;
                        var objectSize = sizes[objectIndex] = headerSize + componentsSize;
                        totalSize += objectSize;
                    }
                }
                else
                {
                    var stringLength = *(uint*)(*(nint*)&@object + SizeOf.MethodTable);
                    var objectSize = sizes[objectIndex] = SizeOf.StringLength + (stringLength << 1);
                    totalSize += objectSize;
                }
            }
            else
            {
                var eeClass = methodTable->Class;
                var objectSize = sizes[objectIndex] = methodTable->BaseSize - eeClass->BaseSizePadding;
                totalSize += objectSize;

                if (methodTable->ContainsGCPointers)
                {
                    var bodyPointer = *(byte**)&@object + SizeOf.MethodTable;
                    while (true)
                    {
                        var parentMethodTable = methodTable->ParentMethodTable;
                        if (parentMethodTable is null) // it means that current methodTable is object
                            break;

                        var parentClass = parentMethodTable->Class;
                        var fieldCount = eeClass->NumInstanceFields - parentClass->NumInstanceFields;

                        var fieldIndex = 0U;
                        for (var fieldDesc = eeClass->FieldDesc; fieldIndex < fieldCount; fieldDesc++)
                        {
                            if (fieldDesc->IsStatic)
                                continue;

                            fieldIndex++;

                            if (fieldDesc->Type != CorElementType.Class)
                                continue;

                            innerObject = Unsafe.AsRef<object>(bodyPointer + fieldDesc->Offset);
                            if (innerObject is null)
                                continue;

                            buffers.AddObjectAndEnsureCapacity(
                                innerObject,
                                ref objects, ref objectsCount, ref objectsCapacity,
                                ref sizes, ref sizesCapacity);
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }
            }

            if (++objectIndex == objectsCount)
                break;

            @object = Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectIndex);
        }

        bytesArray = GC.AllocateUninitializedArray<byte>((int)(SizeOf.PackedHeader + totalSize));
        var output = (byte*)(*(nint**)&bytesArray + 2);

        *(uint*)output = objectsCount | (1U << 31)/*object identify state: true*/;
        output += SizeOf.PackedHeader;

        var bytesOffset = 0u;
        for (objectIndex = 0u; objectIndex < objectsCount; objectIndex++)
        {
            @object = Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectIndex);
            var size = sizes[objectIndex];

            Unsafe.CopyBlockUnaligned(output + bytesOffset, *(byte**)&@object + SizeOf.MethodTable, size);
            bytesOffset += size;
        }

        buffers.ExitObjectsContext(objects, objectsCount);

        return bytesArray;
    }

    static byte[] SerializeWithGCPointersWithObjectIdentify(MethodTable* methodTable, object objectToSerialize)
    {
        Pinnable(out object @object);
        Pinnable(out object innerObject);
        Pinnable(out byte[] bytesArray);

        nint* objects;
        uint* sizes;
        ulong* roots;
        uint objectsCapacity;
        uint sizesCapacity;
        uint rootsCapacity;

        uint objectsCount = 1;
        uint rootsCount = 0;
        ulong totalSize = 0;

        var buffers = SerializationBuffers.ThreadLocal;
        buffers.EnterObjectsContext(&objects, &objectsCapacity);
        buffers.EnterSizesContext(&sizes, &sizesCapacity);
        buffers.EnterRootsContext(&roots, &rootsCapacity);

        @object = objectToSerialize;
        objects[0] = *(nint*)&@object;

        var objectIndex = 0U;
        while (true)
        {
            methodTable = **(MethodTable***)&@object;
            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var bodyPointer = *(byte**)&@object + SizeOf.MethodTable;
                    var arrayLength = *(uint*)bodyPointer;
                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;

                    if (methodTable->ContainsGCPointers)
                    {
                        buffers.EnsureObjectsCapacity(objectsCount + arrayLength, ref objects, ref objectsCapacity);
                        buffers.EnsureSizesCapacity(objectsCount + arrayLength, ref sizes, ref sizesCapacity);
                        buffers.EnsureRootsCapacity(rootsCount + arrayLength, ref roots, ref rootsCapacity);

                        var componentsSize = arrayLength << 3;
                        var objectSize = sizes[objectIndex] = headerSize + componentsSize;
                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            innerObject = Unsafe.AsRef<object>(bodyPointer + offset);
                            if (innerObject is null)
                                continue;

                            var index = new Span<nint>(objects, (int)objectsCount).LastIndexOf(*(nint*)&innerObject);
                            if (index == -1)
                            {
                                index = (int)objectsCount;
                                buffers.AddObject(innerObject, objects, ref objectsCount);
                            }

                            // root: { i4 index; i4 offset }
                            var root = totalSize + offset << 32 | (uint)index;
                            buffers.AddRoot(root, roots, ref rootsCount);
                        }

                        totalSize += objectSize;
                    }
                    else
                    {
                        var componentsSize = arrayLength * methodTable->ComponentSize;
                        var objectSize = sizes[objectIndex] = headerSize + componentsSize;
                        totalSize += objectSize;
                    }
                }
                else
                {
                    var stringLength = *(uint*)(*(nint*)&@object + SizeOf.MethodTable);
                    var objectSize = sizes[objectIndex] = SizeOf.StringLength + (stringLength << 1);
                    totalSize += objectSize;
                }
            }
            else
            {
                var eeClass = methodTable->Class;
                var objectSize = sizes[objectIndex] = methodTable->BaseSize - eeClass->BaseSizePadding;
                totalSize += objectSize;

                if (methodTable->ContainsGCPointers)
                {
                    var bodyPointer = *(byte**)&@object + SizeOf.MethodTable;
                    while (true)
                    {
                        var parentMethodTable = methodTable->ParentMethodTable;
                        if (parentMethodTable is null) // it means that current methodTable is object
                            break;

                        var parentClass = parentMethodTable->Class;
                        var fieldCount = eeClass->NumInstanceFields - parentClass->NumInstanceFields;

                        var fieldIndex = 0U;
                        for (var fieldDesc = eeClass->FieldDesc; fieldIndex < fieldCount; fieldDesc++)
                        {
                            if (fieldDesc->IsStatic)
                                continue;

                            fieldIndex++;

                            if (fieldDesc->Type != CorElementType.Class)
                                continue;

                            var offset = fieldDesc->Offset;
                            innerObject = Unsafe.AsRef<object>(bodyPointer + offset);
                            if (innerObject is null)
                                continue;

                            var index = new Span<nint>(objects, (int)objectsCount).LastIndexOf(*(nint*)&innerObject);
                            if (index == -1)
                            {
                                index = (int)objectsCount;
                                buffers.AddObjectAndEnsureCapacity(
                                    innerObject, 
                                    ref objects, ref objectsCount, ref objectsCapacity, 
                                    ref sizes, ref sizesCapacity);
                            }

                            // root: { i4 index; i4 offset }
                            var root = totalSize + offset << 32 | (uint)index;
                            buffers.AddRootAndEnsureCapacity(root, ref roots, ref rootsCount, ref rootsCapacity);
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }
            }

            if (++objectIndex == objectsCount)
                break;

            @object = Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectIndex);
        }

        bytesArray = GC.AllocateUninitializedArray<byte>((int)(SizeOf.PackedHeader + totalSize));
        var bytes = (byte*)(*(nint**)&bytesArray + 2);

        *(uint*)bytes = rootsCount + 1;
        bytes += SizeOf.PackedHeader;

        var bytesOffset = 0u;
        for (objectIndex = 0u; objectIndex < objectsCount; objectIndex++)
        {
            @object = Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectIndex);
            var size = sizes[objectIndex];            

            Unsafe.CopyBlockUnaligned(bytes + bytesOffset, *(byte**)&@object + SizeOf.MethodTable, size);
            bytesOffset += size;
        }

        for (var rootIndex = 0; rootIndex < rootsCount; rootIndex++)
        {
            var root = roots[rootIndex];
            // bytes + root.offset = root.index + 1
            *(ulong*)(bytes + (root >> 32)) = (root & ~0U) + 1;
        }

        buffers.ExitObjectsContext(objects, objectsCount);

        return bytesArray;
    }

    public static T Deserialize<T>(byte[] bytes)
    {
        Pinnable(out object @object);
        Pinnable(out byte[] bytesArray);

        bytesArray = bytes;
        var input = (byte*)(*(nint**)&bytesArray + 2);

        if (typeof(T).IsValueType)
            return Unsafe.Read<T>(input);

        var methodTable = GetMethodTable<T>();
        if (methodTable->ContainsGCPointers)
        {
            if (bytes.Length == 0)
                return default;

            var inputLength = *(int*)(*(nint**)&bytesArray + 1);
            var header = *(uint*)input;
            input += SizeOf.PackedHeader;
            inputLength -= 4;

            var hasObjectIdentify = (header & (1 << 31)) > 0U;
            if (hasObjectIdentify)
                return (T)DeserializeWithGCPointersWithObjectIdentify(input, inputLength, methodTable, header & unchecked((1 << 31) - 1));

            return (T)DeserializeWithGCPointersWithObjectIdentify(input, inputLength, methodTable, header);
        }

        input += SizeOf.PackedHeader;

        if (methodTable->HasComponentSize)
        {
            if (methodTable->IsArray)
            {
                var arrayLength = *(uint*)input;
                var arraySize = arrayLength * methodTable->ComponentSize;
                var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;
                var objectSize = headerSize + arraySize;

                @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));
                **(MethodTable***)&@object = methodTable;

                var destination = LoadEffectiveAddress(@object, SizeOf.MethodTable);
                var source = input;
                Unsafe.CopyBlock(destination, source, objectSize);
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

                    **(MethodTable***)&@object = GetMethodTable<string>();
                    var destination = *(nint**)&@object + 1;
                    var source = input;
                    Unsafe.CopyBlock(destination, source, objectSize);
                }
            }
        }
        else
        {
            @object = RuntimeHelpers.GetUninitializedObject(typeof(T));

            var objectSize = methodTable->BaseSize - methodTable->Class->BaseSizePadding;
            var destination = *(byte**)&@object + SizeOf.MethodTable;
            var source = input;
            Unsafe.CopyBlock(destination, source, objectSize);
        }

        return (T)@object;
    }

    static object DeserializeWithGCPointersWithObjectIdentify(byte* input, int inputLength, MethodTable* objectMethodTable, uint rootsCount)
    {
        Pinnable(out object @object);
        Pinnable(out object innerObject);

        nint* objects;
        ulong* roots;
        MethodTable** methodTables;

        var buffers = SerializationBuffers.ThreadLocal;
        buffers.EnterObjectsContext(&objects, rootsCount);
        buffers.EnterMethodTablesContext(&methodTables, rootsCount);
        buffers.EnterRootsContext(&roots, rootsCount);

        methodTables[0] = objectMethodTable;
        var objectIndex = 0u;
        var inputEndpoint = input + inputLength;
        while (input != inputEndpoint)
        {
            var methodTable = methodTables[objectIndex];
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
                    objects[objectIndex] = *(nint*)&@object; 
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
                                methodTables[elementIdentifier] = elementMethodTable;

                                // root: { i4 index; i4 offset }
                                roots[rootsCount++] = (ulong)(offset + SizeOf.MethodTable) << 32 | objectIndex;
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
                            Unsafe.AsRef<object>(objects + objectIndex) = string.Empty;
                        else Unsafe.AsRef<object>(objects + objectIndex) = new string((char*)(input + SizeOf.StringLength), 0, (int)length);
                    }
                    else
                    {
                        @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));
                        **(MethodTable***)&@object = GetMethodTable<string>();
                        Unsafe.CopyBlockUnaligned(*(nint**)&@object + 1, input, objectSize);
                        objects[objectIndex] = *(nint*)&@object; 
                    }

                    input += objectSize;
                }
            }
            else
            {
                @object = RuntimeHelpers.GetUninitializedObject(Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr((nint)methodTable)));
                objects[objectIndex] = *(nint*)&@object;

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

                                if (methodTables[objectIdentifier] is null)
                                {
                                    var fieldHandle = RuntimeFieldHandle.FromIntPtr((nint)fieldDesc);
                                    var declaringTypeHandle = RuntimeTypeHandle.FromIntPtr((nint)methodTable);
                                    var field = FieldInfo.GetFieldFromHandle(fieldHandle, declaringTypeHandle);

                                    var fieldMethodTable = (MethodTable*)field.FieldType.TypeHandle.Value;
                                    methodTables[objectIdentifier] = fieldMethodTable;
                                }

                                // root: { i4 index; i4 offset }
                                roots[rootsCount++] = (ulong)(offset + SizeOf.MethodTable) << 32 | (uint)objectIndex;
                            }
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }

                input += objectSize;
            }

            objectIndex++;
        }

        for (var rootIndex = 0; rootIndex < rootsCount; rootIndex++)
        {
            var root = roots[rootIndex];

            @object = objects[root & ~0U];
            var pointer = (nint*)(*(byte**)&@object + (root >> 32));
            *(object*)pointer = objects[*(uint*)pointer - 1];
        }

        buffers.ExitObjectsContext(objects, objectIndex);
        buffers.ExitMethodTablesContext(methodTables, objectIndex);

        return objects[0];
    }
}