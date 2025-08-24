using BenchmarkDotNet.Attributes;
using DotnetFastestMemoryPacker;
using MemoryPack;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ExecutionBench.Benchmarks;
public partial class ClassWith60SimpleFields
{
    static readonly Class input = new Class
    {
        Value01 = 100,
        Value02 = 100,
        Value03 = 100,
        Value04 = 100,
        Value05 = 100,
        Value06 = 100,
        Value07 = 100,
        Value08 = 100,
        Value09 = 100,
        Value10 = 100,
        Value11 = 100,
        Value12 = 100,
        Value13 = 100,
        Value14 = 100,
        Value15 = 100,
        Value16 = 100,
        Value17 = 100,
        Value18 = 100,
        Value19 = 100,
        Value20 = 100,
        Value21 = 100,
        Value22 = 100,
        Value23 = 100,
        Value24 = 100,
        Value25 = 100,
        Value26 = 100,
        Value27 = 100,
        Value28 = 100,
        Value29 = 100,
        Value30 = 100,
        Value31 = 100,
        Value32 = 100,
        Value33 = 100,
        Value34 = 100,
        Value35 = 100,
        Value36 = 100,
        Value37 = 100,
        Value38 = 100,
        Value39 = 100,
        Value40 = 100,
        Value41 = 100,
        Value42 = 100,
        Value43 = 100,
        Value44 = 100,
        Value45 = 100,
        Value46 = 100,
        Value47 = 100,
        Value48 = 100,
        Value49 = 100,
        Value50 = 100,
        Value51 = 100,
        Value52 = 100,
        Value53 = 100,
        Value54 = 100,
        Value55 = 100,
        Value56 = 100,
        Value57 = 100,
        Value58 = 100,
        Value59 = 100,
        Value60 = 100
    };

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

    [Benchmark]
    public void DeserializeFastestMemoryPacker()
    {
        FastestMemoryPacker.Deserialize<Class>(outputFMP);
    }

    [MemoryPackable]
    partial class Class
    {
        public long Value01 { get; set; }
        public long Value02 { get; set; }
        public long Value03 { get; set; }
        public long Value04 { get; set; }
        public long Value05 { get; set; }
        public long Value06 { get; set; }
        public long Value07 { get; set; }
        public long Value08 { get; set; }
        public long Value09 { get; set; }
        public long Value10 { get; set; }
        public long Value11 { get; set; }
        public long Value12 { get; set; }
        public long Value13 { get; set; }
        public long Value14 { get; set; }
        public long Value15 { get; set; }
        public long Value16 { get; set; }
        public long Value17 { get; set; }
        public long Value18 { get; set; }
        public long Value19 { get; set; }
        public long Value20 { get; set; }
        public long Value21 { get; set; }
        public long Value22 { get; set; }
        public long Value23 { get; set; }
        public long Value24 { get; set; }
        public long Value25 { get; set; }
        public long Value26 { get; set; }
        public long Value27 { get; set; }
        public long Value28 { get; set; }
        public long Value29 { get; set; }
        public long Value30 { get; set; }
        public long Value31 { get; set; }
        public long Value32 { get; set; }
        public long Value33 { get; set; }
        public long Value34 { get; set; }
        public long Value35 { get; set; }
        public long Value36 { get; set; }
        public long Value37 { get; set; }
        public long Value38 { get; set; }
        public long Value39 { get; set; }
        public long Value40 { get; set; }
        public long Value41 { get; set; }
        public long Value42 { get; set; }
        public long Value43 { get; set; }
        public long Value44 { get; set; }
        public long Value45 { get; set; }
        public long Value46 { get; set; }
        public long Value47 { get; set; }
        public long Value48 { get; set; }
        public long Value49 { get; set; }
        public long Value50 { get; set; }
        public long Value51 { get; set; }
        public long Value52 { get; set; }
        public long Value53 { get; set; }
        public long Value54 { get; set; }
        public long Value55 { get; set; }
        public long Value56 { get; set; }
        public long Value57 { get; set; }
        public long Value58 { get; set; }
        public long Value59 { get; set; }
        public long Value60 { get; set; }
    }
}