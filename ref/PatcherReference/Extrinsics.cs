#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
#pragma warning disable CS8601 // Possible null reference assignment.
namespace PatcherReference;
public unsafe class Extrinsics // well i think you got the idea, like wordplay, intrinsic - extrinsic, ha-ha 💀
{
    // set pinnable flag to passed local variable
    public static void Pinnable<T>(out T value) => value = default;

    // does nothing. just code for compiler.
    // mainly aimed at replacing Unsafe.SkipInit for vectors
    public static void Uninitialized<T>(out T value) => value = default;

    // uses add to any type, even gc, which creates the possibility of direct addressing for pinned objects
    public static byte* LoadEffectiveAddress(/*pinned*/ object @object, long offset) => (byte*)(*(nint*)&@object + offset);
    public static byte* LoadEffectiveAddress(/*pinned*/ object @object, ulong offset) => (byte*)(*(nuint*)&@object + offset);

    // does nothing. just code for compiler.
    public static ToT As<ToT>(object @object) => *(ToT*)&@object;

    // il. ldtoken T
    public static RuntimeTypeHandle GetTypeHandle<T>() => typeof(T).TypeHandle;
}