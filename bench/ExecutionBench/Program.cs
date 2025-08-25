using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Reflection;

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

