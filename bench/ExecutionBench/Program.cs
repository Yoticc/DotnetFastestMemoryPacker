using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

var benchmarksConfig =
    ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.StopOnFirstError)
    .WithOption(ConfigOptions.LogBuildOutput, false)
    .WithOptions(ConfigOptions.JoinSummary)
    .WithOptions(ConfigOptions.DisableLogFile)
    .HideColumns(StatisticColumn.StdDev, StatisticColumn.Median, BaselineRatioColumn.RatioStdDev);
    
var benchmarks = typeof(BenchmarkClass).Assembly.GetTypes().Where(type => type.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(BenchmarkClass))).ToArray();
BenchmarkRunner.Run(benchmarks);

Console.WriteLine($"All benchmarks were passed!");
Console.ReadLine();