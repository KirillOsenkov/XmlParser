using System.Linq;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestApi
    {
        [Fact]
        public void TestAttributeValue()
        {
            var root = Parser.ParseText("<e a=\"\"/>");
            var attributeValue = root.Attributes.First().Value;
            Assert.Equal("", attributeValue);
        }

        [Fact]
        public void TestContent()
        {
            var root = Parser.ParseText("<e>Content</e>");
            var value = root.Value;
            Assert.Equal("Content", value);
        }
    }
}
