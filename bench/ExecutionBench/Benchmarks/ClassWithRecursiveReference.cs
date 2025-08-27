using BenchmarkDotNet.Attributes;
using DotnetFastestMemoryPacker;
using MemoryPack;

namespace ExecutionBench.Benchmarks;
public partial class ClassWithRecursiveReference
{
    static ClassWithRecursiveReference()
    {
        input = new ImplicitRecursiveReference0
        {
            Value1 = 0909
        };

        var implicitRecursiveReference1 = new ImplicitRecursiveReference1
        {
            String = "Waiting for recaf 5. Day 600",
            Value1 = 9061,
            Parent = input
        };

        input.SomeElement = implicitRecursiveReference1;
    }

    static ImplicitRecursiveReference0 input;

    static readonly byte[] outputMP = MemoryPackSerializer.Serialize(input);
    static readonly byte[] outputFMP = FastestMemoryPacker.Serialize(input);

    [BenchmarkClass]
    public class Serialize
    {
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
        public void DeserializeMemoryPack()
        {
            MemoryPackSerializer.Deserialize<ImplicitRecursiveReference0>(outputMP);
        }

        [Benchmark(Baseline = true)]
        public void DeserializeFastestMemoryPacker()
        {
            FastestMemoryPacker.Deserialize<ImplicitRecursiveReference0>(outputFMP);
        }
    }

    [MemoryPackable(GenerateType.CircularReference)]
    partial class ImplicitRecursiveReference0
    {
        [MemoryPackOrder(0)]
        public int Value1;

        [MemoryPackOrder(1)]
        public ImplicitRecursiveReference1? SomeElement;
    }

    [MemoryPackable]
    partial class ImplicitRecursiveReference1
    {
        public int Value1;
        public string? String;
        public ImplicitRecursiveReference0? Parent;
    }
}