using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestTrivia
    {
        const string ThreeSpaces = "   ";
        const string Tab = "\t";
        const string XmlElementWithAttributeAndContent = "<foo attribute=\"value\">content</foo>";
        const string XmlElementWithNamespacedAttributeAndContent = "<foo ns:attribute=\"value\">content</foo>";

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

        [Fact]
        public void TestXmlAttributeSyntaxLeadingTrivia ()
        {
            var element = (IXmlElementSyntax)GetElementSyntax (XmlElementWithAttributeAndContent);
            var attribute = element.Attributes.First();
            var attributeWithTrivia = attribute.WithLeadingTrivia(SyntaxFactory.WhitespaceTrivia(ThreeSpaces));
            Assert.NotSame (attributeWithTrivia, attribute);
            Assert.IsType<XmlAttributeSyntax> (attributeWithTrivia);
            Assert.StartsWith (ThreeSpaces, attributeWithTrivia.ToFullString ());
        }

        [Fact]
        public void TestXmlAttributeSyntaxLeadingTrivia_2TriviaNodes ()
        {
            var element = (IXmlElementSyntax)GetElementSyntax (XmlElementWithNamespacedAttributeAndContent);
            var attribute = element.Attributes.First ();
            var attributeWithTrivia = attribute.WithLeadingTrivia (
                SyntaxFactory.WhitespaceTrivia (Tab),
                SyntaxFactory.WhitespaceTrivia (ThreeSpaces)
            );
            Assert.NotSame (attributeWithTrivia, attribute);
            Assert.IsType<XmlAttributeSyntax> (attributeWithTrivia);
            Assert.StartsWith (Tab + ThreeSpaces, attributeWithTrivia.ToFullString ());
        }

        static XmlElementSyntax GetElementSyntax(string xml) => Parser.ParseText(xml).RootSyntax as XmlElementSyntax;
    }
}
