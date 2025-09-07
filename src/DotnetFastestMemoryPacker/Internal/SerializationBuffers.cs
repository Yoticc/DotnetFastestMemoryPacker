using PatcherReference;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker.Internal;

// it has a similar idea to ArrayPool, but it is very specific. instead of using buckets, it uses one array that expands as needed.
// in the middle-execution all methods must be inlined, because ensuring that every method is inlined in its callers
// in the tier1 or full opts phase is important.

// i got tired of writing [ M e t h o d I m p l ( M e t h o d I m p l O p t i o n s . A g g r e s s i v e I n l i n i n g ) ] for every member of this class,
// so i implemented a separate attribute.
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

    /* nullable/serialization/deserialization */
    /*for debug*/public /* n s d */ object[] objectsArray;
    /*for debug*/public /*   s   */ uint[] sizesArray; // may has a bigger capacity than objects, but not a smaller one
    /*for debug*/public /* n   d */ nint[] methodTablesArray;
    /*for debug*/public /*   s d */ Vector128<ulong>[] rootsArray; // shared array is used instead of two separate ones for 64 and 128 bits roots

    // shared roots requires specific logic to ensure capacity
    // ensure roots128: count >= rootsCapacity
    // ensure roots64: count >= rootsCapacity << 1

    // the extra length is needed to safely use arrays for simd computations
    int GetActualArraySize(uint length) => (int)(length + 16);

    object[] AllocateObjects(uint length) => objectsArray = Allocator.AllocatePinnedArray<object>(GetActualArraySize(objectsCapacity = length));
    
    uint[] AllocateSizes(uint length) => sizesArray = Allocator.AllocatePinnedUninitializedArray<uint>(GetActualArraySize(sizesCapacity = length));
    
    nint[] AllocateMethodTables(uint length) => methodTablesArray = Allocator.AllocatePinnedArray<nint>(GetActualArraySize(methodTablesCapacity = length));
    
    Vector128<ulong>[] AllocateVectorRootsArray(uint length) => rootsArray = Allocator.AllocatePinnedUninitializedArray<Vector128<ulong>>(GetActualArraySize(rootsCapacity = length));

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

    public Vector128<ulong> GetRoot128(Vector128<ulong>* roots, uint index)
    {
        return roots[index];
    }

    public ulong GetRoot64(Vector128<ulong>* roots, uint index)
    {
        return ((ulong*)roots)[index];
    }

    public void AddRoot128(Vector128<ulong> root, Vector128<ulong>* roots, ref uint rootsCount)
    {
        Sse2.Store((ulong*)(roots + rootsCount++), root);
    }

    public void AddRoot64(ulong root, Vector128<ulong>* roots, ref uint rootsCount)
    {
        ((ulong*)roots)[rootsCount++] = root;
    }

    public void AddRoot64AndEnsureCapacity(ulong root, Vector128<ulong>** roots, ref uint count, ref uint capacity)
    {
        AddRoot64(root, *roots, ref count);
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
        Allocator.ResizePinnedArrayForGCElements(ref objectsArray, GetActualArraySize(objectsCapacity = capacity));
        *objects = GetArrayBody(objectsArray);
    }

    void ReallocateSizes(uint** sizes, uint capacity)
    {
        Allocator.ResizePinnedUninitializedArrayForNonGCElements(ref sizesArray, GetActualArraySize(sizesCapacity = capacity));
        *sizes = GetArrayBody(sizesArray);
    }

    void ReallocateMethodTables(MethodTable*** methodTables, uint capacity)
    {
        Allocator.ResizePinnedArrayForNonGCElements(ref methodTablesArray, GetActualArraySize(methodTablesCapacity = capacity));
        *methodTables = (MethodTable**)GetArrayBody(methodTablesArray);
    }

    void ReallocateRoots(Vector128<ulong>** roots, uint capacity)
    {
        Allocator.ResizePinnedUninitializedArrayForNonGCElements(ref rootsArray, GetActualArraySize(rootsCapacity = capacity));
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
        Unsafe.InitBlock(objects, 0, objectsCount << 3);
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