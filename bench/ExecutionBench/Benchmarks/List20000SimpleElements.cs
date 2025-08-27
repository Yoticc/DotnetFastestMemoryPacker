using BenchmarkDotNet.Attributes;
using DotnetFastestMemoryPacker;
using MemoryPack;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ExecutionBench.Benchmarks;
public class List20000SimpleElements
{
    static readonly List<int> input = Enumerable.Repeat(100, 20000).ToList();

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
        public void SerializeBclSTJ()
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
            JsonConvert.DeserializeObject<List<int>>(outputNewtonsoft);
        }

        [Benchmark]
        public void DeserializeSTJ()
        {
            JsonSerializer.Deserialize<List<int>>(outputSTJ);
        }

        [Benchmark]
        public void DeserializeMemoryPack()
        {
            MemoryPackSerializer.Deserialize<List<int>>(outputMP);
        }
        
        [Benchmark(Baseline = true)]
        public void DeserializeFastestMemoryPacker()
        {
            FastestMemoryPacker.Deserialize<List<int>>(outputFMP);
        }
    }
}