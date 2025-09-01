global using static DotnetFastestMemoryPacker.Internal.ExtrinsicsImpl;
global using static PatcherReference.Extrinsics;
using DotnetFastestMemoryPacker.Internal;
using System.Runtime.CompilerServices;
using System.Reflection;
using PatcherReference;

[module: SkipLocalsInit]

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker;
public unsafe static class FastestMemoryPacker
{
    public static byte[] Serialize<T>(in T objectToSerialize)
    {
        Pinnable(out byte[] bytesArray);
        Pinnable(out object @object);

        MethodTable* methodTable;
        // value type is the only instantiation that can be handled by jit as a constraintable branch.
        // unfortunately, it cannot generate a direct path for unmanaged structures unless additional constraints are applied to T,
        // but obtaining a method table for structures quickly enough to eliminate this problem.
        if (typeof(T).IsValueType)
        {
            methodTable = GetMethodTable<T>();
            if (!methodTable->ContainsGCPointers)
            {
                bytesArray = GC.AllocateUninitializedArray<byte>(sizeof(T));
                Unsafe.Write(GetArrayBody(bytesArray), objectToSerialize);
                return bytesArray;
            }
        }

        @object = objectToSerialize;
        if (@object is null)
            return [];

        methodTable = GetMethodTable(@object);
        if (methodTable->ContainsGCPointers)
            return SerializeWithGCPointers(methodTable, objectToSerialize);

        uint length;
        if (methodTable->HasComponentSize)
        {
            length = GetComponentsCount(@object);
            if (methodTable->IsArray)
            {
                var componentsSize = length * methodTable->ComponentSize;
                var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;
                length = componentsSize + headerSize;
            }
            else length = (length << 1) + SizeOf.StringLength;
        }
        else length = methodTable->BaseSize - methodTable->Class->BaseSizePadding;

        bytesArray = GC.AllocateUninitializedArray<byte>((int)(length + SizeOf.PackedHeader));
        
        var destination = GetArrayBody(bytesArray) + SizeOf.PackedHeader;
        var source = GetObjectBody(@object);
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

        var buffers = SerializationBuffers.ThreadLocal;
        buffers.EnterObjectsContext(&objects, &objectsCapacity);
        buffers.EnterSizesContext(&sizes, &sizesCapacity);

        @object = objectToSerialize;
        objects[0] = As<nint>(@object);

        var objectsCount = 1U;
        var objectIndex = 0U;
        while (true)
        {
            methodTable = GetMethodTable(@object);
            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var bodyPointer = GetObjectBody(@object);
                    var arrayLength = *(uint*)bodyPointer;
                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;

                    if (methodTable->ContainsGCPointers)
                    {
                        var requiredCapacity = objectsCount + arrayLength;
                        buffers.EnsureObjectsCapacity(requiredCapacity, ref objects, ref objectsCapacity);
                        buffers.EnsureSizesCapacity(requiredCapacity, ref sizes, ref sizesCapacity);

                        var componentsSize = arrayLength << 3;
                        var objectSize = sizes[objectIndex] = headerSize + componentsSize;
                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            innerObject = Unsafe.AsRef<object>(bodyPointer + offset);
                            if (innerObject is null)
                                continue;

                            buffers.AddObject(innerObject, objects, ref objectsCount);
                        }
                    }
                    else
                    {
                        var componentsSize = arrayLength * methodTable->ComponentSize;
                        sizes[objectIndex] = headerSize + componentsSize;
                    }
                }
                else
                {
                    var stringLength = GetStringLength(@object);
                    sizes[objectIndex] = SizeOf.StringLength + (stringLength << 1);
                }
            }
            else
            {
                var eeClass = methodTable->Class;
                sizes[objectIndex] = methodTable->BaseSize - eeClass->BaseSizePadding;

                if (methodTable->ContainsGCPointers)
                {
                    var fieldsCount = eeClass->NumInstanceFields;
                    var requiredCapacity = objectsCount + fieldsCount;
                    buffers.EnsureObjectsCapacity(requiredCapacity, ref objects, ref objectsCapacity);
                    buffers.EnsureSizesCapacity(requiredCapacity, ref sizes, ref sizesCapacity);

                    var bodyPointer = LoadEffectiveAddress(@object, SizeOf.MethodTable);
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

                            buffers.AddObject(innerObject, objects, ref objectsCount);
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

        var totalSize = IntrinsicsImpl.Sum(sizes, objectsCount);
        bytesArray = GC.AllocateUninitializedArray<byte>((int)(SizeOf.PackedHeader + totalSize));
        var output = GetArrayBody(bytesArray);

        *(uint*)output = objectsCount;
        output += SizeOf.PackedHeader;

        var bytesOffset = 0u;
        for (objectIndex = 0u; objectIndex < objectsCount; objectIndex++)
        {
            @object = Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectIndex);
            var size = sizes[objectIndex];

            Unsafe.CopyBlockUnaligned(output + bytesOffset, GetObjectBody(@object), size);
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

        var objectsCount = 1U;
        var objectIndex = 0U;
        var rootsCount = 0U;
        var totalSize = 0UL;

        var buffers = SerializationBuffers.ThreadLocal;
        buffers.EnterObjectsContext(&objects, &objectsCapacity);
        buffers.EnterSizesContext(&sizes, &sizesCapacity);
        buffers.EnterRootsContext(&roots, &rootsCapacity);

        @object = objectToSerialize;
        objects[0] = As<nint>(@object);

        while (true)
        {
            methodTable = GetMethodTable(@object);
            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var bodyPointer = GetObjectBody(@object);
                    var arrayLength = *(uint*)bodyPointer;
                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;

                    if (methodTable->ContainsGCPointers)
                    {
                        var requiredCapacity = objectsCount + arrayLength;
                        buffers.EnsureObjectsCapacity(requiredCapacity, ref objects, ref objectsCapacity);
                        buffers.EnsureSizesCapacity(requiredCapacity, ref sizes, ref sizesCapacity);
                        buffers.EnsureRootsCapacity(requiredCapacity, ref roots, ref rootsCapacity);

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
                    var stringLength = GetStringLength(@object);
                    var objectSize = sizes[objectIndex] = SizeOf.StringLength + (stringLength << 1);
                    totalSize += objectSize;
                }
            }
            else
            {
                var eeClass = methodTable->Class;
                var objectSize = sizes[objectIndex] = methodTable->BaseSize - eeClass->BaseSizePadding;

                if (methodTable->ContainsGCPointers)
                {
                    var fieldsCount = eeClass->NumInstanceFields;
                    var requiredCapacity = objectsCount + fieldsCount;
                    buffers.EnsureObjectsCapacity(requiredCapacity, ref objects, ref objectsCapacity);
                    buffers.EnsureSizesCapacity(requiredCapacity, ref sizes, ref sizesCapacity);
                    buffers.EnsureRootsCapacity(requiredCapacity, ref roots, ref rootsCapacity);

                    var bodyPointer = GetObjectBody(@object);
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
                                buffers.AddObject(innerObject, objects, ref objectsCount);
                            }

                            // root: { i4 index; i4 offset }
                            var root = totalSize + offset << 32 | (uint)index;
                            buffers.AddRoot(root, roots, ref rootsCount);
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }

                totalSize += objectSize;
            }

            if (++objectIndex == objectsCount)
                break;

            @object = Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectIndex);
        }

        bytesArray = GC.AllocateUninitializedArray<byte>((int)(SizeOf.PackedHeader + totalSize));
        var bytes = GetArrayBody(bytesArray);

        *(uint*)bytes = rootsCount | (1U << 31)/*object identify state: true*/;
        bytes += SizeOf.PackedHeader;

        var bytesOffset = 0u;
        for (objectIndex = 0u; objectIndex < objectsCount; objectIndex++)
        {
            @object = Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectIndex);
            var size = sizes[objectIndex];            

            Unsafe.CopyBlockUnaligned(bytes + bytesOffset, GetObjectBody(@object), size);
            bytesOffset += size;
        }

        for (var rootIndex = 0U; rootIndex < rootsCount; rootIndex++)
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

        if (bytes.Length == 0)
            return default;

        bytesArray = bytes;
        var input = GetArrayBody(bytesArray);        

        var methodTable = GetMethodTable<T>();
        if (methodTable->ContainsGCPointers)
        {
            var inputLength = GetArrayLength(bytesArray);
            var header = *(uint*)input;
            input += SizeOf.PackedHeader;
            inputLength -= SizeOf.PackedHeader;

            var hasObjectIdentify = (header & (1 << 31)) > 0U;
            if (hasObjectIdentify)
            {
                var rootsCount = header & ~(1 << 31);
                return (T)DeserializeWithGCPointersWithObjectIdentify(input, inputLength, methodTable, rootsCount);
            }

            return (T)DeserializeWithGCPointersWithObjectIdentify(input, inputLength, methodTable, header);
        }
        else if (typeof(T).IsValueType)
        {
            return Unsafe.Read<T>(input);
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
                SetMethodTable(@object, methodTable);

                var destination = GetObjectBody(@object);
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
                    SetMethodTable(@object, GetMethodTable<string>());

                    var destination = GetObjectBody(@object);
                    var source = input;
                    Unsafe.CopyBlock(destination, source, objectSize);
                }
            }
        }
        else
        {
            @object = RuntimeHelpers.GetUninitializedObject(typeof(T));

            var objectSize = methodTable->BaseSize - methodTable->Class->BaseSizePadding;
            var destination = GetObjectBody(@object);
            var source = input;
            Unsafe.CopyBlock(destination, source, objectSize);
        }

        return (T)@object;
    }

    static object DeserializeWithGCPointersWithObjectIdentify(byte* input, uint inputLength, MethodTable* objectMethodTable, uint rootsCount)
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
        var rootIndex = 0u;
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

                    if (methodTable->ContainsGCPointers)
                    {
                        @object = new object[arrayLength];
                        var elementMethodTable = methodTable->ElementType;
                        var objectBody = input;
                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            var elementIdentifier = *(int*)(objectBody + offset);
                            if (elementIdentifier > 0)
                            {
                                methodTables[--elementIdentifier] = elementMethodTable;

                                // root: { i4 index; i4 offset }
                                var root = (ulong)(offset + SizeOf.MethodTable) << 32 | objectIndex;
                                buffers.AddRoot(root, roots, ref rootIndex);
                            }
                        }
                    }
                    else
                    {
                        @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));
                        Unsafe.CopyBlockUnaligned(GetObjectBody(@object), input, objectSize);
                    }
                    SetMethodTable(@object, methodTable);
                    buffers.SetObject(@object, objects, objectIndex);

                    input += objectSize;
                }
                else
                {
                    var length = *(uint*)input;
                    var objectSize = SizeOf.StringLength + (length << 1);
                    if (length <= 2U)
                    {
                        if (length == 0)
                            Unsafe.AsRef<object>(objects + objectIndex) = string.Empty;
                        else Unsafe.AsRef<object>(objects + objectIndex) = new string((char*)(input + SizeOf.StringLength), 0, (int)length);
                    }
                    else
                    {
                        @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));
                        SetMethodTable<string>(@object);
                        Unsafe.CopyBlockUnaligned(GetObjectBody(@object), input, objectSize);
                        objects[objectIndex] = As<nint>(@object); 
                    }

                    input += objectSize;
                }
            }
            else
            {
                @object = RuntimeHelpers.GetUninitializedObject(methodTable->GetRuntimeType());
                objects[objectIndex] = As<nint>(@object);

                var eeClass = methodTable->Class;
                var objectSize = methodTable->BaseSize - eeClass->BaseSizePadding;

                if (methodTable->ContainsGCPointers)
                {
                    var objectBody = input;
                    while (true)
                    {
                        var parentMethodTable = methodTable->ParentMethodTable;
                        if (parentMethodTable is null) // it means that current methodTable is object
                            break;

                        var parentClass = parentMethodTable->Class;
                        int fieldCount = eeClass->NumInstanceFields - parentClass->NumInstanceFields;

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
                                var root = (ulong)(offset + SizeOf.MethodTable) << 32 | objectIndex;
                                buffers.AddRoot(root, roots, ref rootIndex);
                            }
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }
                else
                {
                    Unsafe.CopyBlockUnaligned(GetObjectBody(@object), input, objectSize);
                }

                input += objectSize;
            }

            objectIndex++;
        }

        for (rootIndex = 0; rootIndex < rootsCount; rootIndex++)
        {
            var root = roots[rootIndex];

            var ownerReferenceIndex = (int)(root & ~0U);
            @object = Unsafe.Add(ref Unsafe.AsRef<object>(objects), ownerReferenceIndex);
            var pointer = LoadEffectiveAddress(@object, root >> 32);
            var targetReferenceIndex = *(uint*)pointer - 1;
            *(object*)pointer = Unsafe.Add(ref Unsafe.AsRef<object>(objects), targetReferenceIndex);
        }

        @object = Unsafe.AsRef<object>(objects);

        buffers.ExitObjectsContext(objects, objectIndex);
        buffers.ExitMethodTablesContext(methodTables, objectIndex);

        return @object;
    }
}