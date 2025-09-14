using PatcherReference;
using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker.Internal;

// it has a similar idea to ArrayPool, but it is very specific. instead of using buckets, it uses one array that expands as needed.
// in the middle-execution all methods must be inlined, because ensuring that every method is inlined in its callers
// in the tier1 or full opts phase is important.
[InlineAllMembers]
unsafe class SerializationBuffers
{
    const int InitialObjectsCapacity = 2 << 10;
    const int InitialSizesCapacity = 2 << 10;
    const int InitialMethodTableCapacity = 2 << 10;
    const int InitialVectorRootsCapacity = 2 << 11;

    SerializationBuffers()
    {
        AllocateObjects(InitialObjectsCapacity);
        AllocateSizes(InitialSizesCapacity);
        AllocateMethodTables(InitialMethodTableCapacity);
        AllocateVectorRootsArray(InitialVectorRootsCapacity);
    }

    uint methodTablesCapacity;
    uint objectsCapacity;
    uint sizesCapacity;
    uint rootsCapacity;

    /* aligned/unitialized/nullable/serialization/deserialization */
    /*for debug*/public /* a   n s d */ object[] objectsArray;
    /*for debug*/public /*       s   */ uint[] sizesArray; // may has a bigger capacity than objects, but not a smaller one
    /*for debug*/public /* a   n   d */ nint[] methodTablesArray;
    /*for debug*/public /* a     s d */ Vector128<ulong>[] rootsArray; // shared array is used instead of two separate ones for 64 and 128 bits roots

    // shared roots requires specific logic to ensure capacity
    // ensure roots128: count >= rootsCapacity
    // ensure roots64: count >= rootsCapacity << 1
    // ensure roots32: count >= rootsCapacity << 2

    // the extra length is needed to safely use arrays for simd computations
    int GetActualArraySize(uint length) => (int)(length + 32);

    object[] AllocateObjects(uint length) => objectsArray = ObjectAllocator.AllocatePinnedArray<object>(GetActualArraySize(objectsCapacity = length));
    
    uint[] AllocateSizes(uint length) => sizesArray = ObjectAllocator.AllocatePinnedUninitializedArray<uint>(GetActualArraySize(sizesCapacity = length));
    
    nint[] AllocateMethodTables(uint length) => methodTablesArray = ObjectAllocator.AllocatePinnedArray<nint>(GetActualArraySize(methodTablesCapacity = length));
    
    Vector128<ulong>[] AllocateVectorRootsArray(uint length) => rootsArray = ObjectAllocator.AllocatePinnedUninitializedArray<Vector128<ulong>>(GetActualArraySize(rootsCapacity = length));

    public object GetObject(object* objects, uint index)
    {
        return Unsafe.AsRef<object>(objects + index);
    }

    public void SetObject(/*pinned*/ object @object, object* objects, uint index)
    {
        Unsafe.AsRef<object>(objects + index) = @object;
    }

    public void AddObject(/*pinned*/ object @object, object* objects, ref uint objectsCount)
    {
        SetObject(@object, objects, objectsCount++);
    }

    public void AddObjectAndEnsureCapacity(
        object @object,
        object** objects, ref uint objectsCount, ref uint objectsCapacity,
        uint** sizes, ref uint sizesCapacity)
    {
        AddObject(@object, *objects, ref objectsCount);
        if (objectsCount == objectsCapacity)
        {
            ReallocateObjects(objects, objectsCapacity <<= 1);

            if (objectsCount == sizesCapacity)
                ReallocateSizes(sizes, sizesCapacity <<= 1);
        }
    }

    public void GetRoot128(out uint objectIndex, out uint referenceOffset, out uint referenceIndex, Vector128<ulong>* roots, uint rootIndex)
    {
        var root = Sse2.LoadVector128((ulong*)(roots + rootIndex));

        var objectIndexAndReferenceOffset = root.GetElement(0);

        objectIndex = (uint)(objectIndexAndReferenceOffset & ~0u);
        referenceOffset = (uint)(objectIndexAndReferenceOffset >> 32) + SizeOf.MethodTable;
        referenceIndex = root.As<ulong, uint>().GetElement(2);
    }

    public void GetRoot64(out uint offset, out uint index, Vector128<ulong>* roots, uint rootIndex)
    {
        var root = ((ulong*)roots)[rootIndex];
        offset = (uint)(root >> 32);
        index = (uint)(root & ~0U);
    }

    public void AddRoot128(uint objectIndex, uint referenceOffset, uint referenceIndex, Vector128<ulong>* roots, ref uint rootsCount)
    {
        var root = Vector128.CreateScalarUnsafe(objectIndex | (ulong)referenceOffset << 32);
        root = Sse41.Insert(root.As<ulong, uint>(), referenceIndex, 2).As<uint, ulong>();
        Sse2.Store((ulong*)(roots + rootsCount++), root);
    }
    
    public void AddRoot64(uint offset, uint index, Vector128<ulong>* roots, ref uint rootsCount)
    {
        ((ulong*)roots)[rootsCount++] = (ulong)offset << 32 | index;
    }

    public void AddRoot64AndEnsureCapacity(uint offset, uint index, Vector128<ulong>** roots, ref uint count, ref uint capacity)
    {
        AddRoot64(offset, index, *roots, ref count);
        if (count == rootsCapacity << 1)
            ReallocateRoots(roots, capacity <<= 1);
    }

