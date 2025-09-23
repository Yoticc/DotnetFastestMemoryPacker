using DotnetFastestMemoryPacker.Internal;
using PatcherReference;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

unsafe class TypeCache
{
    const uint EntryCount = 1 << 14;

    public static readonly Vector256<ulong>* Entries = (Vector256<ulong>*)NativeMemory.AlignedAlloc(TypeEntry.ByteSize * EntryCount, TypeEntry.ByteSize);

    // the difference in addresses between the allocated method tables is approx 0x1C0, this is a 9 bit mask
    [Inline]
    static ulong GetEntryIndex(MethodTable* methodTable) => ((ulong)methodTable >> 9) & (EntryCount - 1);

    [Inline]
    static Vector256<ulong>* GetApproximateEntryPointer(MethodTable* methodTable) => Entries + GetEntryIndex(methodTable);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<ulong>* GetOrInitializeEntry_SlowPath(MethodTable* methodTable)
    {
        var approxEntryPointer = GetApproximateEntryPointer(methodTable);

        var approxMethodTable = TypeEntry.Lower.GetMethodTable(approxEntryPointer);
        if (approxMethodTable == methodTable)
            return approxEntryPointer;

        if (approxMethodTable is null)
        {
            InitializeEntry(approxEntryPointer, methodTable);
            return approxEntryPointer;
        }
        else
        {
            do
            {
                approxEntryPointer = TypeEntry.Upper.GetNextEntryPointer(approxEntryPointer);
                approxMethodTable = TypeEntry.Lower.GetMethodTable(approxEntryPointer);

                if (approxMethodTable == methodTable)
                    return approxEntryPointer;

                if (approxMethodTable is not null)
                    continue;

                var pentry = TypeEntry.Allocate();
                InitializeEntry(pentry, methodTable);
                return pentry;
            } while (true);
        }        
    }

