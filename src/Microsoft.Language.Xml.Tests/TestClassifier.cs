using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestClassifier
    {
        [Fact]
        public void TestClassifierBasic()
        {
            T("<a></a>",
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [Fact]
        public void ClassifyDeclaration()
        {
            T("<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlAttributeName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeValue,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeValue,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlDelimiter);
        }

        [Fact]
        public void ClassifyNamespaces()
        {
            T("<a:b></a:b>",
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [Fact]
        public void ClassifyEmptyElement()
        {
            T("<a/>",
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [Fact]
        public void TestClassifierAttribute()
        {
            T("<a b=\"c\">t</a>",
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlAttributeName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeValue,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlText,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [Fact]
        public void ClassifierErrorTolerance()
        {
            T("<a><!</a>",
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [Fact]
        public void ClassifierDeepTree()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 30000; i++)
            {
                sb.Append("<br>");
            }

            var xml = sb.ToString();
            T(xml);
        }

        [Fact]
        public void ClassifyWindow()
        {
            T("<root><a/><b/></root>", 6, 4,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [Fact]
        public void ClassifyWindow2()
        {
            T("<root><a/><b/></root>", 7, 4,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter);
        }

        [Fact]
        public void ClassifyWindow3()
        {
            T("<root><a/><b/></root>", 10, 4,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [Fact]
        public void ClassifyWindow4()
        {
            T("<root><ab/><ab/></root>", 8, 5,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName);
        }

        [Fact]
        public void ClassifyWindow5()
        {
            const string xml =
@"<WhitespaceOptInControl> 
	<!-- X --> 
	</WhitespaceOptInControl>";
            int len = xml.Length;
            T(xml, 0, len,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlText,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlComment,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter
            );
        }

        [Fact]
        public void ClassifyAllInOne()
        {
            T(TestParser.allXml,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlAttributeName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeValue,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeValue,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeValue,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlAttributeName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlAttributeQuotes,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlEntityReference,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlText,
                XmlClassificationTypes.XmlEntityReference,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlCDataSection,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlText,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlComment,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        private void T(string xml, params XmlClassificationTypes[] expectedClassifications)
        {
            T(xml, 0, xml.Length, expectedClassifications);
        }

        private void T(string xml, int windowStart, int windowLength, params XmlClassificationTypes[] expectedClassifications)
        {
            var root = Parser.ParseText(xml);
            var actualClassifications = new List<XmlClassificationTypes>();
            int start = windowStart;
            int length = 0;
            ClassifierVisitor.Visit(
                root,
                windowStart,
                windowLength,
                (spanStart, spanLength, spanNode, spanClassification) =>
            {
                Assert.True(spanStart >= start, $"Classified span start ({spanStart}) is less than expected {start}");
                start = spanStart + spanLength;
                length += spanLength;
                actualClassifications.Add(spanClassification);
            });

            //Assert.Equal (windowLength, length);

            if (expectedClassifications != null && expectedClassifications.Length > 0)
            {
                var equal = Enumerable.SequenceEqual(expectedClassifications, actualClassifications);
                var prefix = new string(' ', 16) + "XmlClassificationTypes.";
                var actualText = string.Join(",\r\n", actualClassifications
                    .Select(s => prefix + s)) + ");";
                var differsAt = !equal ? Enumerable.Range(0, System.Math.Min(expectedClassifications.Length, actualClassifications.Count)).First(i => actualClassifications[i] != expectedClassifications[i]) : 0;
                // Clipboard.SetText(actualText);
                Assert.True(equal, "classifications differ at index " + differsAt + " i.e. " + expectedClassifications[differsAt] + " vs " + actualClassifications[differsAt] + ". Actual:\r\n" + actualText);
            }
        }
    }
}
