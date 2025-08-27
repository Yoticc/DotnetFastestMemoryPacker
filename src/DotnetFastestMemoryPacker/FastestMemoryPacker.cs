using DotnetFastestMemoryPacker.Internal;
using PatcherReference;
using System.Buffers;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

[module: SkipLocalsInit]

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker;
public unsafe static class FastestMemoryPacker
{
    class Cache<T>
    {
        static Cache()
        {
            var type = typeof(T);
            var typeHandle = type.TypeHandle;

            Type = type;
            MethodTable = (MethodTable*)typeHandle.Value;
            Class = MethodTable->Class;
            BaseSize = MethodTable->BaseSize;
            ComponentSize = MethodTable->ComponentSize;
            ComponentSizePowerOfTwo = (uint)(32u - BitOperations.LeadingZeroCount(ComponentSize));
            BaseSizePadding = Class->BaseSizePadding;
            ContainsGCPointers = MethodTable->ContainsGCPointers;
            IsValueType = MethodTable->IsValueType;
            HasComponentSize = MethodTable->HasComponentSize;
            IsArray = MethodTable->IsArray;
            ComponentSizeIsAPowerOfTwo = (ComponentSize & (ComponentSize - 1)) == 0;
        }

        public readonly static Type Type;
        public readonly static MethodTable* MethodTable;
        public readonly static EEClass* Class;
        public readonly static uint BaseSize;
        public readonly static uint ComponentSize;
        public readonly static uint ComponentSizePowerOfTwo;
        public readonly static uint BaseSizePadding;
        public readonly static bool ContainsGCPointers;
        public readonly static bool IsValueType;
        public readonly static bool HasComponentSize;
        public readonly static bool IsArray;
        public readonly static bool ComponentSizeIsAPowerOfTwo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetArraySize(uint length)
        {
            if (ComponentSizeIsAPowerOfTwo)
                return length << (int)ComponentSizePowerOfTwo;

            return length * ComponentSize;
        }
    }

    const uint InitialObjectsCapacity = 1 << 5;
    const uint InitialObjectSizesCapacity = 1 << 13;
    const uint InitialObjectRootsCapacity = 1 << 13;

    public static byte[] Serialize<T>(T objectToSerialize)
    {
        Patcher.Pinnable(out byte[] bytesArray);
        Patcher.Pinnable(out object @object);

        if (Cache<T>.ContainsGCPointers)
        {
            if (objectToSerialize is null)
                return [];

            /*
                
            */
            var objectSizes = stackalloc uint[(int)InitialObjectSizesCapacity];
            var objectRoots = stackalloc ulong[(int)InitialObjectRootsCapacity];
            return SerializeWithGCPointers(objectSizes, objectRoots, Cache<T>.MethodTable, objectToSerialize);
        }

        if (Cache<T>.IsValueType)
        {
            bytesArray = GC.AllocateUninitializedArray<byte>(sizeof(T));
            Unsafe.Write(*(nint**)&bytesArray + 2, objectToSerialize);

            return bytesArray;
        }

        uint bytesCount;
        if (Cache<T>.HasComponentSize)
        {
            if (Cache<T>.IsArray)
            {
                @object = objectToSerialize;
                var arrayLength = *(uint*)(*(nint*)&@object + SizeOf.MethodTable);
                var componentsSize = arrayLength * Cache<T>.ComponentSize;
                var headerSize = Cache<T>.BaseSize - SizeOf.ObjectHeader;
                bytesCount = headerSize + componentsSize;
            }
            else
            {
                @object = objectToSerialize;
                var stringLength = *(uint*)(*(nint*)&@object + SizeOf.MethodTable);
                bytesCount = SizeOf.StringLength + (stringLength << 1);
            }
        }
        else
        {
            bytesCount = Cache<T>.BaseSize - Cache<T>.BaseSizePadding;
        }

        bytesArray = GC.AllocateUninitializedArray<byte>((int)(bytesCount + SizeOf.PackedHeader));
        var bytes = (byte*)(*(nint**)&bytesArray + 2);

        var destination = bytes + SizeOf.PackedHeader;
        var source = (void*)(*(nint*)&@object + SizeOf.MethodTable);
        Unsafe.CopyBlock(destination, source, bytesCount);

        return bytesArray;
    }

