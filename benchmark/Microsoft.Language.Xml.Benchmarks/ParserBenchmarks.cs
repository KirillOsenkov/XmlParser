using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Language.Xml.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 3, targetCount: 50)]
    public class ParserBenchmarks
    {
        readonly Buffer textBuffer;

        public ParserBenchmarks()
        {
            textBuffer = new StringBuffer(XmlSnippets.LongAndroidLayoutXml);
        }

        [Benchmark]
        public void ParseLongXml() => Parser.Parse(textBuffer);
    }
}