    [Inline]
    public static void ExtractEntry(MethodTable* methodTable, Vector256<ulong> ymm0, Vector128<ulong> xmm0)
    {
        var approxEntryPointer = GetApproximateEntryPointer(methodTable);
        ymm0 = TypeEntry.FromPointer(approxEntryPointer);
        xmm0 = TypeEntry.Lower.Get(ymm0);

        var approxMethodTable = TypeEntry.Lower.GetMethodTable(xmm0);
        if (approxMethodTable != methodTable)
        {
            if (approxMethodTable is null)
            {
                InitializeEntry(approxEntryPointer, methodTable);
                ymm0 = TypeEntry.FromPointer(approxEntryPointer);
                xmm0 = TypeEntry.Lower.Get(ymm0);
            }
            else
            {
                do
                {
                    xmm0 = TypeEntry.Upper.Get(ymm0);
                    approxEntryPointer = TypeEntry.Upper.GetNextEntryPointer(xmm0);
                    ymm0 = TypeEntry.FromPointer(approxEntryPointer);
                    xmm0 = TypeEntry.Lower.Get(ymm0);
                    approxMethodTable = TypeEntry.Lower.GetMethodTable(xmm0);

                    if (approxMethodTable == methodTable)
                        return;

                    if (approxMethodTable is not null)
                        continue;

                    var entryPointer = TypeEntry.Allocate();
                    InitializeEntry(entryPointer, methodTable);
                    return;
                } while (true);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void InitializeEntry(Vector256<ulong>* pentry, MethodTable* methodTable)
    {
        var eeClass = methodTable->Class;
        TypeEntry.SetMethodTable(pentry, methodTable);

        var typeKind = GetTypeKind(methodTable);
        TypeEntry.SetTypeKind(pentry, typeKind);

        if (typeKind is TypeKind.ValueType or TypeKind.ValueTypeWithGCPointers or TypeKind.Object or TypeKind.ObjectWithGCPointers)
        {
            var objectSize = (ushort)(methodTable->BaseSize - eeClass->BaseSizePadding);
            TypeEntry.SetObjectSize(pentry, objectSize);

            if (typeKind is TypeKind.ValueTypeWithGCPointers or TypeKind.ObjectWithGCPointers)
            {
                InitializeEntryFields(pentry, methodTable, eeClass);
            }
        }
        else
        {
            if (typeKind is TypeKind.OneDimArray or TypeKind.MultiDimArray)
            {
                var componentSize = methodTable->ComponentSize;
                TypeEntry.SetComponentSize(pentry, componentSize);
            }

            if (typeKind is TypeKind.MultiDimArray or TypeKind.MultiDimArrayWithGCPointers)
            {
                var arrayRank = (ushort)methodTable->MultiDimensionalArrayRank;
                TypeEntry.SetArrayRank(pentry, arrayRank);
            }
        }

        TypeEntry.SetNextEntry(pentry, null);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void InitializeEntryFields(Vector256<ulong>* pentry, MethodTable* methodTable, EEClass* eeClass)
    {
        var totalFieldCount = CountFields(methodTable, eeClass);
        var fields = FieldEntry.AllocateCollection(totalFieldCount);
        TypeEntry.SetFields(pentry, fields);

        CacheFields(methodTable, eeClass, &fields);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CacheFields(MethodTable* methodTable, EEClass* eeClass, Vector128<ulong>** pfield)
    {
        var fieldsCount = eeClass->NumInstanceFields;
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

                var fieldType = fieldDesc->Type;
                if (fieldType is CorElementType.Class)
                {
                    var field = *pfield;
                    var fieldMethodTable = GetFieldMethodTable(methodTable, fieldDesc);
                    var cachedTypeEntry = GetOrInitializeEntry_SlowPath(fieldMethodTable);
                    FieldEntry.SetTypeCache(field, cachedTypeEntry);

                    FieldEntry.SetOffset(field, (ushort)fieldDesc->Offset);
                    FieldEntry.SetIsLast(field, false);
                }
                else if (fieldType is CorElementType.ValueType)
                {
                    var fieldHandle = RuntimeFieldHandle.FromIntPtr((nint)fieldDesc);
                    var declaringTypeHandle = RuntimeTypeHandle.FromIntPtr((nint)methodTable);
                    var field = FieldInfo.GetFieldFromHandle(fieldHandle, declaringTypeHandle);

                    var fieldMethodTable = (MethodTable*)field.FieldType.TypeHandle.Value;
                    if (fieldMethodTable->ContainsGCPointers)
                    {
                        var fieldEEClass = fieldMethodTable->Class;
                        CacheFields(fieldMethodTable, fieldEEClass, pfield);
                    }
                }
            }

            methodTable = parentMethodTable;
            eeClass = parentClass;
        }

        var lastField = *pfield - 1;
        FieldEntry.SetIsLast(lastField, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CountFields(MethodTable* methodTable, EEClass* eeClass)
    {
        var totalFieldCount = 0;

        var fieldsCount = eeClass->NumInstanceFields;
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

                var fieldType = fieldDesc->Type;
                if (fieldType is CorElementType.Class)
                {
                    totalFieldCount++;
                }
                else if (fieldType is CorElementType.ValueType)
                {
                    var fieldMethodTable = GetFieldMethodTable(methodTable, fieldDesc);
                    if (fieldMethodTable->ContainsGCPointers)
                    {
                        var fieldEEClass = fieldMethodTable->Class;
                        totalFieldCount += CountFields(fieldMethodTable, fieldEEClass);
                    }
                }
            }

            methodTable = parentMethodTable;
            eeClass = parentClass;
        }

        return totalFieldCount;
    }

    // 21 lines of code saved and 6 launch attempts were wasted before i realized where i had missed a space.
    [Inline]
    static TypeKind GetTypeKind(MethodTable* methodTable) =>
        methodTable->IsValueType
        ? methodTable->ContainsGCPointers
          ? TypeKind.ValueTypeWithGCPointers
          : TypeKind.ValueType
        : methodTable->HasComponentSize
          ? methodTable->IsArray
            ? methodTable->MultiDimensionalArrayRank == 1
              ? methodTable->ContainsGCPointers
                ? TypeKind.OneDimArrayWithGCPointers
                : TypeKind.OneDimArray
              : methodTable->ContainsGCPointers
                ? TypeKind.MultiDimArrayWithGCPointers
                : TypeKind.MultiDimArray
            : TypeKind.String
          : methodTable->ContainsGCPointers
            ? TypeKind.ObjectWithGCPointers
            : TypeKind.Object;

    // obtains method table taking into account the declaring type instantiation
    [MethodImpl(MethodImplOptions.NoInlining)]
    static MethodTable* GetFieldMethodTable(MethodTable* declaringMethodTable, FieldDesc* fieldDesc)
    {
        var fieldHandle = RuntimeFieldHandle.FromIntPtr((nint)fieldDesc);
        var declaringTypeHandle = RuntimeTypeHandle.FromIntPtr((nint)declaringMethodTable);

        var field = FieldInfo.GetFieldFromHandle(fieldHandle, declaringTypeHandle);
        var fieldMethodTable = (MethodTable*)field.FieldType.TypeHandle.Value;

        return fieldMethodTable;
    }

    public static class TypeEntry
    {
        public const int ByteSize = 32;

        public static class Lower
        {
            [Inline] public static Vector128<ulong> Get(Vector256<ulong> ymm0) => ymm0.GetLower();

            [Inline] public static MethodTable* GetMethodTable(Vector128<ulong> xmm0) => (MethodTable*)xmm0.GetElement(0);

            [Inline] public static MethodTable* GetMethodTable(Vector256<ulong>* pentry) => *(MethodTable**)((byte*)pentry + 0);
        }

        public static class Upper
        {
            [Inline] public static Vector128<ulong> Get(Vector256<ulong> ymm0) => ymm0.GetUpper();

            [Inline] public static Vector256<ulong>* GetNextEntryPointer(Vector128<ulong> xmm0) => (Vector256<ulong>*)xmm0.GetElement(1);

            [Inline] public static Vector256<ulong>* GetNextEntryPointer(Vector256<ulong>* pentry) => *(Vector256<ulong>**)((byte*)pentry + 24);
        }

        // obtaining
        [Inline] public static Vector256<ulong>* Allocate() => (Vector256<ulong>*)NativeMemory.AlignedAlloc(ByteSize, ByteSize);

        [Inline] public static Vector256<ulong> FromPointer(Vector256<ulong>* pointer) => Vector256.LoadAligned((ulong*)pointer);

        // initialization
        [Inline] public static void SetMethodTable(void* pentry, MethodTable* methodTable) => *(MethodTable**)((byte*)pentry + 0) = methodTable;

        [Inline] public static void SetTypeKind(void* pentry, TypeKind typeKind) => *(TypeKind*)((byte*)pentry + 8) = typeKind;

        [Inline] public static void SetObjectSize(void* pentry, ushort objectSize) => *(ushort*)((byte*)pentry + 9) = objectSize;

        [Inline] public static void SetComponentSize(void* pentry, ushort componentSize) => *(ushort*)((byte*)pentry + 9) = componentSize;

        [Inline] public static void SetFieldCount(void* pentry, ushort fieldCount) => *(ushort*)((byte*)pentry + 11) = fieldCount;

        [Inline] public static void SetArrayRank(void* pentry, ushort rank) => *(ushort*)((byte*)pentry + 11) = rank;

        [Inline] public static void SetFields(void* pentry, void* fields) => *(void**)((byte*)pentry + 16) = fields;

        [Inline] public static void SetNextEntry(void* pentry, Vector256<ulong>* nextEntry) => *(Vector256<ulong>**)((byte*)pentry + 16) = nextEntry;
    }

    // type cache field structure:
    // [                 v16                 ]
    // [    u8    ][  u2  ][   u1  ][   u5   ]
    // [type cache][offset][is last]----------
    // |0          |8      |10     
    public static class FieldEntry
    {
        public const int ByteSize = 16;

        // obtaining
        [Inline] 
        public static Vector128<ulong>* AllocateCollection(int count)
        {
            var bytesToAllocate = count * ByteSize;
            if ((bytesToAllocate & 31) != 0)
                bytesToAllocate += 32 - (bytesToAllocate & 31);

            var allocatedMemory = NativeMemory.AlignedAlloc((nuint)bytesToAllocate, 32);
            return (Vector128<ulong>*)allocatedMemory;
        }

        [Inline] public static Vector128<ulong> FromPointer(Vector128<ulong>* pointer) => Vector128.LoadAligned((ulong*)pointer);

        // initialization
        [Inline] public static void SetTypeCache(void* pentry, Vector256<ulong>* cachedType) => *(void**)((byte*)pentry + 0) = cachedType;

        [Inline] public static void SetOffset(void* pentry, ushort offset) => *(ushort*)((byte*)pentry + 8) = offset;

        [Inline] public static void SetIsLast(void* pentry, bool isLast) => *(bool*)((byte*)pentry + 10) = isLast;
    }
}
                                // type cache entry structure
                                //
                                // [                                      v32                                         ]
enum TypeKind : byte            // [                              v16                             ][        v16       ]
{                               // [     u8     ][    u1   ][      u2      ][     u2    ][   u3   ][  u8  ][    u8    ]
    ValueType,                  // [method table][type kind][     size     ]-------------------------------[next entry]
    ValueTypeWithGCPointers,    // [method table][type kind][     size     ][field count]----------[fields][next entry]
    Object,                     // [method table][type kind][     size     ]-------------------------------[next entry]
    ObjectWithGCPointers,       // [method table][type kind][     size     ][field count]----------[fields][next entry]
    String,                     // [method table][type kind]-----------------------------------------------[next entry]
    OneDimArray,                // [method table][type kind][component size]-------------------------------[next entry]
    OneDimArrayWithGCPointers,  // [method table][type kind]-----------------------------------------------[next entry]
    MultiDimArray,              // [method table][type kind][component size][    rank   ]------------------[next entry]
    MultiDimArrayWithGCPointers // [method table][type kind]----------------[    rank   ]------------------[next entry]
}                               // |0            |8         |9              |11         |13        |16     |24