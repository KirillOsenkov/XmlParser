using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Microsoft.Language.Xml.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net472, id: ".NET Framework")]
[SimpleJob(RuntimeMoniker.Net80, id: "Modern .NET")]
public class IncrementalParserBenchmarks
{
    private const string OriginalValue = "Union Square";
    private const string ModifiedValue = "Times Square";

    private string originalXml;
    private string modifiedXml;
    private XmlDocumentSyntax originalTree;
    private TextChangeRange[] changes;

    [GlobalSetup]
    public void Setup()
    {
        originalXml = XmlSnippets.LongAndroidLayoutXml;
        originalTree = Parser.ParseText(originalXml);

        // Simulate typing inside an attribute value (a common editor scenario).
        // Replace "Union Square" with "Times Square" - same length, no structural chars.
        var index = originalXml.IndexOf(OriginalValue);
        modifiedXml = originalXml.Substring(0, index) + ModifiedValue + originalXml.Substring(index + OriginalValue.Length);
        changes = new[]
        {
            new TextChangeRange(new TextSpan(index, OriginalValue.Length), ModifiedValue.Length)
        };
    }

    [Benchmark(Baseline = true)]
    public XmlDocumentSyntax FullReparse() => Parser.ParseText(modifiedXml);

    [Benchmark]
    public XmlDocumentSyntax IncrementalParse() => Parser.ParseIncremental(modifiedXml, changes, originalTree);
}
