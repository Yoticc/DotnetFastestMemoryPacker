using BenchmarkDotNet.Attributes;
using DotnetFastestMemoryPacker;
using MemoryPack;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ExecutionBench.Benchmarks;
public partial class StructWithOtherStructs
{
    static readonly Struct4 struct4 = new Struct4
    {
        Value1 = 901327461849171L,
        Value2 = 524381661529101L,
        Value3 = 208332764279421L,
        Value4 = 298339263729171L,
        Value5 = 001339268939481L,
        Value6 = 641397561619291L
    };

    static readonly Struct3 struct3 = new Struct3
    {
        Struct4 = struct4,
        Struct42 = struct4,
        Value1 = false,
        Value2 = false
    };

    static readonly Struct2 struct2 = new Struct2
    {
        Struct3 = struct3,
        Struct4 = struct4,
        Value1 = 2947928748,
        Value2 = 24987
    };

    static readonly Struct1 struct1 = new Struct1
    {
        Struct2 = struct2,
        Struct3 = struct3,
        Value1 = 80,
        Value2 = 2908744792,
        Value3 = 299403920
    };

    static readonly RootStruct input = new RootStruct
    {
        Struct1 = struct1,
        Value1 = 0xB00B1E3,
        Value2 = 0xDEAD40E
    };

    static readonly string outputNewtonsoft = JsonConvert.SerializeObject(input);
    static readonly string outputSTJ = JsonSerializer.Serialize(input);
    static readonly byte[] outputMP = MemoryPackSerializer.Serialize(input);
    static readonly byte[] outputFMP = FastestMemoryPacker.SerializeUnmanaged(input);

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

    [Benchmark]
    public void SerializeFastestMemoryPacker()
    {
        FastestMemoryPacker.SerializeUnmanaged(input);
    }

    [Benchmark]
    public void DeserializeNewtonsoft()
    {
        JsonConvert.DeserializeObject<RootStruct>(outputNewtonsoft);
    }

    [Benchmark]
    public void DeserializeSTJ()
    {
        JsonSerializer.Deserialize<RootStruct>(outputSTJ);
    }

    [Benchmark]
    public void DeserializeMemoryPack()
    {
        MemoryPackSerializer.Deserialize<RootStruct>(outputMP);
    }

    [Benchmark]
    public void DeserializeFastestMemoryPacker()
    {
        FastestMemoryPacker.DeserializeUnmanaged<RootStruct>(outputFMP);
    }

    [MemoryPackable]
    public partial struct RootStruct
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public Struct1 Struct1 { get; set; }
    }

    [MemoryPackable]
    public partial struct Struct1
    {
        public int Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public Struct2 Struct2 { get; set; }
        public Struct3 Struct3 { get; set; }
    }

    [MemoryPackable]
    public partial struct Struct2
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public Struct3 Struct3 { get; set; }
        public Struct4 Struct4 { get; set; }
    }

    [MemoryPackable]
    public partial struct Struct3
    {
        public bool Value1 { get; set; }
        public bool Value2 { get; set; }
        public Struct4 Struct4 { get; set; }
        public Struct4 Struct42 { get; set; }
    }

    [MemoryPackable]
    public partial struct Struct4
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public long Value4 { get; set; }
        public long Value5 { get; set; }
        public long Value6 { get; set; }
    }
}