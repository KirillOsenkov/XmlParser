using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Language.Xml.Test
{
    public class TestTrivia
    {
        const string ThreeSpaces = "   ";
        const string XmlElementWithAttributeAndContent = "<foo attribute=\"value\">content</foo>";

        [Fact]
        public void TestXmlElementSyntaxLeadingTrivia()
        {
            var element = GetElementSyntax(XmlElementWithAttributeAndContent);
            var elementWithTrivia = element.WithLeadingTrivia(SyntaxFactory.WhitespaceTrivia(ThreeSpaces));
            Assert.NotSame(elementWithTrivia, element);
            Assert.IsType<XmlElementSyntax>(elementWithTrivia);
            Assert.StartsWith(ThreeSpaces, elementWithTrivia.ToFullString());
        }

        [Fact]
        public void TestXmlElementSyntaxTrailingTrivia()
        {
            var element = GetElementSyntax(XmlElementWithAttributeAndContent);
            var elementWithTrivia = element.WithTrailingTrivia(SyntaxFactory.WhitespaceTrivia(ThreeSpaces));
            Assert.NotSame(elementWithTrivia, element);
            Assert.IsType<XmlElementSyntax>(elementWithTrivia);
            Assert.EndsWith(ThreeSpaces, elementWithTrivia.ToFullString());
        }

        static XmlElementSyntax GetElementSyntax(string xml) => Parser.ParseText(xml).RootSyntax as XmlElementSyntax;
    }
}
