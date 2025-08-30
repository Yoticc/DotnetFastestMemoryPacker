#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
namespace DotnetFastestMemoryPacker.Internal;
static unsafe class ExtrinsicsImpl
{
    public static MethodTable* GetMethodTable<T>() => (MethodTable*)GetTypeHandle<T>().Value;

    public static MethodTable* GetMethodTable(object @object) => *(MethodTable**)As<nint>(@object);

    public static T* GetArrayPointer<T>(T[] array) => (T*)LoadEffectiveAddress(array, 16);
    public static void* GetArrayPointer(Array array) => LoadEffectiveAddress(array, 16);

    public static T LoadEffectiveValue<T>(in object @object, long offset) => *(T*)LoadEffectiveAddress(@object, offset);
}