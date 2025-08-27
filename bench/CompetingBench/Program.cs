using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DotnetFastestMemoryPacker;

BenchmarkRunner.Run<Bench>();
Console.ReadLine();

public class Bench
{
    static readonly Class2 class2 = new Class2
    {
        String1 = "Hello",
        String2 = "World!"
    };

    static readonly Class1 class1 = new Class1
    {
        Class2 = class2,
        Value1 = 2838239283923,
        Value2 = 4328948174241
    };

    static readonly Class0 class0 = new Class0
    {
        Class1 = class1,
        Class2 = class2,
    };

    [Benchmark]
    public void Serialization()
    {
        FastestMemoryPacker.Serialize(class0);
    }

    [Benchmark]
    public void Serialization2()
    {
        //FastestMemoryPacker.Serialize2(class0);
    }

    class Class0
    {
        public Class1 Class1;
        public Class2 Class2;
    }

    class Class1
    {
        public long Value1;
        public long Value2;
        public Class2 Class2;
    }

    class Class2
    {
        public string String1;
        public string String2;
        public int Value1;
        public int Value2;
    }
}