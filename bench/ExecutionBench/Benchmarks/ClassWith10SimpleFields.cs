using BenchmarkDotNet.Attributes;
using DotnetFastestMemoryPacker;
using MemoryPack;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ExecutionBench.Benchmarks;
public partial class ClassWith10SimpleFields
{
    static readonly Class input = new Class
    {
        IntValue = 10,
        IntValue2 = 20,
        LongValue = 30,
        LongValue2 = 40,
        ShortValue = 50,
        ShortValue2 = 60,
        ShortValue3 = 70,
        ShortValue4 = 80,
        BoolValue = false,
        BoolValue2 = true
    };

    static readonly string outputNewtonsoftJson = JsonConvert.SerializeObject(input);
    static readonly string outputBclJson = JsonSerializer.Serialize(input);
    static readonly byte[] outputMP = MemoryPackSerializer.Serialize(input);
    static readonly byte[] outputFMP = FastestMemoryPacker.Serialize(input);

    [Benchmark]
    public void SerializeNewtonsoftJson()
    {
        JsonConvert.SerializeObject(input);
    }

    [Benchmark]
    public void SerializeBclJson()
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
        FastestMemoryPacker.Serialize(input);
    }

    [Benchmark]
    public void DeserializeNewtonsoftJson()
    {
        JsonConvert.DeserializeObject<Class>(outputNewtonsoftJson);
    }

    [Benchmark]
    public void DeserializeBclJson()
    {
        JsonSerializer.Deserialize<Class>(outputBclJson);
    }

    [Benchmark]
    public void DeserializeMemoryPack()
    {
        MemoryPackSerializer.Deserialize<Class>(outputMP);
    }

    [Benchmark]
    public void DeserializeFastestMemoryPacker()
    {
        FastestMemoryPacker.Deserialize<Class>(outputFMP);
    }

    [MemoryPackable]
    partial class Class
    {
        public int IntValue;
        public int IntValue2;
        public long LongValue;
        public long LongValue2;
        public short ShortValue;
        public short ShortValue2;
        public short ShortValue3;
        public short ShortValue4;
        public bool BoolValue;
        public bool BoolValue2;
    }
}