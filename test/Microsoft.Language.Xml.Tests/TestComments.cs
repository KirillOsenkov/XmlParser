using System;
using System.Linq;
using Microsoft.Language.Xml.Comments;
using Microsoft.Language.Xml.Tests.Properties;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestComments
    {
        [Fact]
        public void CommentElementContent()
        {
            TC(Resources.TestXml, TextSpan.FromBounds(199, 209),
                TextSpan.FromBounds(199, 209));

            // Test at edges xml element content
            TC(Resources.TestXml, TextSpan.FromBounds(276, 350),
                TextSpan.FromBounds(276, 350));
        }

        [Fact]
        public void CommentSingleCharacter()
        {
            TC(Resources.TestXml, TextSpan.FromBounds(722, 723),
                TextSpan.FromBounds(722, 723));
        }

        [Fact]
        public void CommentExpandsExcludingElementWhitespace()
        {
            TC(Resources.TestXml, TextSpan.FromBounds(451, 453),
                TextSpan.FromBounds(444, 513));
        }

        [Fact]
        public void CommentMultipleElements()
        {
            TC(Resources.TestXml, TextSpan.FromBounds(753, 912),
                TextSpan.FromBounds(740, 919));

            TC(Resources.TestXml, TextSpan.FromBounds(823, 911),
                TextSpan.FromBounds(818, 919));
        }

        [Fact]
        public void CommentDeclaration()
        {
            TC(Resources.TestXml, TextSpan.FromBounds(22, 25),
                TextSpan.FromBounds(0, 39));
        }

        [Fact]
        public void CommentedDeclaration()
        {
            TU(Resources.TestXml.Insert(39, "-->").Insert(0, "<!--"), new TextSpan(22, 25),
                TextSpan.FromBounds(0, 46));
        }

        [Fact]
        public void CommentedEdge()
        {
            TU(Resources.TestXml, new TextSpan(436, 0),
                TextSpan.FromBounds(411, 436));
        }

        [Fact]
        public void CommentExcludesComments()
        {
            // Selection inside comment
            TC(Resources.TestXml, TextSpan.FromBounds(550, 555));

            TC(Resources.TestXml, TextSpan.FromBounds(391, 436),
                TextSpan.FromBounds(391, 392));

            TC(Resources.TestXml, TextSpan.FromBounds(370, 588),
                TextSpan.FromBounds(370, 392),
                TextSpan.FromBounds(436, 542),
                TextSpan.FromBounds(563, 595)
                );
        }

        private void TC(string xml, TextSpan commentSpan, params TextSpan[] expectedSpans)
        {
            TestCore(xml, commentSpan, expectedSpans, commented: false);
        }

        private void TU(string xml, TextSpan commentSpan, params TextSpan[] expectedSpans)
        {
            TestCore(xml, commentSpan, expectedSpans, commented: true);
        }

        private static void TestCore(string xml, TextSpan commentSpan, TextSpan[] expectedSpans, bool commented)
        {
            var root = Parser.ParseText(xml);

            var actualSpans = commented ?
                root.GetCommentedSpans(commentSpan).ToList() :
                root.GetValidCommentSpans(commentSpan).ToList();

            for (int i = 0; i < Math.Min(expectedSpans.Length, actualSpans.Count); i++)
            {
                Assert.Equal(expectedSpans[i], actualSpans[i]);
            }

            Assert.Equal(expectedSpans.Length, actualSpans.Count);
        }
    }
}
