using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Microsoft.Language.Xml.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net472)]
[SimpleJob(RuntimeMoniker.Net80)]
public class ParserBenchmarks
{
    private Buffer textBuffer;

    [GlobalSetup]
    public void Setup()
    {
        textBuffer = new StringBuffer(XmlSnippets.LongAndroidLayoutXml);
    }

    [Benchmark]
    public void ParseLongXml() => Parser.Parse(textBuffer);
}
