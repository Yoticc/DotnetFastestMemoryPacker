using BenchmarkDotNet.Attributes;
using DotnetFastestMemoryPacker;
using MemoryPack;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ExecutionBench.Benchmarks;
public partial class ClassWithOtherClasses
{
    static readonly Class4 class4 = new Class4
    {
        Value1 = 901327461849171L,
        Value2 = 524381661529101L,
        Value3 = 208332764279421L,
        Value4 = 298339263729171L,
        Value5 = 001339268939481L,
        Value6 = 641397561619291L
    };

    static readonly Class3 class3 = new Class3
    {
        Class4 = class4,
        Class42 = class4,
        Value1 = false,
        Value2 = false
    };

    static readonly Class2 class2 = new Class2
    {
        Class3 = class3,
        Class4 = class4,
        Value1 = 2947928748,
        Value2 = 24987
    };

    static readonly Class1 class1 = new Class1
    {
        Class2 = class2,
        Class3 = class3,
        Value1 = 80,
        Value2 = 2908744792,
        Value3 = 299403920
    };

    static readonly RootClass input = new RootClass
    {
        Struct1 = class1,
        Value1 = 0xB00B1E3,
        Value2 = 0xDEAD40E
    };

    static readonly string outputNewtonsoft = JsonConvert.SerializeObject(input);
    static readonly string outputSTJ = JsonSerializer.Serialize(input);
    static readonly byte[] outputMP = MemoryPackSerializer.Serialize(input);
    static readonly byte[] outputFMP = FastestMemoryPacker.Serialize(input);

    [BenchmarkClass]
    public class Serialize
    {
        [Benchmark]
        public void SerializeNewtonsoft()
        {
            JsonConvert.SerializeObject(input);
        }

        [Benchmark]
        public void SerializeSTJ()
        {
            JsonSerializer.Serialize(input);
        }

        [Benchmark]
        public void SerializeMemoryPack()
        {
            MemoryPackSerializer.Serialize(input);
        }

        [Benchmark(Baseline = true)]
        public void SerializeFastestMemoryPacker()
        {
            FastestMemoryPacker.Serialize(input);
        }
    }

    [BenchmarkClass]
    public class Deserialize
    {
        [Benchmark]
        public void DeserializeNewtonsoft()
        {
            JsonConvert.DeserializeObject<RootClass>(outputNewtonsoft);
        }

        [Benchmark]
        public void DeserializeSTJ()
        {
            JsonSerializer.Deserialize<RootClass>(outputSTJ);
        }

        [Benchmark]
        public void DeserializeMemoryPack()
        {
            MemoryPackSerializer.Deserialize<RootClass>(outputMP);
        }

        [Benchmark(Baseline = true)]
        public void DeserializeFastestMemoryPacker()
        {
            FastestMemoryPacker.Deserialize<RootClass>(outputFMP);
        }
    }

    [MemoryPackable]
    public partial class RootClass
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public Class1 Struct1 { get; set; }
    }

    [MemoryPackable]
    public partial class Class1
    {
        public int Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public Class2 Class2 { get; set; }
        public Class3 Class3 { get; set; }
    }

    [MemoryPackable]
    public partial class Class2
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public Class3 Class3 { get; set; }
        public Class4 Class4 { get; set; }
    }

    [MemoryPackable]
    public partial class Class3
    {
        public bool Value1 { get; set; }
        public bool Value2 { get; set; }
        public Class4 Class4 { get; set; }
        public Class4 Class42 { get; set; }
    }

    [MemoryPackable]
    public partial class Class4
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public long Value4 { get; set; }
        public long Value5 { get; set; }
        public long Value6 { get; set; }
    }
}