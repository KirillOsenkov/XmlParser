using System.Linq;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestModification
    {
        [Fact]
        public void TestEmptyElementWithContent ()
        {
            var root = Parser.ParseText ("<a/>")?.RootSyntax;
            var content = Parser.ParseText ("<b />")?.RootSyntax;
            root = root.WithContent (SyntaxFactory.SingletonList (content.AsNode));
            Assert.Equal("<a><b /></a>", root.ToFullString());
        }

        [Fact]
        public void TestElementWithAttributeAndContent()
        {
            var root = Parser.ParseText("<a b=\"c\"/>")?.RootSyntax;
            var content = Parser.ParseText("<d />")?.RootSyntax;
            root = root.WithContent(SyntaxFactory.SingletonList(content.AsNode));
            Assert.Equal("<a b=\"c\"><d /></a >", root.ToFullString());
        }
    }
}
