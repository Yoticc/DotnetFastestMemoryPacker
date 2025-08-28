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
    public static byte[] Serialize<T>(T objectToSerialize)
    {
        Patcher.Pinnable(out byte[] bytesArray);
        Patcher.Pinnable(out object @object);

        if (TypeCache<T>.ContainsGCPointers)
        {
            if (objectToSerialize is null)
                return [];

            return SerializeWithGCPointers(TypeCache<T>.MethodTable, objectToSerialize);
        }

        if (TypeCache<T>.IsValueType)
        {
            bytesArray = GC.AllocateUninitializedArray<byte>(sizeof(T));
            Unsafe.Write(*(nint**)&bytesArray + 2, objectToSerialize);

            return bytesArray;
        }

        uint bytesCount;
        if (TypeCache<T>.HasComponentSize)
        {
            if (TypeCache<T>.IsArray)
            {
                @object = objectToSerialize;
                var arrayLength = *(uint*)(*(nint*)&@object + SizeOf.MethodTable);
                var componentsSize = TypeCache<T>.GetArraySize(arrayLength);
                var headerSize = TypeCache<T>.BaseSize - SizeOf.ObjectHeader;
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
            bytesCount = TypeCache<T>.BaseSize - TypeCache<T>.BaseSizePadding;
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

        if (TypeCache<T>.ContainsGCPointers)
        {
            if (bytes.Length == 0)
                return default;

            var inputLength = *(int*)(*(nint**)&bytesArray + 1);
            return (T)DeserializeWithGCPointers(input, inputLength, TypeCache<T>.MethodTable);
        }

        if (TypeCache<T>.IsValueType)
            return Unsafe.Read<T>(input);

        input += SizeOf.PackedHeader;

        if (TypeCache<T>.HasComponentSize)
        {
            if (TypeCache<T>.IsArray)
            {
                var arrayLength = *(uint*)input;
                var arraySize = TypeCache<T>.GetArraySize(arrayLength);
                var headerSize = TypeCache<T>.BaseSize - SizeOf.ObjectHeader;
                var objectSize = headerSize + arraySize;

                @object = GC.AllocateUninitializedArray<byte>((int)(objectSize - SizeOf.ArrayLength));
                **(MethodTable***)&@object = TypeCache<T>.MethodTable;

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

                    **(MethodTable***)&@object = TypeCache<string>.MethodTable;
                    var destination = *(nint**)&@object + 1;
                    var source = input;
                    Unsafe.CopyBlock(destination, source, objectSize);
                }
            }
        }
        else
        {
            @object = RuntimeHelpers.GetUninitializedObject(TypeCache<T>.Type);

            var objectSize = TypeCache<T>.BaseSize - TypeCache<T>.BaseSizePadding;
            var destination = *(byte**)&@object + SizeOf.MethodTable;
            var source = input;
            Unsafe.CopyBlock(destination, source, objectSize);
        }

        return (T)@object;
    }

    static byte[] SerializeWithGCPointers(MethodTable* methodTable, object objectToSerialize)
    {
        Patcher.Pinnable(out object @object);
        Patcher.Pinnable(out object innerObject);
        Patcher.Pinnable(out byte[] bytesArray);
        Patcher.Pinnable(out object[] objectsArray);
        Patcher.Pinnable(out uint[] sizesArray);
        Patcher.Pinnable(out ulong[] rootsArray);

        nint* objects;
        uint* sizes;
        ulong* roots;
        uint objectsCapacity;
        uint sizesCapacity;
        uint rootsCapacity;
        uint objectsCount = 1;
        uint sizesCount = 0;
        uint rootsCount = 0;
        ulong totalSize = 0;

        var buffers = SerializationContextBuffers.ThreadLocal;
        buffers.EnterSerializationContext(&objectsArray, &objects, &objectsCapacity, &sizesArray, &sizes, &sizesCapacity, &rootsArray, &roots, &rootsCapacity);

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
                    var arrayLength = *(uint*)bodyPointer;
                    var headerSize = methodTable->BaseSize - SizeOf.ObjectHeader;

                    if (methodTable->ContainsGCPointers)
                    {
                        buffers.EnsureObjectsCapacity(objectsCount + arrayLength, ref objectsArray, ref objects, ref objectsCapacity);
                        buffers.EnsureSizesCapacity(sizesCount + arrayLength, ref sizesArray, ref sizes, ref sizesCapacity);
                        buffers.EnsureRootsCapacity(rootsCount + arrayLength, ref rootsArray, ref roots, ref rootsCapacity);

                        var componentsSize = arrayLength << 3;
                        var objectSize = sizes[objectIndex] = headerSize + componentsSize;
                        for (var offset = headerSize; offset < objectSize; offset += SizeOf.Reference)
                        {
                            innerObject = *(object*)(bodyPointer + offset);
                            if (innerObject is null)
                                continue;

                            var index = new Span<nint>(objects, (int)objectsCount).LastIndexOf(*(nint*)&innerObject);
                            if (index == -1)
                            {
                                index = (int)objectsCount;
                                buffers.AddObject(innerObject, ref objects, ref objectsCount);
                            }

                            // root: { i4 index; i4 offset }
                            var root = totalSize + offset << 32 | (uint)index;
                            buffers.AddRoot(root, ref roots, ref rootsCount);
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
                    var @objectBody = *(nint*)&@object + SizeOf.MethodTable;
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
                            innerObject = *(object*)(@objectBody + offset);
                            if (innerObject is null)
                                continue;

                            var index = new Span<nint>(objects, (int)objectsCount).LastIndexOf(*(nint*)&innerObject);
                            if (index == -1)
                            {
                                index = (int)objectsCount;
                                buffers.AddObjectAndEnsureCapacity(
                                    innerObject, 
                                    ref objectsArray, ref objects, ref objectsCount, ref objectsCapacity, 
                                    ref sizesArray, ref sizes, ref sizesCapacity);
                            }

                            // root: { i4 index; i4 offset }
                            var root = totalSize + offset << 32 | (uint)index;
                            buffers.AddRootAndEnsureCapacity(root, ref rootsArray, ref roots, ref rootsCount, ref rootsCapacity);
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

        *(ulong*)bytes = objectsCount | (ulong)rootsCount << 32;
        bytes += SizeOf.PackedHeader;

        for (uint objectIndex = 0, bytesOffset = 0; objectIndex < objectsCount; objectIndex++)
        {
            @object = Unsafe.Add(ref Unsafe.AsRef<string>(objects), objectIndex);
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

        if (rootsArray is not null)
            ArrayPool<ulong>.Shared.Return(rootsArray);

        buffers.ExitSerializationContext(objects, objectsCount, objectsArray, sizesArray, rootsArray);

        return bytesArray;
    }

    static object DeserializeWithGCPointers(byte* input, int inputLength, MethodTable* objectMethodTable)
    {
        /*
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
                        **(MethodTable***)&@object = TypeCache<string>.MethodTable;
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
        */

        return null;
    }

    // Generic type variable -> MethodTable cache. needed for optimization of branches in generic method
    class TypeCache<T>
    {
        static TypeCache()
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

    // similar idea to ArrayPool, but very specific and instead of buckets, using one array that expands as needed.
    // the idea is that in the middle of execution all methods will be inlined,
    // because it is important to ensure that every method is inlined in their callers in tier1 or full-opts phase.
    class SerializationContextBuffers
    {
        const int InitialMethodTableCapacity = 2 << 11;
        const int InitialObjectsCapacity = 2 << 11;
        const int InitialSizesCapacity = 2 << 13;
        const int InitialRootsCapacity = 2 << 13;

        SerializationContextBuffers()
        {
            methodTablesArray = new MethodTable*[InitialMethodTableCapacity];
            objectsArray = new object[InitialObjectsCapacity];
            sizesArray = GC.AllocateUninitializedArray<uint>(InitialSizesCapacity);
            rootsArray = GC.AllocateUninitializedArray<ulong>(InitialRootsCapacity);
        }

        uint methodTablesCapacity = InitialMethodTableCapacity;
        uint objectsCapacity = InitialObjectsCapacity;
        uint sizesCapacity = InitialSizesCapacity;
        uint rootsCapacity = InitialRootsCapacity;

        MethodTable*[] methodTablesArray; // nullable, deserialization-context
        object[] objectsArray; // nullable
        uint[] sizesArray; // serialization-context, can has bigger capacity than objects, but not less
        ulong[] rootsArray;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnterSerializationContext(
            /*pinned*/ object[]* objectsArray, nint** objects, uint* objectsCapacity, 
            /*pinned*/ uint[]* sizesArray, uint** sizes, uint* sizesCapacity,
            /*pinned*/ ulong[]* rootsArray, ulong** roots, uint* rootsCapacity)
        {
            *objectsArray = this.objectsArray;
            *sizesArray = this.sizesArray;
            *rootsArray = this.rootsArray;

            *objects = *(nint**)objectsArray + 2;
            *sizes = *(uint**)sizesArray + 2;
            *roots = *(ulong**)rootsArray + 2;

            *objectsCapacity = this.objectsCapacity;
            *sizesCapacity = this.sizesCapacity;
            *rootsCapacity = this.rootsCapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddObject(
            object @object,
            ref nint* objects,
            ref uint objectsCount)
        {
            Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectsCount++) = @object; // bypass range check
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddObjectAndEnsureCapacity(
            object @object,
            /*pinned*/ ref object[] objectsArray, 
            ref nint* objects,
            ref uint objectsCount, 
            ref uint objectsCapacity,
            /*pinned*/ ref uint[] sizesArray,
            ref uint* sizes,
            ref uint sizesCapacity)
        {
            Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectsCount++) = @object; // bypass range check
            if (objectsCount == objectsCapacity)
            {
                ReallocateObjects(ref objectsArray, ref objects, ref objectsCapacity);

                if (objectsCount == sizesCapacity)
                    ReallocateSizes(ref sizesArray, ref sizes, ref sizesCapacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRootAndEnsureCapacity(
            ulong root,
            /*pinned*/ ref ulong[] rootsArray,
            ref ulong* roots,
            ref uint rootsCount,
            ref uint rootsCapacity)
        {
            roots[rootsCount++] = root;
            if (rootsCount == objectsCapacity)
                ReallocateRoots(ref rootsArray, ref roots, ref rootsCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRoot(
            ulong root,
            ref ulong* roots,
            ref uint rootsCount)
        {
            roots[rootsCount++] = root;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureSizesCapacity(
            uint requiredSizesCount,
            /*pinned*/ ref uint[] sizesArray,
            ref uint* sizes,
            ref uint sizesCapacity)
        {
            if (requiredSizesCount < sizesCapacity)
                return;

            var allocationRation = 0;
            while (requiredSizesCount > (sizesCapacity << ++allocationRation));

            ReallocateSizes(ref sizesArray, ref sizes, ref sizesCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureObjectsCapacity(
            uint requiredObjectsCount,
            /*pinned*/ ref object[] objectsArray,
            ref nint* objects,
            ref uint objectsCapacity)
        {
            if (requiredObjectsCount < objectsCapacity)
                return;

            var allocationRation = 0;
            while (requiredObjectsCount > (objectsCapacity << ++allocationRation));

            ReallocateObjects(ref objectsArray, ref objects, ref objectsCapacity, allocationRation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureRootsCapacity(
            uint requiredRootsCount,
            /*pinned*/ ref ulong[] rootsArray,
            ref ulong* roots,
            ref uint rootsCapacity)
        {
            if (requiredRootsCount < rootsCapacity)
                return;

            var allocationRation = 0;
            while (requiredRootsCount > (rootsCapacity << ++allocationRation));

            ReallocateRoots(ref rootsArray, ref roots, ref rootsCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureMethodTablesCapacity(
            uint requiredMethodTablesCount,
            /*pinned*/ ref MethodTable*[] methodTablesArray,
            ref MethodTable** methodTables,
            ref uint methodTablesCapacity)
        {
            if (requiredMethodTablesCount < sizesCapacity)
                return;

            var allocationRation = 0;
            while (requiredMethodTablesCount > (sizesCapacity << ++allocationRation));

            ReallocateMethodTables(ref methodTablesArray, ref methodTables, ref methodTablesCapacity, allocationRation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ReallocateObjects(
            /*pinned*/ ref object[] objectsArray,
            ref nint* objects,
            ref uint objectsCapacity,
            int allocationRatio = 1)
        {
            Array.Resize(ref objectsArray, (int)(objectsCapacity <<= allocationRatio));
            objects = *(nint**)Unsafe.AsPointer(ref objectsArray) + 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ReallocateSizes(
            /*pinned*/ ref uint[] sizesArray,
            ref uint* sizes,
            ref uint sizesCapacity,
            int allocationRatio = 1)
        {
            Patcher.Pinnable(out uint[] newSizesArray);

            var sizesToCopy = sizesCapacity;
            newSizesArray = GC.AllocateUninitializedArray<uint>((int)(sizesCapacity <<= allocationRatio));
            var newSizes = *(uint**)&newSizesArray + 2;

            Unsafe.CopyBlock(newSizes, sizes, sizesToCopy * sizeof(uint));
            sizesArray = newSizesArray;
            sizes = newSizes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ReallocateRoots(
            /*pinned*/ ref ulong[] rootsArray,
            ref ulong* roots,
            ref uint rootsCapacity,
            int allocationRatio = 1)
        {
            Patcher.Pinnable(out ulong[] newRootsArray);

            var rootsToCopy = rootsCapacity;
            newRootsArray = GC.AllocateUninitializedArray<ulong>((int)(rootsCapacity <<= allocationRatio));
            var newRoots = *(ulong**)&newRootsArray + 2;

            Unsafe.CopyBlock(newRoots, roots, rootsToCopy * sizeof(ulong));
            rootsArray = newRootsArray;
            roots = newRoots;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ReallocateMethodTables(
            /*pinned*/ ref MethodTable*[] methodTablesArray,
            ref MethodTable** methodTables,
            ref uint methodTablesCapacity,
            int allocationRatio = 1)
        {
            Patcher.Pinnable(out object newMethodTables);

            var methodTablesToCopy = methodTablesCapacity;
            // MethodTable*[] is invalid generic variable, so bypass this restriction by manual changing the MethodTable of MethodTables array
            newMethodTables = GC.AllocateUninitializedArray<nint>((int)(methodTablesCapacity <<= allocationRatio));
            *(nint*)&newMethodTables = typeof(MethodTable*[]).TypeHandle.Value;
            var pnewMethodTables = *(MethodTable***)&newMethodTables + 2;

            Unsafe.CopyBlock(pnewMethodTables, methodTables, methodTablesToCopy * (uint)sizeof(MethodTable*));
            methodTablesArray = *(MethodTable*[]*)&newMethodTables;
            methodTables = pnewMethodTables;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitSerializationContext(
            nint* objects, 
            uint objectsCount, 
            object[] objectsArray,
            uint[] sizesArray, 
            ulong[] rootsArray)
        {
            // it is important to clear the buffer from references, otherwise it will cause problems with objects lifetime over time.
            // also, instead this approach can be used finalization, in which do the final cleaning, but this will not work if firstly
            // serialize a large array of objects, and then smaller ones
            Unsafe.InitBlock(objects, 0, objectsCount << 3);

            this.objectsArray = objectsArray;
            this.sizesArray = sizesArray;
            this.rootsArray = rootsArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitDeserializationContext(
            nint* objects, 
            uint objectsCount, 
            MethodTable** methodTables,
            uint methodTablesCount, 
            object[] objectsArray, 
            MethodTable*[] methodTablesArray, 
            ulong[] rootsArray)
        {
            /* supra */
            Unsafe.InitBlock(objects, 0, objectsCount << 3);
            // methodTable is nullable so it needs to be cleared
            Unsafe.InitBlock(methodTables, 0, methodTablesCount << 3);

            this.objectsArray = objectsArray;
            this.methodTablesArray = methodTablesArray;
            this.rootsArray = rootsArray;
        }

        [ThreadStatic]
        public static readonly SerializationContextBuffers ThreadLocal = new SerializationContextBuffers();
    }
}