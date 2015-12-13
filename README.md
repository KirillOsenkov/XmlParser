# XmlParser
A Roslyn-inspired full-fidelity XML parser with no dependencies and a simple Visual Studio XML language service.

 * The parser produces a **full-fidelity** syntax tree, meaning every character of the source text is represented in the tree. The tree covers the entire source text.
 * The parser has **no dependencies** and can easily be made portable. I would appreciate a high quality pull request making the parser portable.
 * The parser is based on the section of the Roslyn VB parser that parses XML literals. The Roslyn code is ported to C# and is made standalone.
 * The parser is **error-tolerant**. It will still produce a full tree even from invalid XML with missing tags, extra invalid text, etc. Missing and skipped tokens are still represented in the tree.
 * This library is more **low-level** than XLinq (for instance XLinq doesn't seem to represent whitespace around attributes). Also it has no idea about XML namespaces and just tells you what's in the source text (whereas in XLinq there's too much ceremony around XML namespaces).

This is work in progress and by no means complete. Specifically:
 * XML DTD is not supported (Roslyn didn't support it either)
 * Although most of the tree is immutable, shortcuts were taken just to get it working, for example parent and position information were slapped directly onto the nodes, instead of proper red-green model like in Roslyn.
 * Code wasn't tuned for performance and allocations, I'm sure a lot can be done to reduce memory consumption by the resulting tree. It should be pretty efficient though.
 * I reserve the right to accept only very high quality pull requests. I have very limited time to work on this so I ask everybody to please respect that.

## Download from NuGet:
 * https://www.nuget.org/packages/Microsoft.Language.Xml
 * https://www.nuget.org/packages/Microsoft.Language.Xml.Editor

## Sample:

```
var root = Parser.ParseText(xml);
...
public interface IXmlElement
{
    string Name { get; }
    string Value { get; }
    IXmlElement Parent { get; }
    IEnumerable<IXmlElement> Elements { get; }
    IEnumerable<KeyValuePair<string, string>> Attributes { get; }
    string this[string attributeName] { get; }
}
```
