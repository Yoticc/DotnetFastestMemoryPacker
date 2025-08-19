using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Reflection;

var benchmarksConfig =
    ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.StopOnFirstError);

var benchmarks = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.Namespace == "ExecutionBench.Benchmarks");
foreach (var benchmark in benchmarks)
{
    Console.WriteLine($"Running benchmark {benchmark.Name}...");
    BenchmarkRunner.Run(benchmark, benchmarksConfig);
}

Console.WriteLine($"All benchmarks were passed!");
Console.ReadLine();