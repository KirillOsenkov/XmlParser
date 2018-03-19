using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Language.Xml.Tests
{
    public class TestIncremental
    {
        const string Xml =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<X reusedAttribute=""foobar"" reusedAttr=""foobar"">
	<!-- Everything that is a Y node should be reused -->
	<Y>Unrelated node</Y>
    <foo attributeName=""attributeValue"">
		<Y>&amp;</Y>
	</foo>
</X>";

        [Theory]
        [InlineData("attributeValue", "newAttributeValue")]
        [InlineData("attributeValue", "smallVal")]
        [InlineData("attributeValue", "")]
        [InlineData("attributeValue", "value&quot;other")]
        public void IncrementalAttributeValueChange(string attrValue1, string attrValue2)
        {
            T(attrValue1, attrValue2);
        }

        [Theory]
        [InlineData("attributeName", "newAttributeName")]
        [InlineData("attributeName", "n")]
        [InlineData("attributeName", "")]
        [InlineData("attributeName", "name")]
        public void IncrementalAttributeNameChange(string attrName1, string attrName2)
        {
            T(attrName1, attrName2);
        }

        void T(string originalText, string replacementText)
        {
            var offset = Xml.IndexOf(originalText);
            var length = originalText.Length;
            var newLength = replacementText.Length;
            var newXml = Xml.Replace(originalText, replacementText);
            var changes = new TextChangeRange[] { new TextChangeRange(new TextSpan(offset, length), newLength) };

            var root = Parser.ParseText(Xml);
            var newRoot = Parser.ParseIncremental(newXml, changes, root);

            Assert.NotSame(root, newRoot);
            Assert.Equal(newXml, newRoot.ToFullString());

            AssertShareElementGreenNodesWithName("Y", 2, root.Body, newRoot.Body);
            AssertShareAttributeGreenNodesWithPrefix("reused", 2, root.Body, newRoot.Body);
        }

        void AssertShareElementGreenNodesWithName(string elementName, int expectedCount, XmlNodeSyntax root1, XmlNodeSyntax root2)
        {
            var nodes1 = root1.DescendantsAndSelf();
            var nodes2 = root2.DescendantsAndSelf();
            var combined = nodes1.Zip(nodes2, (n1, n2) => (n1, n2))
                                 .Where(t => t.Item1.NameNode.FullName == elementName)
                                 .ToList();
            Assert.Equal(expectedCount, combined.Count);

            foreach (var node in combined)
                AssertShareGreen((SyntaxNode)node.Item1, (SyntaxNode)node.Item2);
        }

        void AssertShareAttributeGreenNodesWithPrefix(string attributePrefix, int expectedCount, XmlNodeSyntax root1, XmlNodeSyntax root2)
        {
            var attributes1 = root1.DescendantsAndSelf().SelectMany(n => n.Attributes);
            var attributes2 = root2.DescendantsAndSelf().SelectMany(n => n.Attributes);
            var combined = attributes1.Zip(attributes2, (a1, a2) => (a1, a2))
                                      .Where(t => t.Item1.Name.StartsWith(attributePrefix, StringComparison.Ordinal))
                                      .ToList();
            Assert.Equal(expectedCount, combined.Count);

            foreach (var node in combined)
                AssertShareGreen(node.Item1, node.Item2);
        }

        void AssertShareGreen(SyntaxNode node1, SyntaxNode node2)
        {
            var prop = typeof(SyntaxNode).GetProperty("GreenNode", BindingFlags.Instance | BindingFlags.NonPublic);
            var gn1 = prop.GetValue(node1);
            var gn2 = prop.GetValue(node2);

            Assert.Same(gn1, gn2);
        }
    }
}
