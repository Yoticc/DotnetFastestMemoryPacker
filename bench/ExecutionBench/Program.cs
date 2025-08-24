using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ExecutionBench.Benchmarks;
using System.Reflection;

BenchmarkRunner.Run<Array200WithDifferentInstances>();
Console.ReadLine();

return;

Console.ReadLine();

var benchmarksConfig =
    ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.StopOnFirstError)
    .WithOption(ConfigOptions.LogBuildOutput, false)
    .WithOptions(ConfigOptions.JoinSummary)
    .WithOptions(ConfigOptions.DisableLogFile);
    
var benchmarks = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.Namespace == "ExecutionBench.Benchmarks" && !type.IsNested).ToArray();
BenchmarkRunner.Run(benchmarks);

Console.WriteLine($"All benchmarks were passed!");
Console.ReadLine();