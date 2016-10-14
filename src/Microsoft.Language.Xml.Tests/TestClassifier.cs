using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public void T(string xml, params XmlClassificationTypes[] expectedClassifications)
        {
            var root = Parser.ParseText(xml);
            var actualClassifications = new List<XmlClassificationTypes>();
            ClassifierVisitor.Visit(root, 0, xml.Length, (s, e, n, c) => actualClassifications.Add(c));

            if (expectedClassifications != null && expectedClassifications.Length > 0)
            {
                var equal = Enumerable.SequenceEqual(expectedClassifications, actualClassifications);
                Assert.IsTrue(equal, "classifications differ. Actual: " + string.Join(",\r\n", actualClassifications));
            }
        }
    }
}
