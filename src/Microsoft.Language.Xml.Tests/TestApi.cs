using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Language.Xml.Tests
{
    [TestClass]
    public class TestApi
    {
        [TestMethod]
        public void TestAttributeValue()
        {
            var root = Parser.ParseText("<e a=\"\"/>");
            var attributeValue = root.Attributes.First().Value;
            Assert.AreEqual("", attributeValue);
        }
    }
}
