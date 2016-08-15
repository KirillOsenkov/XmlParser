using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Language.Xml.Test
{
    [TestClass]
    public class TestParser
    {
        private const string allXml =
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
            T("<a/ >");
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
            var width = root.FullWidth;
            Assert.AreEqual(xml.Length, width);

            root.GetLeadingTrivia();
            root.GetTrailingTrivia();

            return root;
        }
    }
}
