using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Language.Xml.Test
{
    [TestClass]
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

        [TestMethod]
        public void ParserErrorTolerance2()
        {
            T("<abc></abc>");
        }

        [TestMethod]
        public void ParseEmptyElement()
        {
            T(" <a/>");
            T("<a/> ");
            T(" <a/> ");
            T(" <a  /> ");
        }

        //[TestMethod]
        public void ParseLargeFile()
        {
            var text = File.ReadAllText(@"D:\1.xml");
            T(text);
        }

        [TestMethod]
        public void ParserAttributeOnNonEmptyElement()
        {
            var document = T("<a b='bval' d='dval'><c /></a>");
            Assert.AreEqual(2, document.Root.Attributes.Count());
            Assert.AreEqual("bval", document.Root["b"]);
        }

        [TestMethod]
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

        [TestMethod]
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

            var descendantList = root.GetDescendants().Select(x =>
                new KeyValuePair<int, IXmlElement>(x.Start, x))
                .ToList();

            int last = 0;
            foreach (var descendantEntry in descendantList)
            {
                int start = descendantEntry.Key;
                var element = (SyntaxNode)descendantEntry.Value;
                Assert.IsTrue(last <= start);
                VerifyText(xml, element);

                foreach (var node in element.ChildNodes)
                {
                    VerifyText(xml, node);
                }

                last = start;
            }

            var width = root.FullWidth;
            Assert.AreEqual(xml.Length, width);

            root.GetLeadingTrivia();
            root.GetTrailingTrivia();

            return root;
        }

        private static void VerifyText(string xml, SyntaxNode node)
        {
            var terminal = (SyntaxToken)(node.GetFirstTerminal() ?? node);
            var subXml = xml.Substring(node.Start + terminal.GetLeadingTriviaWidth(),
                terminal.Text.Length);
            Assert.AreEqual(subXml, terminal.Text);
        }
    }
}