    public static T Deserialize<T>(byte[] bytes)
    {
        Patcher.Pinnable(out object @object);
        Patcher.Pinnable(out byte[] bytesArray);

        bytesArray = bytes;
        var input = (byte*)(*(nint**)&bytesArray + 2);

        if (Cache<T>.ContainsGCPointers)
        {
            if (bytes.Length == 0)
                return default;

            var inputLength = *(int*)(*(nint**)&bytesArray + 1);
            return (T)DeserializeWithGCPointers(input, inputLength, Cache<T>.MethodTable);
        }

        if (Cache<T>.IsValueType)
            return Unsafe.Read<T>(input);

        input += SizeOf.PackedHeader;

        if (Cache<T>.HasComponentSize)
        {
            if (Cache<T>.IsArray)
            {
                var arrayLength = *(uint*)input;
                var arraySize = Cache<T>.GetArraySize(arrayLength);
                var headerSize = Cache<T>.BaseSize - SizeOf.ObjectHeader;
                var objectSize = headerSize + arraySize;

                @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));
                **(MethodTable***)&@object = Cache<T>.MethodTable;

                var destination = *(byte**)&@object + SizeOf.MethodTable;
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

                    **(MethodTable***)&@object = Cache<string>.MethodTable;
                    var destination = *(nint**)&@object + 1;
                    var source = input;
                    Unsafe.CopyBlock(destination, source, objectSize);
                }
            }
        }
        else
        {
            @object = RuntimeHelpers.GetUninitializedObject(Cache<T>.Type);

            var objectSize = Cache<T>.BaseSize - Cache<T>.BaseSizePadding;
            var destination = *(byte**)&@object + SizeOf.MethodTable;
            var source = input;
            Unsafe.CopyBlock(destination, source, objectSize);
        }

        return (T)@object;
    }

    static byte[] SerializeWithGCPointers(uint* objectSizes, ulong* objectRoots, MethodTable* methodTable, object objectToSerialize)
    {
        Patcher.Pinnable(out object @object);
        Patcher.Pinnable(out byte[] bytesArray);
        Patcher.Pinnable(out object[] objectsArray);
        Patcher.Pinnable(out uint[] objectSizesArray);
        Patcher.Pinnable(out uint[] newObjectSizesArray);
        Patcher.Pinnable(out ulong[] objectRootsArray);
        Patcher.Pinnable(out ulong[] newObjectRootsArray);

        var objectsCapacity = InitialObjectsCapacity;
        var objectSizesCapacity = InitialObjectSizesCapacity;
        var objectRootsCapacity = InitialObjectRootsCapacity;
        objectRootsArray = null;
        objectSizesArray = null;

        objectsArray = ArrayPool<object>.Shared.Rent((int)objectsCapacity);
        var objects = *(nint**)&objectsArray + 2;

        var objectsCount = 1U;
        var totalSize = 0UL;
        var objectRootsCount = 0U;

        @object = objectToSerialize;
        *objects = *(nint*)&@object;
        for (var objectIndex = 0U; ;)
        {
            methodTable = **(MethodTable***)&@object;
            if (methodTable->HasComponentSize)
            {
                if (methodTable->IsArray)
                {
                    var bodyPointer = *(byte**)&@object + SizeOf.MethodTable;
                    var arrayLength = *(uint*)(*(byte**)&@object + SizeOf.MethodTable);
                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;

                    if (methodTable->ContainsGCPointers)
                    {
                        var componentsSize = arrayLength << 3;
                        var objectSize = objectSizes[objectIndex] = headerSize + componentsSize;

                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            Serialization_AddObject(
                                bodyPointer,
                                ref objects,
                                ref objectsArray,
                                ref objectsCount,
                                ref objectsCapacity,
                                ref objectRoots,
                                ref objectRootsArray,
                                ref newObjectRootsArray,
                                ref objectRootsCount,
                                ref objectRootsCapacity,
                                ref objectSizes,
                                ref objectSizesArray,
                                ref newObjectSizesArray,
                                ref objectSizesCapacity,
                                totalSize,
                                offset
                            );
                        }

                        totalSize += objectSize;
                    }
                    else
                    {
                        var componentsSize = arrayLength * methodTable->ComponentSize;
                        var objectSize = objectSizes[objectIndex] = headerSize + componentsSize;
                        totalSize += objectSize;
                    }
                }
                else
                {
                    var stringLength = *(uint*)(*(nint*)&@object + SizeOf.MethodTable);
                    var objectSize = objectSizes[objectIndex] = SizeOf.StringLength + (stringLength << 1);
                    totalSize += objectSize;
                }
            }
            else
            {
                var eeClass = methodTable->Class;
                var objectSize = objectSizes[objectIndex] = methodTable->BaseSize - eeClass->BaseSizePadding;
                totalSize += objectSize;

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

                            Serialization_AddObject(
                                (void*)(*(nint*)&@object + SizeOf.MethodTable),
                                ref objects,
                                ref objectsArray,
                                ref objectsCount,
                                ref objectsCapacity,
                                ref objectRoots,
                                ref objectRootsArray,
                                ref newObjectRootsArray,
                                ref objectRootsCount,
                                ref objectRootsCapacity,
                                ref objectSizes,
                                ref objectSizesArray,
                                ref newObjectSizesArray,
                                ref objectSizesCapacity,
                                totalSize,
                                fieldDesc->Offset
                            );
                        }

                        methodTable = parentMethodTable;
                        eeClass = parentClass;
                    }
                }
            }

            objectIndex++;
            if (objectIndex == objectsCount)
                break;

            @object = Unsafe.Add(ref Unsafe.AsRef<string>(objects), objectIndex);
        }

        bytesArray = GC.AllocateUninitializedArray<byte>((int)(SizeOf.PackedHeader + totalSize));
        var bytes = (byte*)(*(nint**)&bytesArray + 2);

        *(ulong*)bytes = objectsCount | (ulong)objectRootsCount << 32;
        bytes += SizeOf.PackedHeader;

        for (uint objectIndex = 0, bytesOffset = 0; objectIndex < objectsCount; objectIndex++)
        {
            @object = Unsafe.Add(ref Unsafe.AsRef<string>(objects), objectIndex);
            var size = objectSizes[objectIndex];            

            Unsafe.CopyBlockUnaligned(bytes + bytesOffset, *(byte**)&@object + SizeOf.MethodTable, size);
            bytesOffset += size;
        }

        if (objectSizesArray is not null)
            ArrayPool<uint>.Shared.Return(objectSizesArray);

        ArrayPool<object>.Shared.Return(objectsArray);

        for (var rootIndex = 0; rootIndex < objectRootsCount; rootIndex++)
        {
            var root = objectRoots[rootIndex];
            // bytes + root.offset = root.index + 1
            *(ulong*)(bytes + (root >> 32)) = (root & ~0U) + 1;
        }

        if (objectRootsArray is not null)
            ArrayPool<ulong>.Shared.Return(objectRootsArray);

        return bytesArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Serialization_AddObject(
        void* ownerObject,
        ref nint* objects,
        ref object[] objectsArray,
        ref uint objectsCount,
        ref uint objectsCapacity,
        ref ulong* objectRoots,
        ref ulong[] objectRootsArray,
        ref ulong[] newObjectRootsArray,
        ref uint objectRootsCount,
        ref uint objectRootsCapacity,
        ref uint* objectSizes,
        ref uint[] objectSizesArray,
        ref uint[] newObjectSizesArray,
        ref uint objectSizesCapacity,
        ulong totalSize,
        uint offset)
    {
        Patcher.Pinnable(out object @object);
        @object = Unsafe.AddByteOffset(ref Unsafe.AsRef<object>(ownerObject), offset);

        if (@object is null)
            return;

        var index = new Span<nint>(objects, (int)objectsCount).LastIndexOf(*(nint*)&@object);
        if (index == -1)
        {
            index = (int)objectsCount;
            objectsArray[objectsCount++] = @object;
            if (objectsCount == objectsCapacity)
            {
                var newObjectsArray = ArrayPool<object>.Shared.Rent((int)(objectsCapacity <<= 1));
                Array.Copy(objectsArray, newObjectsArray, objectsCount);
                ArrayPool<object>.Shared.Return(objectsArray);
                objectsArray = newObjectsArray;
                objects = *(nint**)Unsafe.AsPointer(ref objectsArray) + 2;
            }

            if (objectsCount == objectSizesCapacity)
            {
                newObjectSizesArray = ArrayPool<uint>.Shared.Rent((int)(objectSizesCapacity <<= 1));
                var source = objectSizes;
                var destination = objectSizes = (uint*)(*(nint**)Unsafe.AsPointer(ref newObjectSizesArray) + 2);
                Unsafe.CopyBlock(destination, source, objectsCount * sizeof(uint));

                if (objectSizesArray is not null)
                    ArrayPool<uint>.Shared.Return(objectSizesArray);

                objectSizesArray = newObjectSizesArray;
            }
        }

        // root: { i4 index; i4 offset }
        objectRoots[objectRootsCount++] = totalSize + offset << 32 | (uint)index;
        if (objectRootsCount == objectRootsCapacity)
        {
            newObjectRootsArray = ArrayPool<ulong>.Shared.Rent((int)(objectRootsCapacity <<= 1));
            var source = objectRoots;
            var destination = objectRoots = (ulong*)(*(nint**)Unsafe.AsPointer(ref newObjectRootsArray) + 2);
            Unsafe.CopyBlock(destination, source, objectRootsCount * sizeof(ulong));

            if (objectRootsArray is not null)
                ArrayPool<ulong>.Shared.Return(objectRootsArray);

            objectRootsArray = newObjectRootsArray;
        }
    }

    static object DeserializeWithGCPointers(byte* input, int inputLength, MethodTable* objectMethodTable)
    {
        Patcher.Pinnable(out ulong[] objectRootsArray);
        Patcher.Pinnable(out object[] objectsArray);
        Patcher.Pinnable(out MethodTable*[] objectMethodTablesArray);
        Patcher.Pinnable(out object @object);
        Patcher.Pinnable(out object fieldObject);

        var packedHeader = *(ulong*)input;
        var objectsCount = (uint)(packedHeader & ~0u);
        var objectRootsCapacity = (uint)(packedHeader >> 32);
        input += SizeOf.PackedHeader;

        objectsArray = ArrayPool<object>.Shared.Rent((int)objectsCount);
        var objects = (object*)(*(nint**)&objectsArray + 2);

        var objectRoots = stackalloc ulong[(int)InitialObjectRootsCapacity];
        if (objectRootsCapacity > InitialObjectRootsCapacity)
        {
            objectRootsArray = ArrayPool<ulong>.Shared.Rent((int)objectRootsCapacity);
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

        var objectRootsCount = 0;
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
                        **(MethodTable***)&@object = Cache<string>.MethodTable;
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

        if (objectRootsArray is not null)
            ArrayPool<ulong>.Shared.Return(objectRootsArray);

        @object = objects[0];
        ArrayPool<object>.Shared.Return(objectsArray);

        return objects[0];
    }
}