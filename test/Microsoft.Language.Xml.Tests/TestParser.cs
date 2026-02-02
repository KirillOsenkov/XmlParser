using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestParser
    {
        public const string allXml =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<X>
    <n:T></n:T>
    <X/>
    <A.B></A.B>
    <A B=""""></A>
    <A>&#x03C0;</A>
    <A>a &lt;</A>
    <A><![CDATA[bar]]></A>
    <!-- comment -->
</X>";

        [Fact]
        public void ParserErrorTolerance2()
        {
            T("<abc></abc>");
        }

        [Fact]
        public void TestIssue59()
        {
            var xml = Parser.ParseText(" <a><b><//b></a>");
            var root = (XmlElementSyntax)xml.RootSyntax;
            var allTrivia = root.DescendantTrivia().ToArray();
            var b = root.EndTag;
            var leadingTrivia = b.GetLeadingTrivia();
            foreach (var item in leadingTrivia)
            {
            }
        }

        [Fact]
        public void ParseEmptyElement()
        {
            T(" <a/>");
            T("<a/> ");
            T(" <a/> ");
            T(" <a  /> ");
        }

        [Fact]
        public void ParseMultipleAttributes()
        {
            T(@"<a attr1=""foo"" attr2=""bar"" />");
            T(@"<a attr1=""foo"" attr2=""bar""></a>");
        }

        //[TestMethod]
        private void ParseLargeFile()
        {
            var text = File.ReadAllText(@"D:\1.xml");
            T(text);
        }

        [Fact]
        public void ParserAttributeOnNonEmptyElement()
        {
            var document = T("<a b='bval' d='dval'><c /></a>");
            Assert.Equal(2, document.Root.Attributes.Count());
            Assert.Equal("bval", document.Root["b"]);
        }

        [Fact]
        public void SelectsCorrectEndTag_ChildWithSameName()
        {
            var xml = """
                <Root>
                  <A>
                    <A>
                  </A>
                </Root>
                """;
            var document = Parser.ParseText(xml);
            var root = (XmlElementSyntax)document.RootSyntax;
            var a1 = (XmlElementSyntax)Assert.Single(root.Content);
            var a2 = (XmlElementSyntax)Assert.Single(a1.Content);

            // a1 should have an end tag, a2 should not. The parser should use the indentation
            // to determine which start tag the end tag belongs to.
            Assert.True(a1.EndTag.Span.Length > 0);
            Assert.False(a2.EndTag.Span.Length > 0);
        }
        [Fact]
        public void SelectsCorrectEndTag_ThreeLevels()
        {
            var xml = """
                <Root>
                  <A>
                    <A>
                      <A>
                    </A>
                  </A>
                </Root>
                """;
            var document = Parser.ParseText(xml);
            var root = (XmlElementSyntax)document.RootSyntax;
            var a1 = (XmlElementSyntax)Assert.Single(root.Content);
            var a2 = (XmlElementSyntax)Assert.Single(a1.Content);
            var a3 = (XmlElementSyntax)Assert.Single(a2.Content);

            // a1 and a2 should have end tags matching by indent, a3 should not.
            Assert.True(a1.EndTag.Span.Length > 0);
            Assert.True(a2.EndTag.Span.Length > 0);
            Assert.False(a3.EndTag.Span.Length > 0);
        }

        [Fact]
        public void SelectsCorrectEndTag_SameIndentSiblings()
        {
            // When same-name elements are siblings at the same indent, normal matching applies.
            var xml = """
                <Root>
                  <A>
                  </A>
                  <A>
                  </A>
                </Root>
                """;
            var document = Parser.ParseText(xml);
            var root = (XmlElementSyntax)document.RootSyntax;
            Assert.Equal(2, root.Content.Count);
            var a1 = (XmlElementSyntax)root.Content[0];
            var a2 = (XmlElementSyntax)root.Content[1];
            Assert.True(a1.EndTag.Span.Length > 0);
            Assert.True(a2.EndTag.Span.Length > 0);
        }

        [Fact]
        public void SelectsCorrectEndTag_DifferentNames()
        {
            // When names differ, indent doesn't matter - the parser matches by name.
            var xml = """
                <Root>
                  <A>
                    <B>
                  </A>
                </Root>
                """;
            var document = Parser.ParseText(xml);
            var root = (XmlElementSyntax)document.RootSyntax;
            var a = (XmlElementSyntax)Assert.Single(root.Content);
            var b = (XmlElementSyntax)Assert.Single(a.Content);
            Assert.True(a.EndTag.Span.Length > 0);
            Assert.False(b.EndTag.Span.Length > 0);
        }

        [Fact]
        public void SelectsCorrectEndTag_DifferentNamesSibling()
        {
            var xml = """
                <Root>
                  <A>
                    <B>
                    <C/>
                  </A>
                </Root>
                """;
            var document = Parser.ParseText(xml);
            var root = (XmlElementSyntax)document.RootSyntax;
            var a = (XmlElementSyntax)Assert.Single(root.Content);
            var b = (XmlElementSyntax)a.Content[0];
            var c = (XmlElementSyntax)a.Content[1];

            Assert.True(a.EndTag.Span.Length > 0);
            Assert.False(b.EndTag.Span.Length > 0);
            Assert.True(c.EndTag.Span.Length > 0);
        }

        [Fact]
        public void ParserErrorTolerance()
        {
            T("");
            T("<");
            T("<a b=");
            T("<x><a b=</x>");
            T("</a>");
            T("<?");
            T("<?xml");
            T("<?xml v");
            T("![CDATA[b]]>");
            T("<?xml version=\"\"1.0\" encoding=\"UTF-8\" ?><test></test>");
            T("?xml version=\"1.0\"?><X></X>");
            T("<?xml?>a><b/></a>");
            T("<?xml?>a&<A>");
            T("<ab><a a=1\" /></ab>");
            T("< /x>");
            T("<? ");
            T("<a/ >");
        }

        [Fact]
        public void WrongClosingTagRecovery()
        {
            T("<X><n:></a><b></X>");
        }

        [Fact]
        public void WrongDoctypeParse()
        {
            T("<x><!DOCTYPE");
        }

        [Fact]
        public void Doctype()
        {
            T("<!DOCTYPE html><x></x>");
            T("<!DOCTYPE html>a<x></x>");
        }

        [Fact]
        public void ExhaustiveSubstring()
        {
            for (int start = 0; start < allXml.Length; start++)
            {
                for (int length = 1; length < allXml.Length - start + 1; length++)
                {
                    // take just the middle
                    var substring = allXml.Substring(start, length);
                    T(substring);

                    // take everything but the middle
                    substring = allXml.Substring(0, start) + allXml.Substring(start + length, allXml.Length - start - length);
                    T(substring);

                    // cut out one char at beginning of chunk and one char at the end
                    substring = allXml.Remove(start + length - 1, 1);
                    if (start < substring.Length)
                    {
                        substring = substring.Remove(start, 1);
                    }

                    T(substring);

                    // cut out the middle and replace with a space
                    substring = allXml.Substring(0, start) + " " + allXml.Substring(start + length, allXml.Length - start - length);
                    T(substring);
                }
            }
        }

        private XmlDocumentSyntax T(string xml)
        {
            var root = Parser.ParseText(xml);

            var descendantList = root.Descendants().Select (x => x.AsElement).Select(x =>
                new KeyValuePair<int, IXmlElement>(x.Start, x))
                .ToList();

            string tokensAndTriviaConcatenated = ConcatenateAllTokensAndTrivia(root);
            Assert.Equal(xml, tokensAndTriviaConcatenated);

            int last = 0;
            foreach (var descendantEntry in descendantList)
            {
                int start = descendantEntry.Key;
                var element = (SyntaxNode)descendantEntry.Value;
                Assert.True(last <= start);
                VerifyText(xml, element);

                foreach (var node in element.ChildNodes)
                {
                    VerifyText(xml, node);
                }

                last = start;
            }

            var width = root.FullWidth;
            Assert.Equal(xml.Length, width);

            root.GetLeadingTrivia();
            root.GetTrailingTrivia();
            var allTrivia = root.DescendantTrivia().ToArray();

            return root;
        }

        private string ConcatenateAllTokensAndTrivia(XmlDocumentSyntax root)
        {
            var sb = new StringBuilder();

            var all = root.DescendantNodesAndTokens();
            foreach (var item in all)
            {
                if (item is SyntaxToken token)
                {
                    var leading = token.GetLeadingTrivia();
                    if (leading.Count > 0)
                    {
                        foreach (var trivia in leading)
                        {
                            sb.Append(trivia.ToString());
                        }
                    }

                    sb.Append(token.Text);

                    var trailing = token.GetTrailingTrivia();
                    if (trailing.Count > 0)
                    {
                        foreach (var trivia in trailing)
                        {
                            sb.Append(trivia.ToString());
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static void VerifyText(string xml, SyntaxNode node)
        {
            var terminal = (SyntaxToken)(node.GetFirstTerminal() ?? node);
            int start = node.Start + terminal.GetLeadingTriviaSpan().Length;
            int length = terminal.Text.Length;
            if (start + length > xml.Length)
            {
                throw new Exception($"String out of bounds. Start={start} Length={length} xml.Length={xml.Length}\nxml={xml}node={node}\nterminal={terminal}");
            }

            var subXml = xml.Substring(start, length);
            Assert.Equal(subXml, terminal.Text);
        }
    }
}
