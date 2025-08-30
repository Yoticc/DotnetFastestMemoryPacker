using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<Bench>();
Console.ReadLine();

public class Bench
{
    class CustomCache<T>()
    {
        static CustomCache() => MethodTable = typeof(T).TypeHandle.Value;

        public static readonly nint MethodTable;
    }

    public static nint methodTable;

    [Benchmark]
    public void GetMTFromCustomCache() => methodTable = GetMTFromCustomCache<string>();

    [Benchmark]
    public void GetMTFromSystemCache() => methodTable = GetMTFromSystemCache<string>();

    static nint GetMTFromSystemCache<T>() => typeof(T).TypeHandle.Value;

    static nint GetMTFromCustomCache<T>() => CustomCache<T>.MethodTable;
}