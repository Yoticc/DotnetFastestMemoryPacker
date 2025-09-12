#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
using PatcherReference;

namespace DotnetFastestMemoryPacker.Internal;
[InlineAllMembers]
static unsafe class ExtrinsicsImpl
{
    public static MethodTable* GetMethodTable<T>() => (MethodTable*)GetTypeHandle<T>().Value;

    public static MethodTable* GetMethodTable(object @object) => *(MethodTable**)As<nint>(@object);
    
    public static void SetMethodTable(object @object, MethodTable* methodTable) => *(MethodTable**)As<nint>(@object) = methodTable;

    public static void SetMethodTable<T>(object @object) => *(MethodTable**)As<nint>(@object) = GetMethodTable<T>();

    public static T* GetArrayBody<T>(T[] array) => (T*)LoadEffectiveAddress(array, SizeOf.MethodTable + sizeof(nint));
    public static byte* GetArrayBody(object/*Array*/ array) => LoadEffectiveAddress(array, SizeOf.MethodTable + sizeof(nint));

    public static byte* GetObjectBody(object @object) => LoadEffectiveAddress(@object, SizeOf.MethodTable);

    public static char* GetStringBody(object @object) => (char*)LoadEffectiveAddress(@object, SizeOf.MethodTable + SizeOf.StringLength);

    public static uint GetComponentsCount(object/*mt->HasComponentSize*/ @object) => LoadEffectiveValue<uint>(@object, SizeOf.MethodTable);
    public static uint GetArrayLength(object/*Array*/ array) => GetComponentsCount(array);
    public static uint GetStringLength(object/*string*/ array) => GetComponentsCount(array);

    public static void SetStringLength(object array, uint length) => *(uint*)LoadEffectiveAddress(array, SizeOf.MethodTable) = length;

    public static T LoadEffectiveValue<T>(object @object, long offset) => *(T*)LoadEffectiveAddress(@object, offset);
}