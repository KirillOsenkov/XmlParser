using System.Collections.Generic;
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

        public static IEnumerable<object[]> AllErrors() => new ERRID[]
        {
            ERRID.ERR_Syntax,
            ERRID.ERR_IllegalChar,
            ERRID.ERR_ExpectedGreater,
            ERRID.ERR_ExpectedXmlName,
            ERRID.ERR_DuplicateXmlAttribute,
            ERRID.ERR_MismatchedXmlEndTag,
            ERRID.ERR_MissingXmlEndTag,
            ERRID.ERR_MissingVersionInXmlDecl,
            ERRID.ERR_IllegalAttributeInXmlDecl,
            ERRID.ERR_VersionMustBeFirstInXmlDecl,
            ERRID.ERR_AttributeOrder,
            ERRID.ERR_ExpectedSQuote,
            ERRID.ERR_ExpectedQuote,
            ERRID.ERR_ExpectedLT,
            ERRID.ERR_StartAttributeValue,
            ERRID.ERR_IllegalXmlStartNameChar,
            ERRID.ERR_IllegalXmlNameChar,
            ERRID.ERR_IllegalXmlCommentChar,
            ERRID.ERR_ExpectedXmlWhiteSpace,
            ERRID.ERR_IllegalProcessingInstructionName,
            ERRID.ERR_DTDNotSupported,
            ERRID.ERR_IllegalXmlWhiteSpace,
            ERRID.ERR_ExpectedSColon,
            ERRID.ERR_XmlEntityReference,
            ERRID.ERR_InvalidAttributeValue1,
            ERRID.ERR_InvalidAttributeValue2,
            ERRID.ERR_XmlEndCDataNotAllowedInContent,
            ERRID.ERR_XmlEndElementNoMatchingStart,
        }.Select (err => new object[] { err });

        [Theory]
        [MemberData (nameof (AllErrors))]
        public void ErrorHasDiagnosticDescription (ERRID error)
        {
            var diagnostic = ErrorFactory.ErrorInfo(error);
            var desc = diagnostic.GetDescription();
            Assert.NotNull(desc);
            Assert.NotEmpty(desc);
            Assert.Equal(error, diagnostic.ErrorID);
        }
    }
}
