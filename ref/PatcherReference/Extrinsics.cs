#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace PatcherReference;
public unsafe class Extrinsics // well i think you got the idea, like wordplay, intrinsic - extrinsic, ha-ha 💀
{
    // set pinnable flag to every passed local variable
    public static void Pinnable<T>(out T value) => value = default;

    // uses add to any type, even gc, which creates the possibility of direct addressing for pinned objects.
    // warning: object should be pinned.
    public static void* LoadEffectiveAddress(object @object, long offset) => (void*)(*(nint*)&@object + offset);
    public static void* LoadEffectiveAddress(object @object, ulong offset) => (void*)(*(nuint*)&@object + offset);

    // does nothing. just code for compilation
    public static ToT As<ToT>(object @object) => *(ToT*)&@object;

    // il. ldtoken T
    public static RuntimeTypeHandle GetTypeHandle<T>() => typeof(T).TypeHandle;
}