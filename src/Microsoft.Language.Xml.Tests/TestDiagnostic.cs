using System.Linq;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestDiagnostic
    {
        [Fact]
        public void XmlDeclarationWrongVersion ()
        {
            var root = Parser.ParseText(@"<?xml version=""2.0"" ?>
<root></root>");
            var versionOptionDecl = root?.Prologue?.Version;
            Assert.NotNull(versionOptionDecl);
            Assert.Equal("2.0", versionOptionDecl.Value.TextTokens.ToFullString());
            Assert.True(versionOptionDecl.ContainsDiagnostics);
            Assert.False(root.ContainsDiagnostics);
            var diagnostics = versionOptionDecl.GetDiagnostics();
            Assert.Single(diagnostics);
            Assert.Equal(ERRID.ERR_InvalidAttributeValue1, diagnostics[0].ErrorID);
        }

        [Fact]
        public void MismatchTags()
        {
            var root = Parser.ParseText(@"<?xml version=""1.0"" ?>
<root></toor>");
            Assert.IsType<XmlElementSyntax>(root.Body);
            var node = root.Body as XmlElementSyntax;
            var startTag = node.StartTag;
            var endTag = node.EndTag;
            Assert.Equal("root", startTag.Name);
            Assert.Equal("toor", endTag.Name);
            Assert.False(startTag.ContainsDiagnostics);
            Assert.True(endTag.ContainsDiagnostics);
            var diagnostics = endTag.GetDiagnostics();
            Assert.Single(diagnostics);
            Assert.Equal(ERRID.ERR_MismatchedXmlEndTag, diagnostics[0].ErrorID);
        }
    }
}
