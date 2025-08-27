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
            JsonConvert.DeserializeObject<Class>(outputNewtonsoft);
        }

        [Benchmark]
        public void DeserializeSTJ()
        {
            JsonSerializer.Deserialize<Class>(outputSTJ);
        }

        [Benchmark]
        public void DeserializeMemoryPack()
        {
            MemoryPackSerializer.Deserialize<Class>(outputMP);
        }

        [Benchmark(Baseline = true)]
        public void DeserializeFastestMemoryPacker()
        {
            FastestMemoryPacker.Deserialize<Class>(outputFMP);
        }
    }

    [MemoryPackable]
    partial class Class
    {
        public int IntValue { get; set; }
        public int IntValue2 { get; set; }
        public long LongValue { get; set; }
        public long LongValue2 { get; set; }
        public short ShortValue { get; set; }
        public short ShortValue2 { get; set; }
        public short ShortValue3 { get; set; }
        public short ShortValue4 { get; set; }
        public bool BoolValue { get; set; }
        public bool BoolValue2 { get; set; }
    }
}