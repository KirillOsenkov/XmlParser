using System;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestAnnotations
    {
        private static readonly SyntaxAnnotation annotation = new SyntaxAnnotation("test");

        [Fact]
        public void AddingContentToElementPreservesAnnotations()
        {
            var root = GetRootElementSyntax("<root><child/></root>");
            var child = Parser.ParseText("<child2/>").RootSyntax;

            AssertAnnotation(root.AddChild(child));
        }

        [Fact]
        public void AddingChildToEmptyElementPreservesAnnotations()
        {
            var root = GetRootEmptyElementSyntax("<root/>");
            var child = Parser.ParseText("<child2/>").RootSyntax;

            AssertAnnotation(root.AddChild(child));
        }

        [Fact]
        public void AddingElementTriviaPreservesAnnotations()
        {
            var root = GetRootElementSyntax("<root></root>");
            var trivia = SyntaxFactory.WhitespaceTrivia(" ");

            AssertAnnotation(root.WithLeadingTrivia(trivia));
        }

        [Fact]
        public void AddingEmptyElementTriviaPreservesAnnotations()
        {
            var root = GetRootEmptyElementSyntax("<root/>");
            var trivia = SyntaxFactory.WhitespaceTrivia(" ");

            AssertAnnotation(root.WithLeadingTrivia(trivia));
        }

        private static void AssertAnnotation<T>(T element) where T : SyntaxNode
        {
            Assert.True(element.ContainsAnnotations);
            element.HasAnnotation(annotation);
        }

        private static void AssertAnnotation(IXmlElementSyntax element)
        {
            AssertAnnotation((SyntaxNode)element);
        }

        private static XmlElementSyntax GetRootElementSyntax(string xml)
        {
            return Assert.IsType<XmlElementSyntax>(Parser.ParseText(xml).RootSyntax)
                .WithAdditionalAnnotations(annotation);
        }

        private static XmlEmptyElementSyntax GetRootEmptyElementSyntax(string xml)
        {
            return Assert.IsType<XmlEmptyElementSyntax>(Parser.ParseText(xml).RootSyntax)
                .WithAdditionalAnnotations(annotation);
        }
    }
}
