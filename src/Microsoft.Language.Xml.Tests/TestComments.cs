using System;
using System.Linq;
using Microsoft.Language.Xml.Comments;
using Microsoft.Language.Xml.Tests.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Language.Xml.Tests
{
    [TestClass]
    public class TestComments
    {
        [TestMethod]
        public void TestCommentElementContent()
        {
            T(Resources.TestXml, TextSpan.FromBounds(199, 209),
                TextSpan.FromBounds(199, 209));

            // Test at edges xml element content 
            T(Resources.TestXml, TextSpan.FromBounds(276, 350),
                TextSpan.FromBounds(276, 350));
        }

        [TestMethod]
        public void TestExcludesComments()
        {
            T(Resources.TestXml, TextSpan.FromBounds(391, 436),
                TextSpan.FromBounds(391, 392));

            T(Resources.TestXml, TextSpan.FromBounds(370, 588),
                TextSpan.FromBounds(370, 392),
                TextSpan.FromBounds(436, 542),
                TextSpan.FromBounds(563, 595)
                );
        }

        public void T(string xml, TextSpan commentSpan, params TextSpan[] expectedSpans)
        {
            var root = Parser.ParseText(xml);

            var actualSpans = root.GetValidCommentSpans(commentSpan).ToList();

            for (int i = 0; i < Math.Min(expectedSpans.Length, actualSpans.Count); i++)
            {
                Assert.AreEqual(expectedSpans[i], actualSpans[i]);
            }

            Assert.AreEqual(expectedSpans.Length, actualSpans.Count);
        }
    }
}
