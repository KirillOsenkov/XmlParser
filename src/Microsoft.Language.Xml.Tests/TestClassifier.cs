using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Language.Xml.Test
{
    [TestClass]
    public class TestClassifier
    {
        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void ClassifyEmptyElement()
        {
            T("<a/>",
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void ClassifyWindow()
        {
            T("<a/><b/>", 0, 4,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [TestMethod]
        public void ClassifyWindow2()
        {
            T("<a/><b/>", 1, 4,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter);
        }

        [TestMethod]
        public void ClassifyWindow3()
        {
            T("<a/><b/>", 4, 4,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        [TestMethod]
        public void ClassifyWindow4()
        {
            T("<ab/><ab/>", 2, 5,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName);
        }

        [TestMethod]
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
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlComment,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlDelimiter,
                XmlClassificationTypes.XmlName,
                XmlClassificationTypes.XmlDelimiter);
        }

        public void T(string xml, params XmlClassificationTypes[] expectedClassifications)
        {
            T(xml, 0, xml.Length, expectedClassifications);
        }

        public void T(string xml, int windowStart, int windowLength, params XmlClassificationTypes[] expectedClassifications)
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
                Assert.IsTrue(spanStart >= start, $"Classified span start ({spanStart}) is less than expected {start}");
                start = spanStart + spanLength;
                length += spanLength;
                actualClassifications.Add(spanClassification);
            });

            Assert.AreEqual(windowLength, length);

            if (expectedClassifications != null && expectedClassifications.Length > 0)
            {
                var equal = Enumerable.SequenceEqual(expectedClassifications, actualClassifications);
                var prefix = new string(' ', 16) + "XmlClassificationTypes.";
                var actualText = string.Join(",\r\n", actualClassifications
                    .Select(s => prefix + s)) + ");";
                Clipboard.SetText(actualText);
                Assert.IsTrue(equal, "classifications differ. Actual:\r\n" + actualText);
            }
        }
    }
}
