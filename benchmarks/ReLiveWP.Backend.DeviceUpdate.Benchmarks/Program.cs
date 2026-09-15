using BenchmarkDotNet.Running;
using ReLiveWP.Backend.DeviceUpdate.Benchmarks;

Console.WriteLine($"catalog: {BenchmarkCatalog.Describe()}");

BenchmarkSwitcher.FromAssembly(typeof(BenchmarkCatalog).Assembly).Run(args);
