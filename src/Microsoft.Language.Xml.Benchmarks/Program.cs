using BenchmarkDotNet.Running;

namespace Microsoft.Language.Xml.Benchmarks
{
    class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<ParserBenchmarks>();
        }
    }
}
