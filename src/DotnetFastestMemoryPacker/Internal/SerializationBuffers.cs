using PatcherReference;
using System.Runtime.CompilerServices;

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
    const int InitialObjectsCapacity = 2 << 11;
    const int InitialSizesCapacity = 2 << 11;
    const int InitialMethodTableCapacity = 2 << 11;
    const int InitialRootsCapacity = 2 << 12;
    const int InitialVectorRootsCapacity = 2 << 12;

    SerializationBuffers()
    {
        AllocateObjects(InitialObjectsCapacity);
        AllocateSizes(InitialSizesCapacity);
        AllocateMethodTables(InitialMethodTableCapacity);
        AllocateRootsArray(InitialRootsCapacity);
        AllocateVectorRootsArray(InitialVectorRootsCapacity);
    }

    uint methodTablesCapacity;
    uint objectsCapacity;
    uint sizesCapacity;
    uint rootsCapacity;
    uint vrootsCapacity;

    /* nullable/serialization/deserialization */
    /* n s d */ object[] objectsArray;
    /*   s   */ uint[] sizesArray; // can has bigger capacity than objects, but not less
    /* n   d */ nint[] methodTablesArray;
    /*   s d */ ulong[] rootsArray;
    /*     d */ ulong[] vrootsArray;

    // the extra length is needed to safely use vector computations on such arrays
    int GetObjectsActualSize(uint length) => (int)length;
    int GetSizesActualSize(uint length) => (int)(length + 8);
    int GetMethodTablesActualSize(uint length) => (int)(length + 8);
    int GetRootsActualSize(uint length) => (int)(length + 8);
    int GetVectorRootsActualSize(uint length) => (int)(length + 8 << 1);

    object[] AllocateObjects(uint length) => objectsArray = Allocator.AllocatePinnedArray<object>(GetObjectsActualSize(methodTablesCapacity = length));
    uint[] AllocateSizes(uint length) => sizesArray = Allocator.AllocatePinnedUninitializedArray<uint>(GetSizesActualSize(sizesCapacity = length));
    nint[] AllocateMethodTables(uint length) => methodTablesArray = Allocator.AllocatePinnedArray<nint>(GetMethodTablesActualSize(methodTablesCapacity = length));
    ulong[] AllocateRootsArray(uint length) => rootsArray = Allocator.AllocatePinnedUninitializedArray<ulong>(GetRootsActualSize(rootsCapacity = length));
    ulong[] AllocateVectorRootsArray(uint length) => vrootsArray = Allocator.AllocatePinnedUninitializedArray<ulong>(GetVectorRootsActualSize(vrootsCapacity = length));

    public void AddObject(object @object, nint* objects, ref uint objectsCount)
    {
        Unsafe.Add(ref Unsafe.AsRef<object>(objects), objectsCount++) = @object; // bypass range check
    }

    public void AddObjectAndEnsureCapacity(
        object @object,
        ref nint* objects, ref uint objectsCount, ref uint objectsCapacity,
        ref uint* sizes, ref uint sizesCapacity)
    {
        AddObject(@object, objects, ref objectsCount);
        if (objectsCount == objectsCapacity)
        {
            ReallocateObjects(ref objects, ref objectsCapacity);

            if (objectsCount == sizesCapacity)
                ReallocateSizes(ref sizes, ref sizesCapacity);
        }
    }

    public void AddRoot(ulong root, ulong* roots, ref uint rootsCount)
    {
        roots[rootsCount++] = root;
    }

    public void AddRootAndEnsureCapacity(ulong root, ref ulong* roots, ref uint rootsCount, ref uint rootsCapacity)
    {
        AddRoot(root, roots, ref rootsCount);
        if (rootsCount == objectsCapacity)
            ReallocateRoots(ref roots, ref rootsCapacity);
    }

    static int CalculateAllocationRate(uint requiredCount, uint capacity)
    {
        var allocationRation = 0;
        while (requiredCount > (capacity << ++allocationRation));
        return allocationRation;
    }

    public void EnsureObjectsCapacity(uint requiredCount, ref nint* objects, ref uint capacity)
    {
        if (requiredCount >= capacity)
            ReallocateObjects(ref objects, ref capacity, CalculateAllocationRate(requiredCount, capacity));
    }

    public void EnsureSizesCapacity(uint requiredCount, ref uint* sizes, ref uint capacity)
    {
        if (requiredCount >= capacity)
            ReallocateSizes(ref sizes, ref capacity, CalculateAllocationRate(requiredCount, capacity));
    }

    public void EnsureRootsCapacity(uint requiredCount, ref ulong* roots, ref uint capacity)
    {
        if (requiredCount >= capacity)
            ReallocateRoots(ref roots, ref capacity, CalculateAllocationRate(requiredCount, capacity));
    }

    public void EnsureMethodTablesCapacity(uint requiredCount, ref MethodTable** methodTables, ref uint capacity)
    {
        if (requiredCount >= capacity)
            ReallocateMethodTables(ref methodTables, ref capacity, CalculateAllocationRate(requiredCount, capacity));
    }

    void ReallocateObjects(nint** objects, ref uint capacity, int allocationRatio = 1)
    {
        Allocator.ResizePinnedArrayForGCElements(ref objectsArray, GetObjectsActualSize(objectsCapacity = capacity <<= allocationRatio));
        *objects = (nint*)GetArrayPointer(objectsArray);
    }

    void ReallocateObjects(ref nint* objects, ref uint capacity, int allocationRatio = 1)
    {
        Allocator.ResizePinnedArrayForGCElements(ref objectsArray, GetObjectsActualSize(objectsCapacity = capacity <<= allocationRatio));
        objects = (nint*)GetArrayPointer(objectsArray);
    }

    void ReallocateSizes(ref uint* sizes, ref uint capacity, int allocationRatio = 1)
    {
        Allocator.ResizePinnedUninitializedArrayForNonGCElements(ref sizesArray, GetSizesActualSize(sizesCapacity = capacity <<= allocationRatio));
        sizes = GetArrayPointer(sizesArray);
    }

    void ReallocateMethodTables(MethodTable*** methodTables, ref uint capacity, int allocationRatio = 1)
    {
        Allocator.ResizePinnedArrayForNonGCElements(ref methodTablesArray, GetMethodTablesActualSize(methodTablesCapacity = capacity <<= allocationRatio));
        *methodTables = (MethodTable**)GetArrayPointer(methodTablesArray);
    }

    void ReallocateMethodTables(ref MethodTable** methodTables, ref uint capacity, int allocationRatio = 1)
    {
        Allocator.ResizePinnedArrayForNonGCElements(ref methodTablesArray, GetMethodTablesActualSize(methodTablesCapacity = capacity <<= allocationRatio));
        methodTables = (MethodTable**)GetArrayPointer(methodTablesArray);
    }

    void ReallocateRoots(ulong** roots, ref uint capacity, int allocationRatio = 1)
    {
        Allocator.ResizePinnedUninitializedArrayForNonGCElements(ref rootsArray, GetRootsActualSize(rootsCapacity = capacity <<= allocationRatio));
        *roots = GetArrayPointer(rootsArray);
    }

    void ReallocateRoots(ref ulong* roots, ref uint capacity, int allocationRatio = 1)
    {
        Allocator.ResizePinnedUninitializedArrayForNonGCElements(ref rootsArray, GetRootsActualSize(rootsCapacity = capacity <<= allocationRatio));
        roots = GetArrayPointer(rootsArray);
    }

    void ReallocateVectorRoots(ref ulong* vroots, ref uint capacity, int allocationRatio = 1)
    {
        Allocator.ResizePinnedUninitializedArrayForNonGCElements(ref vrootsArray, GetVectorRootsActualSize(vrootsCapacity = capacity <<= allocationRatio));
        vroots = GetArrayPointer(vrootsArray);
    }

    public void EnterObjectsContext(nint** objects, uint requiredCount)
    {
        var capacity = objectsCapacity;
        if (requiredCount >= capacity)
            ReallocateObjects(objects, ref capacity, CalculateAllocationRate(requiredCount, capacity));
        else *objects = (nint*)GetArrayPointer(objectsArray);
    }

    public void EnterObjectsContext(nint** objects, uint* capacity)
    {
        *objects = (nint*)GetArrayPointer(objectsArray);
        *capacity = objectsCapacity;
    }

    public void EnterSizesContext(uint** sizes, uint* capacity)
    {
        *sizes = GetArrayPointer(sizesArray);
        *capacity = sizesCapacity;
    }

    public void EnterMethodTablesContext(MethodTable*** methodTables, uint requiredCount)
    {
        var capacity = methodTablesCapacity;
        if (requiredCount >= capacity)
            ReallocateMethodTables(methodTables, ref capacity, CalculateAllocationRate(requiredCount, capacity));
        else *methodTables = (MethodTable**)GetArrayPointer(methodTablesArray);
    }

    public void EnterMethodTablesContext(MethodTable*** methodTables, uint* capacity)
    {
        *methodTables = (MethodTable**)GetArrayPointer(methodTablesArray);
        *capacity = methodTablesCapacity;
    }

    public void EnterRootsContext(ulong** roots, uint requiredCount)
    {
        var capacity = rootsCapacity;
        if (requiredCount >= capacity)
            ReallocateRoots(roots, ref capacity, CalculateAllocationRate(requiredCount, capacity));
        else *roots = GetArrayPointer(rootsArray);
    }

    public void EnterRootsContext(ulong** roots, uint* capacity)
    {
        *roots = GetArrayPointer(rootsArray);
        *capacity = rootsCapacity;
    }

    public void EnterVectorRootsContext(ulong** vroots, uint* capacity)
    {
        *vroots = GetArrayPointer(vrootsArray);
        *capacity = this.vrootsCapacity;
    }

    public void ExitObjectsContext(nint* objects, uint objectsCount)
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
    public static readonly SerializationBuffers ThreadLocal = new SerializationBuffers();
}