using BenchmarkDotNet.Running;
using Microsoft.Language.Xml.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(ParserBenchmarks).Assembly).Run(args);
