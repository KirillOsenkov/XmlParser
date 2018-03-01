using System.Linq;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestApi
    {
        [Fact]
        public void TestAttributeValue()
        {
            var root = Parser.ParseText("<e a=\"\"/>")?.RootSyntax;
            var attributeValue = root.Attributes.First().Value;
            Assert.Equal("", attributeValue);
        }

        [Fact]
        public void TestContent()
        {
            var root = Parser.ParseText("<e>Content</e>")?.Root;
            var value = root.Value;
            Assert.Equal("Content", value);
        }

        [Fact]
        public void TestRootLevel()
        {
            var root = Parser.ParseText("<Root></Root>")?.Root;
            Assert.Equal("Root", root.Name);
        }

        [Fact(Skip = "https://github.com/KirillOsenkov/XmlParser/issues/8")]
        public void TestRootLevelTrivia()
        {
            var root = Parser.ParseText("<!-- C --><Root></Root>")?.Root;
            Assert.Equal("Root", root.Name);
        }

        [Fact]
        public void TestRootLevelTriviaWithDeclaration()
        {
            var root = Parser.ParseText("<?xml version=\"1.0\" encoding=\"utf-8\"?><!-- C --><Root></Root>")?.Root;
            Assert.Equal("Root", root.Name);
        }
    }
}
