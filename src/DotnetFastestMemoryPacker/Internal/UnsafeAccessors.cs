using PatcherReference;
using System.Runtime.CompilerServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker.Internal;

static unsafe class UnsafeAccessors
{
    public static string AllocateUninitializedString(uint length)
    {
        return FastAllocateString(GetMethodTable<string>(), length);
    }

    [UnsafeAccess("System.String")]
    static extern string FastAllocateString(MethodTable* methodTable, uint length);

    public static void AllocateUninitializedObject(MethodTable* methodTable, ref object @object)
    {
        @object = InternalAllocNoChecks_FastPath(methodTable);
        if (@object is null)
            InternalAllocNoChecks(methodTable, ref @object);
    }

    [UnsafeAccess(
        DeclaringTypeFullName: "System.RuntimeTypeHandle",
        MethodSignature: "System.Void System.RuntimeTypeHandle::InternalAllocNoChecks(System.Runtime.CompilerServices.MethodTable*,System.Runtime.CompilerServices.ObjectHandleOnStack)"
    )]

    static extern void InternalAllocNoChecks(MethodTable* methodTable, ref object @object);

    [UnsafeAccess("System.RuntimeTypeHandle")]
    static extern object InternalAllocNoChecks_FastPath(MethodTable* methodTable);

    public static void AllocateArray(MethodTable* methodTable, uint rank, uint* lengths, uint* lowerBounds, ref object array)
    {
        var runtimeType = methodTable->GetRuntimeType();

        var qcallType = stackalloc void*[] { &runtimeType, methodTable };
        var parray = Unsafe.AsPointer(ref array);

        InternalCreate(*(Span<int>*)qcallType, rank, lengths, lowerBounds, isFromArrayType: 1, *(Span<int>*)&parray);
    }

    [UnsafeAccess("System.Array", MethodName: "<InternalCreate>g____PInvoke|0_0")]
    static extern void InternalCreate(Span<int> qcallType, uint rank, uint* pLengths, uint* pLowerBounds, int isFromArrayType, Span<int> stackObjectResult);

    public static void AllocateArray(MethodTable* methodTable, uint length, uint gcFlags, ref object array)
    {
        AllocateNewArray(methodTable, length, gcFlags, ref @array);
    }

    [UnsafeAccess("System.GC")]
    static extern void AllocateNewArray(MethodTable* methodTable, uint length, uint flags, ref object array);
}

enum GCAllocFlags : uint
{
    NoFlags = 0x00,
    ZeroingOptional = 0x10,
    PinnedObjectHeap = 0x40
}