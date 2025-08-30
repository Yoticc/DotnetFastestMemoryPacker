#define SUPPORT_DISASMO_ENTRYPOINT

#if SUPPORT_DISASMO_ENTRYPOINT
using DotnetFastestMemoryPacker;
using System.Runtime.CompilerServices;

class Program
{
    static void Main()
    {
        for (var i = 0; i < 100; i++)
        {
            //A();
            //B();
            //C();
            D();
            Thread.Sleep(120);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void A()
    {
        FastestMemoryPacker.Serialize(new int[] { 4, 4, 4});
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void B()
    {
        FastestMemoryPacker.Serialize("mystring");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void C()
    {
        FastestMemoryPacker.Serialize(100);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void D()
    {
        var serialized = FastestMemoryPacker.Serialize(new string[] { "mystring", "mystring2", "mystring3" });
    }
}
#endif