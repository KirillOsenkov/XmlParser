using System.Linq;
using Xunit;

namespace Microsoft.Language.Xml.Test
{
    public class SyntaxReplacerTests
    {
        [Fact]
        public void TestReplaceNode()
        {
            var original = """
                           <Project Sdk="Microsoft.NET.Sdk">
                             <PropertyGroup>
                               <TargetFramework>net8.0</TargetFramework>
                             </PropertyGroup>
                           </Project>
                           """;

            var expected = """
                           <Project Sdk="Microsoft.NET.Sdk">
                             <PropertyGroup>
                               <TargetFramework>net9.0</TargetFramework>
                             </PropertyGroup>
                           </Project>
                           """;

            XmlDocumentSyntax root = Parser.ParseText(original);
            XmlElementSyntax syntaxToReplace = root
                .Descendants()
                .OfType<XmlElementSyntax>()
                .Single(n => n.Name == "TargetFramework");
            SyntaxNode textSyntaxToReplace = syntaxToReplace.Content.Single();

            XmlTextSyntax content = SyntaxFactory.XmlText(SyntaxFactory.XmlTextLiteralToken("net9.0", null, null));

            root = root.ReplaceNode(textSyntaxToReplace, content);

            Assert.Equal(expected, root.ToFullString());
        }
    }
}