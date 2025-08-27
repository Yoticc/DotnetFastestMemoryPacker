using BenchmarkDotNet.Attributes;
using DotnetFastestMemoryPacker;
using MemoryPack;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ExecutionBench.Benchmarks;
public partial class ClassWith8Strings
{
    static readonly Class input = new Class
    {
        String1 = "⣿⣿⣿⣿⡇⢀⣼⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣧⠄⠄⢸⣿⣿⣿⣿",
        String2 = "⣿⣿⣿⣿⣇⣼⣿⣿⠿⠶⠙⣿⡟⠡⣴⣿⣽⣿⣧⠄⢸⣿⣿⣿⣿",
        String3 = "⣿⣿⣿⣿⣿⣾⣿⣿⣟⣭⣾⣿⣷⣶⣶⣴⣶⣿⣿⢄⣿⣿⣿⣿⣿",
        String4 = "⣿⣿⣿⣿⣿⣿⣿⣿⡟⣩⣿⣿⣿⡏⢻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿",
        String5 = "⣿⣿⣿⣿⣿⣿⣹⡋⠘⠷⣦⣀⣠⡶⠁⠈⠁⠄⣿⣿⣿⣿⣿⣿⣿",
        String6 = "⣿⣿⣿⣿⣿⣿⣍⠃⣴⣶⡔⠒⠄⣠⢀⠄⠄⠄⡨⣿⣿⣿⣿⣿⣿",
        String7 = "⣿⣿⣿⣿⣿⣿⣿⣦⡘⠿⣷⣿⠿⠟⠃⠄⠄⣠⡇⠈⠻⣿⣿⣿⣿",
        String8 = "⣿⣿⣿⣿⡿⠟⠋⢁⣷⣠⠄⠄⠄⠄⣀⣠⣿⠄⣀⠄⠄⣀⣿⣿⣿"
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
        public string String1 { get; set; }
        public string String2 { get; set; }
        public string String3 { get; set; }
        public string String4 { get; set; }
        public string String5 { get; set; }
        public string String6 { get; set; }
        public string String7 { get; set; }
        public string String8 { get; set; }
    }
}