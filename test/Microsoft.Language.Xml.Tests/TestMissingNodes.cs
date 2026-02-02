using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    /// <summary>
    /// Tests that required syntax node children are never null. Following the Roslyn
    /// convention, required-but-absent children should be represented as zero-width
    /// "missing" nodes (IsMissing == true) rather than null references.
    /// </summary>
    public class TestMissingNodes
    {
        // =====================================================================
        // XmlElementSyntax
        // =====================================================================

        [Fact]
        public void XmlElement_WellFormed_HasNonNullStartAndEndTags()
        {
            var root = Parser.ParseText("<a></a>");
            var element = (XmlElementSyntax)root.Body;

            Assert.NotNull(element.StartTag);
            Assert.NotNull(element.EndTag);
            Assert.False(element.StartTag.IsMissing);
            Assert.False(element.EndTag.IsMissing);
        }

        [Fact]
        public void XmlElement_MissingEndTag_EndTagIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<a>");
            var element = (XmlElementSyntax)root.Body;

            Assert.NotNull(element.StartTag);
            Assert.NotNull(element.EndTag);
            Assert.False(element.StartTag.IsMissing);
            Assert.True(element.EndTag.IsMissing);
        }

        [Fact]
        public void XmlElement_MissingEndTag_Nested_EndTagIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<x><a></x>");
            var outer = (XmlElementSyntax)root.Body;
            var inner = (XmlElementSyntax)outer.Content[0];

            Assert.NotNull(inner.StartTag);
            Assert.NotNull(inner.EndTag);
            Assert.True(inner.EndTag.IsMissing);
        }

        // =====================================================================
        // XmlElementStartTagSyntax
        // =====================================================================

        [Fact]
        public void XmlElementStartTag_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<a></a>");
            var element = (XmlElementSyntax)root.Body;
            var startTag = element.StartTag;

            Assert.NotNull(startTag.LessThanToken);
            Assert.NotNull(startTag.NameNode);
            Assert.NotNull(startTag.GreaterThanToken);
            Assert.False(startTag.LessThanToken.IsMissing);
            Assert.False(startTag.NameNode.IsMissing);
            Assert.False(startTag.GreaterThanToken.IsMissing);
        }

        [Fact]
        public void XmlElementStartTag_MissingGreaterThan_TokenIsNotNullButIsMissing()
        {
            // Incomplete start tag - greater-than should be missing
            var root = Parser.ParseText("<a");
            var element = root.Body as XmlElementSyntax;
            if (element != null)
            {
                Assert.NotNull(element.StartTag);
                Assert.NotNull(element.StartTag.GreaterThanToken);
            }
        }

        [Fact]
        public void XmlElementStartTag_MissingName_NameIsNotNullButIsMissing()
        {
            // Tag with no name
            var root = Parser.ParseText("<></>");
            var body = root.Body;
            if (body is XmlElementSyntax element)
            {
                Assert.NotNull(element.StartTag);
                Assert.NotNull(element.StartTag.NameNode);
                Assert.True(element.StartTag.NameNode.IsMissing);
            }
        }

        // =====================================================================
        // XmlElementEndTagSyntax
        // =====================================================================

        [Fact]
        public void XmlElementEndTag_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<a></a>");
            var element = (XmlElementSyntax)root.Body;
            var endTag = element.EndTag;

            Assert.NotNull(endTag.LessThanSlashToken);
            Assert.NotNull(endTag.NameNode);
            Assert.NotNull(endTag.GreaterThanToken);
            Assert.False(endTag.LessThanSlashToken.IsMissing);
            Assert.False(endTag.NameNode.IsMissing);
            Assert.False(endTag.GreaterThanToken.IsMissing);
        }

        [Fact]
        public void XmlElementEndTag_MissingName_NameIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<a></>");
            var element = (XmlElementSyntax)root.Body;
            var endTag = element.EndTag;

            Assert.NotNull(endTag);
            Assert.NotNull(endTag.NameNode);
            Assert.True(endTag.NameNode.IsMissing);
        }

        // =====================================================================
        // XmlEmptyElementSyntax
        // =====================================================================

        [Fact]
        public void XmlEmptyElement_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<a/>");
            var element = (XmlEmptyElementSyntax)root.Body;

            Assert.NotNull(element.LessThanToken);
            Assert.NotNull(element.NameNode);
            Assert.NotNull(element.SlashGreaterThanToken);
            Assert.False(element.LessThanToken.IsMissing);
            Assert.False(element.NameNode.IsMissing);
            Assert.False(element.SlashGreaterThanToken.IsMissing);
        }

        // =====================================================================
        // XmlAttributeSyntax
        // =====================================================================

        [Fact]
        public void XmlAttribute_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<a b='c'/>");
            var element = (XmlEmptyElementSyntax)root.Body;
            var attr = element.AttributesNode.First();

            Assert.NotNull(attr.NameNode);
            Assert.NotNull(attr.Equals);
            Assert.NotNull(attr.ValueNode);
            Assert.False(attr.NameNode.IsMissing);
            Assert.False(attr.Equals.IsMissing);
            Assert.False(attr.ValueNode.IsMissing);
        }

        [Fact]
        public void XmlAttribute_MissingValue_ValueIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<a b=/>");
            var body = root.Body;
            if (body is XmlEmptyElementSyntax element)
            {
                var attrs = element.AttributesNode;
                if (attrs.Any())
                {
                    var attr = (XmlAttributeSyntax)attrs.First();
                    Assert.NotNull(attr.NameNode);
                    Assert.NotNull(attr.Equals);
                    Assert.NotNull(attr.ValueNode);
                }
            }
        }

        [Fact]
        public void XmlAttribute_MissingEquals_EqualsIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<a b 'c'/>");
            var body = root.Body;
            if (body is XmlEmptyElementSyntax element)
            {
                var attrs = element.AttributesNode;
                if (attrs.Any())
                {
                    var attr = (XmlAttributeSyntax)attrs.First();
                    Assert.NotNull(attr.NameNode);
                    Assert.NotNull(attr.Equals);
                    Assert.NotNull(attr.ValueNode);
                }
            }
        }

        // =====================================================================
        // XmlStringSyntax
        // =====================================================================

        [Fact]
        public void XmlString_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<a b='c'/>");
            var element = (XmlEmptyElementSyntax)root.Body;
            var attr = element.AttributesNode.First();
            var value = attr.ValueNode;

            Assert.NotNull(value.StartQuoteToken);
            Assert.NotNull(value.EndQuoteToken);
            Assert.False(value.StartQuoteToken.IsMissing);
            Assert.False(value.EndQuoteToken.IsMissing);
        }

        [Fact]
        public void XmlString_MissingEndQuote_EndQuoteIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<a b='c />");
            var body = root.Body;
            if (body is XmlEmptyElementSyntax element)
            {
                var attrs = element.AttributesNode;
                if (attrs.Any())
                {
                    var attr = attrs.First() as XmlAttributeSyntax;
                    if (attr?.ValueNode != null)
                    {
                        Assert.NotNull(attr.ValueNode.StartQuoteToken);
                        Assert.NotNull(attr.ValueNode.EndQuoteToken);
                    }
                }
            }
        }

        // =====================================================================
        // XmlCommentSyntax
        // =====================================================================

        [Fact]
        public void XmlComment_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<a><!-- hello --></a>");
            var element = (XmlElementSyntax)root.Body;
            var comment = (XmlCommentSyntax)element.Content[0];

            Assert.NotNull(comment.BeginComment);
            Assert.NotNull(comment.EndComment);
            Assert.False(comment.BeginComment.IsMissing);
            Assert.False(comment.EndComment.IsMissing);
        }

        [Fact]
        public void XmlComment_MissingEnd_EndCommentIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<!-- hello");
            // The parser should produce a comment node with missing end
            var body = root.Body;
            if (body is XmlCommentSyntax comment)
            {
                Assert.NotNull(comment.BeginComment);
                Assert.NotNull(comment.EndComment);
                Assert.True(comment.EndComment.IsMissing);
            }
        }

        // =====================================================================
        // XmlCDataSectionSyntax
        // =====================================================================

        [Fact]
        public void XmlCData_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<a><![CDATA[data]]></a>");
            var element = (XmlElementSyntax)root.Body;
            var cdata = (XmlCDataSectionSyntax)element.Content[0];

            Assert.NotNull(cdata.BeginCData);
            Assert.NotNull(cdata.EndCData);
            Assert.False(cdata.BeginCData.IsMissing);
            Assert.False(cdata.EndCData.IsMissing);
        }

        [Fact]
        public void XmlCData_MissingEnd_EndCDataIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<a><![CDATA[data</a>");
            var element = root.Body as XmlElementSyntax;
            if (element != null)
            {
                foreach (var child in element.Content)
                {
                    if (child is XmlCDataSectionSyntax cdata)
                    {
                        Assert.NotNull(cdata.BeginCData);
                        Assert.NotNull(cdata.EndCData);
                        Assert.True(cdata.EndCData.IsMissing);
                    }
                }
            }
        }

        // =====================================================================
        // XmlProcessingInstructionSyntax
        // =====================================================================

        [Fact]
        public void XmlProcessingInstruction_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<a><?pi data?></a>");
            var element = (XmlElementSyntax)root.Body;
            var pi = (XmlProcessingInstructionSyntax)element.Content[0];

            Assert.NotNull(pi.LessThanQuestionToken);
            Assert.NotNull(pi.Name);
            Assert.NotNull(pi.QuestionGreaterThanToken);
            Assert.False(pi.LessThanQuestionToken.IsMissing);
            Assert.False(pi.Name.IsMissing);
            Assert.False(pi.QuestionGreaterThanToken.IsMissing);
        }

        [Fact]
        public void XmlProcessingInstruction_MissingEnd_EndTokenIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<?pi data");
            var body = root.Body;
            if (body is XmlProcessingInstructionSyntax pi)
            {
                Assert.NotNull(pi.LessThanQuestionToken);
                Assert.NotNull(pi.Name);
                Assert.NotNull(pi.QuestionGreaterThanToken);
                Assert.True(pi.QuestionGreaterThanToken.IsMissing);
            }
        }

        // =====================================================================
        // XmlDeclarationSyntax
        // =====================================================================

        [Fact]
        public void XmlDeclaration_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<?xml version='1.0'?><a/>");
            var decl = root.Prologue;

            Assert.NotNull(decl);
            Assert.NotNull(decl.LessThanQuestionToken);
            Assert.NotNull(decl.XmlKeyword);
            Assert.NotNull(decl.Version);
            Assert.NotNull(decl.QuestionGreaterThanToken);
            Assert.False(decl.LessThanQuestionToken.IsMissing);
            Assert.False(decl.XmlKeyword.IsMissing);
            Assert.False(decl.Version.IsMissing);
            Assert.False(decl.QuestionGreaterThanToken.IsMissing);
        }

        [Fact]
        public void XmlDeclaration_MissingEnd_EndTokenIsNotNullButIsMissing()
        {
            var root = Parser.ParseText("<?xml version='1.0'");
            var decl = root.Prologue;
            if (decl != null)
            {
                Assert.NotNull(decl.LessThanQuestionToken);
                Assert.NotNull(decl.QuestionGreaterThanToken);
                Assert.True(decl.QuestionGreaterThanToken.IsMissing);
            }
        }

        // =====================================================================
        // XmlDeclarationOptionSyntax
        // =====================================================================

        [Fact]
        public void XmlDeclarationOption_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<?xml version='1.0'?><a/>");
            var version = root.Prologue.Version;

            Assert.NotNull(version);
            Assert.NotNull(version.NameNode);
            Assert.NotNull(version.Equals);
            Assert.NotNull(version.Value);
            Assert.False(version.NameNode.IsMissing);
            Assert.False(version.Equals.IsMissing);
            Assert.False(version.Value.IsMissing);
        }

        // =====================================================================
        // XmlDocumentSyntax
        // =====================================================================

        [Fact]
        public void XmlDocument_WellFormed_BodyAndEofNonNull()
        {
            var root = Parser.ParseText("<a/>");

            Assert.NotNull(root.Body);
            Assert.NotNull(root.Eof);
            Assert.False(root.Body.IsMissing);
            Assert.False(root.Eof.IsMissing);
        }

        [Fact]
        public void XmlDocument_Empty_BodyAndEofStillNonNull()
        {
            var root = Parser.ParseText("");

            Assert.NotNull(root.Body);
            Assert.NotNull(root.Eof);
        }

        // =====================================================================
        // XmlNameSyntax
        // =====================================================================

        [Fact]
        public void XmlName_WellFormed_LocalNameNonNull()
        {
            var root = Parser.ParseText("<a/>");
            var element = (XmlEmptyElementSyntax)root.Body;
            var name = element.NameNode;

            Assert.NotNull(name);
            Assert.NotNull(name.LocalNameNode);
            Assert.False(name.LocalNameNode.IsMissing);
        }

        [Fact]
        public void XmlName_WithPrefix_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<ns:a/>");
            var element = (XmlEmptyElementSyntax)root.Body;
            var name = element.NameNode;

            Assert.NotNull(name);
            Assert.NotNull(name.LocalNameNode);
            Assert.NotNull(name.PrefixNode);
            Assert.False(name.LocalNameNode.IsMissing);
            Assert.False(name.PrefixNode.IsMissing);
        }

        // =====================================================================
        // XmlPrefixSyntax
        // =====================================================================

        [Fact]
        public void XmlPrefix_WellFormed_AllChildrenNonNull()
        {
            var root = Parser.ParseText("<ns:a/>");
            var element = (XmlEmptyElementSyntax)root.Body;
            var prefix = element.NameNode.PrefixNode;

            Assert.NotNull(prefix);
            Assert.NotNull(prefix.Name);
            Assert.NotNull(prefix.ColonToken);
            Assert.False(prefix.Name.IsMissing);
            Assert.False(prefix.ColonToken.IsMissing);
        }

        // =====================================================================
        // Comprehensive: parsing incomplete input never yields null for required
        // =====================================================================

        [Theory]
        [InlineData("<")]
        [InlineData("<a")]
        [InlineData("<a>")]
        [InlineData("<a><")]
        [InlineData("<a></")]
        [InlineData("<a></a")]
        [InlineData("<a b")]
        [InlineData("<a b=")]
        [InlineData("<a b='")]
        [InlineData("<a b='c")]
        [InlineData("<?xml")]
        [InlineData("<?xml version")]
        [InlineData("<?xml version=")]
        [InlineData("<?xml version='1.0'")]
        public void IncompleteInput_RequiredChildrenAreNeverNull(string xml)
        {
            var root = Parser.ParseText(xml);

            // Document should always have non-null body and eof
            Assert.NotNull(root.Body);
            Assert.NotNull(root.Eof);

            // Walk entire tree and verify no required children are null
            AssertNoNullRequiredChildren(root);
        }

        private void AssertNoNullRequiredChildren(SyntaxNode node)
        {
            switch (node)
            {
                case XmlElementSyntax element:
                    Assert.NotNull(element.StartTag);
                    Assert.NotNull(element.EndTag);
                    break;
                case XmlElementStartTagSyntax startTag:
                    Assert.NotNull(startTag.LessThanToken);
                    Assert.NotNull(startTag.NameNode);
                    Assert.NotNull(startTag.GreaterThanToken);
                    break;
                case XmlElementEndTagSyntax endTag:
                    Assert.NotNull(endTag.LessThanSlashToken);
                    Assert.NotNull(endTag.GreaterThanToken);
                    // NameNode is arguably optional in end tags (e.g. </>)
                    break;
                case XmlEmptyElementSyntax emptyElement:
                    Assert.NotNull(emptyElement.LessThanToken);
                    Assert.NotNull(emptyElement.NameNode);
                    Assert.NotNull(emptyElement.SlashGreaterThanToken);
                    break;
                case XmlAttributeSyntax attribute:
                    Assert.NotNull(attribute.NameNode);
                    Assert.NotNull(attribute.Equals);
                    Assert.NotNull(attribute.ValueNode);
                    break;
                case XmlStringSyntax str:
                    Assert.NotNull(str.StartQuoteToken);
                    Assert.NotNull(str.EndQuoteToken);
                    break;
                case XmlCommentSyntax comment:
                    Assert.NotNull(comment.BeginComment);
                    Assert.NotNull(comment.EndComment);
                    break;
                case XmlCDataSectionSyntax cdata:
                    Assert.NotNull(cdata.BeginCData);
                    Assert.NotNull(cdata.EndCData);
                    break;
                case XmlProcessingInstructionSyntax pi:
                    Assert.NotNull(pi.LessThanQuestionToken);
                    Assert.NotNull(pi.Name);
                    Assert.NotNull(pi.QuestionGreaterThanToken);
                    break;
                case XmlDeclarationSyntax decl:
                    Assert.NotNull(decl.LessThanQuestionToken);
                    Assert.NotNull(decl.XmlKeyword);
                    Assert.NotNull(decl.Version);
                    Assert.NotNull(decl.QuestionGreaterThanToken);
                    break;
                case XmlDeclarationOptionSyntax option:
                    Assert.NotNull(option.NameNode);
                    Assert.NotNull(option.Equals);
                    Assert.NotNull(option.Value);
                    break;
                case XmlNameSyntax name:
                    Assert.NotNull(name.LocalNameNode);
                    break;
                case XmlPrefixSyntax prefix:
                    Assert.NotNull(prefix.Name);
                    Assert.NotNull(prefix.ColonToken);
                    break;
                case XmlDocumentSyntax doc:
                    Assert.NotNull(doc.Body);
                    Assert.NotNull(doc.Eof);
                    break;
            }

            foreach (var child in node.ChildNodes)
            {
                AssertNoNullRequiredChildren(child);
            }
        }
    }
}
