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

        [Fact]
        public void TestWithoutLeadingTrivia ()
        {
            var element = GetElementSyntax(ThreeSpaces + XmlElementWithAttributeAndContent);
            Assert.NotNull(element);
            Assert.True (element.HasLeadingTrivia);
            Assert.Equal(ThreeSpaces, element.GetLeadingTrivia().First().Text);
            var newElement = element.WithoutLeadingTrivia();
            Assert.NotSame(element, newElement);
            Assert.False(newElement.HasLeadingTrivia);
            Assert.StartsWith("<", newElement.ToFullString());
        }

        [Fact]
        public void TestWithoutTrailingTrivia ()
        {
            var element = GetElementSyntax (
                XmlElementWithAttributeAndContent.Substring (0, XmlElementWithAttributeAndContent.Length - 1) + ThreeSpaces + ">"
            );
            Assert.NotNull (element);
            var endTagName = element.EndTag.NameNode;
            Assert.True(endTagName.HasTrailingTrivia);
            Assert.Equal (ThreeSpaces, endTagName.GetTrailingTrivia ().First ().Text);
            Assert.EndsWith ("foo" + ThreeSpaces, endTagName.ToFullString ());
            var newTagName = endTagName.WithoutTrailingTrivia ();
            Assert.NotSame (endTagName, newTagName);
            Assert.False (newTagName.HasTrailingTrivia);
            Assert.EndsWith ("foo", newTagName.ToFullString ());
        }

        static XmlElementSyntax GetElementSyntax(string xml) => Parser.ParseText(xml).RootSyntax as XmlElementSyntax;
    }
}
