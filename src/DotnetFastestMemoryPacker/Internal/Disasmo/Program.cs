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
            A();
            B();
            C();
            Thread.Sleep(120);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void A()
    {
        FastestMemoryPacker.Serialize(new string[] { "123", "234", "567" });
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
}
#endif