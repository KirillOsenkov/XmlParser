using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestSyntaxLocator
    {
        [Fact]
        public void PositionEmptyElement()
        {
            var (root, position) = GetElementAndPosition("<root><foo $/></root>");
            var node = root.FindNode(position);
            Assert.NotNull(node);
            Assert.Equal(SyntaxKind.WhitespaceTrivia, node.Kind);
            Assert.IsType<XmlEmptyElementSyntax>(node.ParentElement);
            var parent = (XmlEmptyElementSyntax)node.ParentElement;
            Assert.Equal(SyntaxKind.XmlEmptyElement, parent.Kind);
            Assert.Equal("foo", parent.Name);
        }

        [Fact]
        public void PositionElementAfterAttribute()
        {
            var (root, position) = GetElementAndPosition(@"<root><foo attr=""value"" $/></root>");
            var node = root.FindNode(position);
            Assert.NotNull(node);
            Assert.Equal(SyntaxKind.WhitespaceTrivia, node.Kind);
            Assert.IsType<XmlEmptyElementSyntax>(node.ParentElement);
            var parent = (XmlEmptyElementSyntax)node.ParentElement;
            Assert.Equal(SyntaxKind.XmlEmptyElement, parent.Kind);
            Assert.Equal("foo", parent.Name);
        }

        [Fact]
        public void PositionElementInsideAttributeName()
        {
            var (root, position) = GetElementAndPosition(@"<root><foo attr$ /></root>");
            var node = root.FindNode(position);
            Assert.NotNull(node);
            Assert.Equal(SyntaxKind.XmlNameToken, node.Kind);
            Assert.IsType<XmlEmptyElementSyntax>(node.ParentElement);
            var parent = (XmlEmptyElementSyntax)node.ParentElement;
            Assert.Equal(SyntaxKind.XmlEmptyElement, parent.Kind);
            Assert.Equal("foo", parent.Name);
        }

        static (XmlElementSyntax element, int position) GetElementAndPosition(string markedXml)
        {
            var position = markedXml.IndexOf('$');
            var xml = markedXml.Remove(position, 1);
            var element = Parser.ParseText(xml).RootSyntax as XmlElementSyntax;
            return (element, position - 1);
        }
    }
}