    public void EnsureObjectsCapacity(uint requiredCount, object** objects, ref uint capacity)
    {
        if (requiredCount >= capacity)
            ReallocateObjects(objects, capacity += requiredCount);
    }

    public void EnsureSizesCapacity(uint requiredCount, uint** sizes, ref uint capacity)
    {
        if (requiredCount >= capacity)
            ReallocateSizes(sizes, capacity += requiredCount);
    }

    public void EnsureRoots128Capacity(uint requiredCount, Vector128<ulong>** roots, ref uint capacity)
    {
        if (requiredCount >= capacity)
            ReallocateRoots(roots, capacity += requiredCount);
    }

    public void EnsureRoots64Capacity(uint requiredCount, Vector128<ulong>** roots, ref uint capacity)
    {
        if (requiredCount >= capacity << 1)
            ReallocateRoots(roots, capacity += requiredCount);
    }

    public void EnsureMethodTablesCapacity(uint requiredCount, MethodTable*** methodTables, ref uint capacity)
    {
        if (requiredCount >= capacity)
            ReallocateMethodTables(methodTables, capacity += requiredCount);
    }

    void ReallocateObjects(object** objects, uint capacity)
    {
        ObjectAllocator.ResizePinnedArrayForGCElements(ref objectsArray, GetActualArraySize(objectsCapacity = capacity));
        *objects = GetArrayBody(objectsArray);
    }

    void ReallocateSizes(uint** sizes, uint capacity)
    {
        ObjectAllocator.ResizePinnedUninitializedArrayForNonGCElements(ref sizesArray, GetActualArraySize(sizesCapacity = capacity));
        *sizes = GetArrayBody(sizesArray);
    }

    void ReallocateMethodTables(MethodTable*** methodTables, uint capacity)
    {
        ObjectAllocator.ResizePinnedArrayForNonGCElements(ref methodTablesArray, GetActualArraySize(methodTablesCapacity = capacity));
        *methodTables = (MethodTable**)GetArrayBody(methodTablesArray);
    }

    void ReallocateRoots(Vector128<ulong>** roots, uint capacity)
    {
        ObjectAllocator.ResizePinnedUninitializedArrayForNonGCElements(ref rootsArray, GetActualArraySize(rootsCapacity = capacity));
        *roots = GetArrayBody(rootsArray);
    }

    public void EnterObjectsContext(object** objects, uint requiredCount)
    {
        var capacity = objectsCapacity;
        if (requiredCount >= capacity)
            ReallocateObjects(objects, capacity + requiredCount);
        else *objects = GetArrayBody(objectsArray);
    }

    public void EnterObjectsContext(object** objects, uint* capacity)
    {
        *objects = GetArrayBody(objectsArray);
        *capacity = objectsCapacity;
    }

    public void EnterSizesContext(uint** sizes, uint* capacity)
    {
        *sizes = GetArrayBody(sizesArray);
        *capacity = sizesCapacity;
    }

    public void EnterMethodTablesContext(MethodTable*** methodTables, uint requiredCount)
    {
        var capacity = methodTablesCapacity;
        if (requiredCount >= capacity)
            ReallocateMethodTables(methodTables, capacity + requiredCount);
        else *methodTables = (MethodTable**)GetArrayBody(methodTablesArray);
    }

    public void EnterMethodTablesContext(MethodTable*** methodTables, uint* capacity)
    {
        *methodTables = (MethodTable**)GetArrayBody(methodTablesArray);
        *capacity = methodTablesCapacity;
    }

    public void EnterRootsContext(Vector128<ulong>** roots, uint* capacity)
    {
        *roots = GetArrayBody(rootsArray);
        *capacity = rootsCapacity;
    }

    public void EnterRootsContext(Vector128<ulong>** roots, uint requiredCount)
    {
        var rootsCapacity = this.rootsCapacity;
        if (requiredCount >= rootsCapacity)
            ReallocateRoots(roots, rootsCapacity + requiredCount);
        else *roots = GetArrayBody(rootsArray);
    }
    
    public void ExitObjectsContext(object* objects, uint objectsCount)
    {
        // it is important to clear the buffer from left references, otherwise it will cause problems with the objects lifetime over time.
        // also, instead this approach can be used finalization, in which do the final cleaning, but this will not work if firstly
        // serialize a large array of objects, and then smaller ones

        if (FastestMemoryPacker.Advanced.AutoClearObjectCache)
            Unsafe.InitBlock(objects, 0, objectsCount << 3);
    }

    public void ClearObjectsContext()
    {
        var array = objectsArray;
        var length = array.Length;

        var pointer = GetObjectBody(array);
        Unsafe.InitBlock(pointer, 0, (uint)length << 3);
    }

    public void ExitMethodTablesContext(MethodTable** methodTables, uint methodTablesCount)
    {
        // methodTable is nullable so it needs to be cleared
        Unsafe.InitBlock(methodTables, 0, methodTablesCount << 3);
    }

    [ThreadStatic]
    static SerializationBuffers threadLocal;

    public static SerializationBuffers ThreadLocal => threadLocal ?? (threadLocal = new SerializationBuffers());
}