using BenchmarkDotNet.Attributes;
using DotnetFastestMemoryPacker;
using MemoryPack;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ExecutionBench.Benchmarks;
public partial class Array200WithDifferentInstances
{
    static readonly Class[] input = Enumerable.Range(0, 200).Select(i => new Class()
    {
        Value1 = 10,
        Value2 = 20,
        SomeString = "pmpmpmp mppm hppmmp hppm hpm! hpmhmphmp? phpmhpmhphmp!!",
        Value3 = 80
    }).ToArray();

    static readonly string outputNewtonsoft = JsonConvert.SerializeObject(input);
    static readonly string outputSTJ = JsonSerializer.Serialize(input);
    static readonly byte[] outputMP = MemoryPackSerializer.Serialize(input);
    static readonly byte[] outputFMP = FastestMemoryPacker.Serialize(input);

    /*
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
    */
     
    [Benchmark]
    public void SerializeFastestMemoryPacker()
    {
        FastestMemoryPacker.Serialize(input);
    }

    /*
    [Benchmark]
    public void DeserializeNewtonsoft()
    {
        JsonConvert.DeserializeObject<Class[]>(outputNewtonsoft);
    }

    [Benchmark]
    public void DeserializeSTJ()
    {
        JsonSerializer.Deserialize<Class[]>(outputSTJ);
    }

    [Benchmark]
    public void DeserializeMemoryPack()
    {
        MemoryPackSerializer.Deserialize<Class[]>(outputMP);
    }
    */

    [Benchmark]
    public void DeserializeFastestMemoryPacker()
    {
        FastestMemoryPacker.Deserialize<Class[]>(outputFMP);
    }

    [MemoryPackable]
    public partial class Class
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public string SomeString { get; set; }
        public int Value3 { get; set; }
    }
}