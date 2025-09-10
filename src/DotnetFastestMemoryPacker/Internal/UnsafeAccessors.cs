using PatcherReference;
using System.Runtime.CompilerServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker.Internal;

static unsafe class UnsafeAccessors
{
    public static string AllocateUninitializedString(uint length)
    {
#if NET10_0_OR_GREATER
        return FastAllocateString(GetMethodTable<string>(), length);
#elif NET8_0_OR_GREATER
    return FastAllocateString(length);
#endif
    }

#if NET10_0_OR_GREATER
    [UnsafeAccess("System.String")]
    unsafe static extern string FastAllocateString(MethodTable* methodTable, uint length);
#elif NET8_0_OR_GREATER
    [UnsafeAccess("System.String")]
    unsafe static extern string FastAllocateString(uint length);
#endif

    /********************************************************************************************/

    public static void AllocateUninitializedObject(MethodTable* methodTable, ref object @object)
    {
#if NET10_0_OR_GREATER
        @object = InternalAllocNoChecks_FastPath(methodTable);
        if (@object is null)
            InternalAllocNoChecks(methodTable, ref @object);
#elif NET8_0_OR_GREATER
        @object = RuntimeHelpers.GetUninitializedObject(methodTable->GetRuntimeType());
#endif
    }

#if NET10_0_OR_GREATER
    [UnsafeAccess(
        DeclaringTypeFullName: "System.RuntimeTypeHandle",
        MethodSignature: "System.Void System.RuntimeTypeHandle::InternalAllocNoChecks(System.Runtime.CompilerServices.MethodTable*,System.Runtime.CompilerServices.ObjectHandleOnStack)"
    )]
    private unsafe static extern void InternalAllocNoChecks(MethodTable* methodTable, ref object @object);

    [UnsafeAccess("System.RuntimeTypeHandle")]
    private unsafe static extern object InternalAllocNoChecks_FastPath(MethodTable* methodTable);
#endif

    /********************************************************************************************/

    public static void AllocateArray(MethodTable* methodTable, uint rank, uint* lengths, uint* lowerBounds, ref object array)
    {
#if NET9_0_OR_GREATER
        var runtimeType = methodTable->GetRuntimeType();

        var qcallType = stackalloc void*[] { &runtimeType, methodTable };
        var parray = Unsafe.AsPointer(ref array);

        InternalCreate(*(Span<int>*)qcallType, rank, lengths, lowerBounds, isFromArrayType: 1, *(Span<int>*)&parray);
#elif NET8_0_OR_GREATER
        var elementMethodTable = methodTable->ElementType;
        var elementRuntimeType = elementMethodTable->GetRuntimeType();
        array = InternalCreate(elementRuntimeType, rank, lengths, lowerBounds);
#endif
    }

#if NET9_0_OR_GREATER
    [UnsafeAccess("System.Array", MethodName: "<InternalCreate>g____PInvoke|0_0")]
    static extern void InternalCreate(Span<int> qcallType, uint rank, uint* pLengths, uint* pLowerBounds, int isFromArrayType, Span<int> stackObjectResult);
#elif NET8_0_OR_GREATER
    [UnsafeAccess("System.Array")]
    static extern Array InternalCreate(Type elementType, uint rank, uint* pLengths, uint* pLowerBounds);
#endif

    /********************************************************************************************/

    public static void AllocateArray(MethodTable* methodTable, uint length, uint gcFlags, ref object array)
    {
#if NET10_0_OR_GREATER
        AllocateNewArray(methodTable, length, gcFlags, ref @array);
#elif NET8_0_OR_GREATER
        array = AllocateNewArray(methodTable, length, gcFlags);
#endif
    }

#if NET10_0_OR_GREATER
    [UnsafeAccess("System.GC")]
    static extern void AllocateNewArray(MethodTable* methodTable, uint length, uint flags, ref object array);
#else
    [UnsafeAccess("System.GC")]
    static extern Array AllocateNewArray(MethodTable* methodTable, uint length, uint flags);
#endif
}