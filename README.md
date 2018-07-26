# XmlParser

![logo image](http://neteril.org/~jeremie/language_xml_logo.png)

[![Build status](https://ci.appveyor.com/api/projects/status/5ur9sv9bp4nr7a3n?svg=true)](https://ci.appveyor.com/project/KirillOsenkov/xmlparser)
[![NuGet package](https://img.shields.io/nuget/v/GuiLabs.Language.Xml.svg)](https://nuget.org/packages/GuiLabs.Language.Xml)
[![NuGet package for VS Editor](https://img.shields.io/nuget/v/GuiLabs.Language.Xml.Editor.svg)](https://nuget.org/packages/GuiLabs.Language.Xml.Editor)

A Roslyn-inspired full-fidelity XML parser with no dependencies and a simple Visual Studio XML language service.

 * The parser produces a **full-fidelity** syntax tree, meaning every character of the source text is represented in the tree. The tree covers the entire source text.
 * The parser has **no dependencies** and can easily be made portable. I would appreciate a high quality pull request making the parser portable.
 * The parser is based on the section of the Roslyn VB parser that parses XML literals. The Roslyn code is ported to C# and is made standalone.
 * The parser is **error-tolerant**. It will still produce a full tree even from invalid XML with missing tags, extra invalid text, etc. Missing and skipped tokens are still represented in the tree.
 * The resulting tree is **immutable** and follows Roslyn's [green/red separation](https://blogs.msdn.microsoft.com/ericlippert/2012/06/08/persistence-facades-and-roslyns-red-green-trees/) for maximum reusability of nodes.
 * The parser has basic support for **incrementality**. Given a previous constructed tree and a list of changes it will try to reuse existing nodes and only re-create what is necessary.
 * This library is more **low-level** than XLinq (for instance XLinq doesn't seem to represent whitespace around attributes). Also it has no idea about XML namespaces and just tells you what's in the source text (whereas in XLinq there's too much ceremony around XML namespaces).

This is work in progress and by no means complete. Specifically:
 * XML DTD is not supported (Roslyn didn't support it either)
 * Code wasn't tuned for performance and allocations, I'm sure a lot can be done to reduce memory consumption by the resulting tree. It should be pretty efficient though.
 * We reserve the right to accept only very high quality pull requests. We have very limited time to work on this so I ask everybody to please respect that.

## Download from NuGet:
 * [GuiLabs.Language.Xml](https://www.nuget.org/packages/GuiLabs.Language.Xml)
 * [GuiLabs.Language.Xml.Editor](https://www.nuget.org/packages/GuiLabs.Language.Xml.Editor)

## Try it!

https://xmlsyntaxvisualizer.azurewebsites.net/index.html

The above app leverages the parser and can help you visualize the resulting syntax tree generated from an XML document.

Code is available at https://github.com/garuma/XmlSyntaxVisualizer

Also see the blog post: 
https://blog.neteril.org/blog/2018/03/21/xml-parsing-roslyn/

Resources about Immutable Syntax Trees:
https://github.com/KirillOsenkov/Bliki/wiki/Roslyn-Immutable-Trees

## FAQ:

### How to find a node in the tree given a position in the source text?
https://github.com/KirillOsenkov/XmlParser/blob/master/src/Microsoft.Language.Xml/Utilities/SyntaxLocator.cs#L24

```
SyntaxLocator.FindNode(SyntaxNode node, int position);
```
