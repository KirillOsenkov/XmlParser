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

        /// <summary>
        /// 2.2.12 [XML] Section 3.3.3, Attribute-Value Normalization
        /// <seealso href="https://learn.microsoft.com/en-us/openspecs/ie_standards/ms-xml/389b8ef1-e19e-40ac-80de-eec2cd0c58ae" />
        /// </summary>
        [Fact]
        public void TestAttributeValueNormalization()
        {
            const string AllWhitespace = " \n\t";
            var xml = $"<A B=\"{AllWhitespace}X{AllWhitespace}\" />";
            var root = Parser.ParseText(xml)?.RootSyntax;

            var attributeValue = root.Attributes.First().Value;
            Assert.Equal("   X   ", attributeValue);
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

        [Fact]
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
