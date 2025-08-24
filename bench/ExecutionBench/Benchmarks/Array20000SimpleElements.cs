using BenchmarkDotNet.Attributes;
using DotnetFastestMemoryPacker;
using MemoryPack;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ExecutionBench.Benchmarks;
public class Array20000SimpleElements
{
    static readonly int[] input = Enumerable.Repeat(100, 20000).ToArray();

    static readonly string outputNewtonsoft = JsonConvert.SerializeObject(input);
    static readonly string outputSTJ = JsonSerializer.Serialize(input);
    static readonly byte[] outputMP = MemoryPackSerializer.Serialize(input);
    static readonly byte[] outputFMP = FastestMemoryPacker.Serialize(input);

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
        FastestMemoryPacker.Serialize(input);
    }

    [Benchmark]
    public void DeserializeNewtonsoft()
    {
        JsonConvert.DeserializeObject<int[]>(outputNewtonsoft);
    }

    [Benchmark]
    public void DeserializeSTJ()
    {
        JsonSerializer.Deserialize<int[]>(outputSTJ);
    }

    [Benchmark]
    public void DeserializeMemoryPack()
    {
        MemoryPackSerializer.Deserialize<int[]>(outputMP);
    }

    [Benchmark]
    public void DeserializeFastestMemoryPacker()
    {
        FastestMemoryPacker.Deserialize<int[]>(outputFMP);
    }
}