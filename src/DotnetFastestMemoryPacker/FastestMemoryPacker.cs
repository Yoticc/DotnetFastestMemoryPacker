using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

[module: SkipLocalsInit]








namespace DotnetFastestMemoryPacker;
public unsafe static class FastestMemoryPacker
{
    // root structure                      
    //
    // 64 bit
    // [                 u8                 ]
    // [       u4      ][         u4        ]
    // [reference index][offset to reference]
    //
    // 128 bit
    // [                             v16                            ]
    // [     u4     ][         u4        ][       u4      ][   u4   ]
    // [object index][offset to reference][reference index]----------

    // header structure
    //
    // [    u4    ]
    // [root count]
    // 
    // object count < root count
    //
    // in future:
    // [                          u4                        ]
    // [     b26    ][        5b       ][         1b        ]
    // [object count][^2 for root count][has object identify]

    public static byte[] Serialize<T>(in T objectToSerialize)
    {
        Pinnable(out byte[] bytesArray);
        Pinnable(out object @object);

        MethodTable* methodTable;
        // thank you, EgorBo, for such excellent optimization:
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

        if (objectToSerialize is null)
            return [];

        @object = objectToSerialize;
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
        
        var destination = GetArrayBody(bytesArray) + SizeOf.PackedHeader;
        var source = GetObjectBody(@object);
        Unsafe.CopyBlock(destination, source, length);

        return bytesArray;
    }

    public static byte[] SerializeWithObjectIdentify<T>(in T objectToSerialize)
    {
        if (objectToSerialize is null)
            return [];

        var methodTable = GetMethodTable<T>();
        return SerializeWithGCPointersWithObjectIdentify(methodTable, objectToSerialize);
    }

    static byte[] SerializeWithGCPointers(MethodTable* methodTable, object objectToSerialize)
    {
        Pinnable(out object @object);
        Pinnable(out object innerObject);
        Pinnable(out byte[] bytesArray);

        object* objects;
        uint* sizes;
        uint objectsCapacity;
        uint sizesCapacity;

        var buffers = SerializationBuffers.ThreadLocal;
        buffers.EnterObjectsContext(&objects, &objectsCapacity);
        buffers.EnterSizesContext(&sizes, &sizesCapacity);

        @object = objectToSerialize;
        buffers.SetObject(@object, objects, 0);

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
                        buffers.EnsureObjectsCapacity(requiredCapacity, &objects, ref objectsCapacity);
                        buffers.EnsureSizesCapacity(requiredCapacity, &sizes, ref sizesCapacity);

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
                    buffers.EnsureObjectsCapacity(requiredCapacity, &objects, ref objectsCapacity);
                    buffers.EnsureSizesCapacity(requiredCapacity, &sizes, ref sizesCapacity);

                    var bodyPointer = LoadEffectiveAddress(@object, SizeOf.MethodTable);
                    while (true)
                    {
                        var parentMethodTable = methodTable->ParentMethodTable;
                        if (parentMethodTable is null)
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

            @object = Unsafe.AsRef<object>(objects + objectIndex);
        }

        var totalSize = IntrinsicsImpl.Sum(sizes, objectsCount);
        bytesArray = GC.AllocateUninitializedArray<byte>((int)(SizeOf.PackedHeader + totalSize));
        var output = GetArrayBody(bytesArray);

        *(uint*)output = objectsCount;
        output += SizeOf.PackedHeader;

        var bytesOffset = 0U;
        for (objectIndex = 0U; objectIndex < objectsCount; objectIndex++)
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

        object* objects;
        uint* sizes;
        Vector128<ulong>* roots;
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
        buffers.SetObject(@object, objects, 0);

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
                        buffers.EnsureObjectsCapacity(requiredCapacity, &objects, ref objectsCapacity);
                        buffers.EnsureSizesCapacity(requiredCapacity, &sizes, ref sizesCapacity);
                        buffers.EnsureRoots64Capacity(requiredCapacity, &roots, ref rootsCapacity);

                        var componentsSize = arrayLength << 3;
                        var objectSize = sizes[objectIndex] = headerSize + componentsSize;
                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            innerObject = Unsafe.AsRef<object>(bodyPointer + offset);
                            if (innerObject is null)
                                continue;

                            var index = new Span<nint>(objects, (int)objectsCount).LastIndexOf(*(nint*)&innerObject); // As<nint>(innerObject) ?
                            if (index == -1)
                            {
                                index = (int)objectsCount;
                                buffers.AddObject(innerObject, objects, ref objectsCount);
                            }

                            buffers.AddRoot64(offset: (uint)(totalSize + offset), index: (uint)index, roots, ref rootsCount);
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
                    buffers.EnsureObjectsCapacity(requiredCapacity, &objects, ref objectsCapacity);
                    buffers.EnsureSizesCapacity(requiredCapacity, &sizes, ref sizesCapacity);
                    buffers.EnsureRoots64Capacity(requiredCapacity, &roots, ref rootsCapacity);

                    var bodyPointer = GetObjectBody(@object);
                    while (true)
                    {
                        var parentMethodTable = methodTable->ParentMethodTable;
                        if (parentMethodTable is null)
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

                            buffers.AddRoot64(offset: (uint)(totalSize + offset), index: (uint)index, roots, ref rootsCount);
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }

                totalSize += objectSize;
            }

            if (++objectIndex == objectsCount)
                break;

            @object = Unsafe.AsRef<object>(objects + objectIndex);
        }

        bytesArray = GC.AllocateUninitializedArray<byte>((int)(SizeOf.PackedHeader + totalSize));
        var bytes = GetArrayBody(bytesArray);

        *(uint*)bytes = rootsCount;
        bytes += SizeOf.PackedHeader;

        var bytesOffset = 0U;
        for (objectIndex = 0U; objectIndex < objectsCount; objectIndex++)
        {
            @object = Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectIndex);
            var size = sizes[objectIndex];

            Unsafe.CopyBlockUnaligned(bytes + bytesOffset, GetObjectBody(@object), size);
            bytesOffset += size;                                                                
        }                                                                                       
                                                                                                
        for (var rootIndex = 0U; rootIndex < rootsCount; rootIndex++)                           
        {                                                                                       
            buffers.GetRoot64(out var offset, out var index, roots, rootIndex);                 
            *(ulong*)(bytes + offset) = index + 1;
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
            return (T)DeserializeWithGCPointers(input, inputLength, methodTable, header);
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

                Unsafe.CopyBlock(destination: GetObjectBody(@object), source: input, objectSize);
            }
            else
            {
                ObjectAllocator.AllocateStringFromItsBody(ref @object, input);
            }
        }
        else
        {
            @object = RuntimeHelpers.GetUninitializedObject(typeof(T));

            var objectSize = methodTable->BaseSize - methodTable->Class->BaseSizePadding;
            Unsafe.CopyBlock(destination: GetObjectBody(@object), source: input, objectSize);
        }

        return (T)@object;
    }

    static object DeserializeWithGCPointers(byte* input, uint inputLength, MethodTable* methodTable, uint rootsCount)
    {
        Pinnable(out object @object);
        Pinnable(out object innerObject);

        object* objects;
        Vector128<ulong>* roots;
        MethodTable** methodTables;

        var buffers = SerializationBuffers.ThreadLocal;
        buffers.EnterObjectsContext(&objects, rootsCount);
        buffers.EnterMethodTablesContext(&methodTables, rootsCount);
        buffers.EnterRootsContext(&roots, rootsCount);

        var inputEndpoint = input + inputLength;
        var objectIndex = 0U;
        var rootIndex = 0U;
        var objectsCount = 1U;
        while (objectIndex < objectsCount)
        {
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
                        var elementMethodTable = methodTable->ElementType;
                        if (headerSize == 8)
                        {
                            if (elementMethodTable == GetMethodTable<string>())
                            {
                                @object = new string[arrayLength];
                            }
                            else
                            {
                                @object = new object[arrayLength];
                                SetMethodTable(@object, methodTable);
                            }
                        }
                        else
                        {
                            var byteCount = (uint)(headerSize & ~8);
                            @object = new object[arrayLength + (byteCount >> 3)];

                            var xmm0 = Vector128.CreateScalarUnsafe((ulong)methodTable);
                            xmm0 = Sse41.Insert(xmm0.As<ulong, uint>(), arrayLength, 2).As<uint, ulong>();
                            Vector128.Store(xmm0, *(ulong**)Unsafe.AsPointer(ref @object));

                            var destination = LoadEffectiveAddress(@object, SizeOf.MethodTable + SizeOf.ArrayLength);
                            var source = input + SizeOf.ArrayLength;
                            Unsafe.CopyBlockUnaligned(destination, source, byteCount);
                        }

                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            var referenceIndex = *(uint*)(input + offset);
                            if (referenceIndex > 0)
                            {
                                if (referenceIndex > objectsCount)
                                    objectsCount = referenceIndex;

                                methodTables[--referenceIndex] = elementMethodTable;
                                buffers.AddRoot128(objectIndex, offset, referenceIndex, roots, ref rootIndex);
                            }
                        }
                    }
                    else
                    {
                        var byteCount = objectSize - SizeOf.ArrayLength;
                        @object = GC.AllocateUninitializedArray<byte>((int)byteCount);

                        var xmm0 = Vector128.CreateScalarUnsafe((ulong)methodTable);
                        xmm0 = Sse41.Insert(xmm0.As<ulong, uint>(), arrayLength, 2).As<uint, ulong>();
                        Vector128.Store(xmm0, *(ulong**)Unsafe.AsPointer(ref @object));

                        var destination = LoadEffectiveAddress(@object, SizeOf.MethodTable + SizeOf.ArrayLength);
                        var source = input + SizeOf.ArrayLength;
                        Unsafe.CopyBlockUnaligned(destination, source, byteCount);
                    }
                    buffers.SetObject(@object, objects, objectIndex);

                    input += objectSize;
                }
                else
                {
                    uint objectSize;
                    ObjectAllocator.AllocateStringFromItsBody(ref @object, input, &objectSize);
                    buffers.SetObject(@object, objects, objectIndex);
                    input += objectSize;
                }
            }
            else
            {
                @object = RuntimeHelpers.GetUninitializedObject(methodTable->GetRuntimeType());
                buffers.SetObject(@object, objects, objectIndex);

                var eeClass = methodTable->Class;
                var objectSize = methodTable->BaseSize - eeClass->BaseSizePadding;

                Unsafe.CopyBlockUnaligned(GetObjectBody(@object), input, objectSize);
                if (methodTable->ContainsGCPointers)
                {
                    while (true)
                    {
                        var parentMethodTable = methodTable->ParentMethodTable;
                        if (parentMethodTable is null)
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
                            var referenceIndex = *(uint*)(input + offset);
                            if (referenceIndex != 0)
                            {
                                if (referenceIndex > objectsCount)
                                    objectsCount = referenceIndex;

                                if (methodTables[--referenceIndex] is null)
                                {
                                    var fieldHandle = RuntimeFieldHandle.FromIntPtr((nint)fieldDesc);
                                    var declaringTypeHandle = RuntimeTypeHandle.FromIntPtr((nint)methodTable);
                                    var field = FieldInfo.GetFieldFromHandle(fieldHandle, declaringTypeHandle);

                                    var fieldMethodTable = (MethodTable*)field.FieldType.TypeHandle.Value;
                                    methodTables[referenceIndex] = fieldMethodTable;
                                }

                                buffers.AddRoot128(objectIndex, offset, referenceIndex, roots, ref rootIndex);
                            }
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }

                input += objectSize;
            }

            methodTable = methodTables[++objectIndex];
        }

        for (rootIndex = 0; rootIndex < rootsCount; rootIndex++)
        {
            buffers.GetRoot128(out var rootObjectIndex, out var referenceOffset, out var referenceIndex, roots, rootIndex);

            @object = buffers.GetObject(objects, rootObjectIndex);
            var referencePointer = LoadEffectiveAddress(@object, referenceOffset);
            Unsafe.AsRef<object>(referencePointer) = buffers.GetObject(objects, referenceIndex);
        }

        @object = buffers.GetObject(objects, 0);

        buffers.ExitObjectsContext(objects, objectsCount);
        buffers.ExitMethodTablesContext(methodTables, objectsCount);

        return @object;
    }

    public static class Advanced
    {
        public static bool AutoClearObjectCache = true;
        public static void ClearObjectCache()
        {
            var buffers = SerializationBuffers.ThreadLocal;
            buffers.ClearObjectsContext();
        }
    }
}