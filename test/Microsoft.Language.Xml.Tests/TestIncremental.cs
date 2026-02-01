using System;
using System.Collections.Generic;
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

        const string Xml2 =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<X sameLengthAttributeName=""foobar"" sameLengthAttributeName=""foobar"">
	<!-- Everything that is a Y node should be reused -->
	<Y>Unrelated node</Y>
    <foo sameLengthAttributeName=""attributeValue"">
		<Y>&amp;</Y>
	</foo>
</X>";

        const string Xml3 =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<X sameLengthAttributeName=""foobar"" sameLengthAttributeName=""foobar"">
	<!-- Everything that is a Y node should be reused -->
	<PotentiallyVeryLongNodeName>Unrelated node</PotentiallyVeryLongNodeName>
    <foo sameLengthAttributeName=""attributeValue"">
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

        [Fact]
        public void IncrementalParsingIsSameAsFullParsing ()
        {
            XmlDocumentSyntax previousDocument = null;
            for (int i = 1; i <= Xml.Length; i++)
            {
                var currentText = Xml.Substring(0, i);
                var full = Parser.ParseText(currentText);
                var incremental = Parser.ParseIncremental(
                    currentText,
                    new[] { new TextChangeRange(new TextSpan(currentText.Length - 1, 0), 1) },
                    previousDocument
                );
                AssertSameNodes(full, incremental);
                previousDocument = incremental;
            }
        }

        [Fact]
        public void IncrementalParsingIsSameAsFullParsing_MiddleEdits()
        {
            var lines = Xml.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var middle = string.Join(Environment.NewLine, lines.Skip(2).Take(lines.Length - 3));
            var delta = lines[0].Length + lines[1].Length + 2 * Environment.NewLine.Length;

            XmlDocumentSyntax previousDocument = null;
            for (int i = 1; i <= middle.Length; i++)
            {
                var currentText =
                    lines[0] + Environment.NewLine
                    + lines[1] + Environment.NewLine
                    + middle.Substring(0, i) + Environment.NewLine
                    + lines.Last ();
                var full = Parser.ParseText(currentText);
                var incremental = Parser.ParseIncremental(
                    currentText,
                    new[] { new TextChangeRange(new TextSpan(delta + i - 1, 0), 1) },
                    previousDocument
                );
                AssertSameNodes(full, incremental);
                previousDocument = incremental;
            }
        }

        [Fact]
        public void IncrementalParsingIsSameAsFullParsing_MultipleConcurrentEdits()
        {
            int[] FindAllIndexes (string str, string needle)
            {
                var result = new List<int>();
                var foundIndex = 0;
                var startSearchAt = 0;
                while ((foundIndex = str.IndexOf (needle, startSearchAt)) != -1)
                {
                    result.Add(foundIndex);
                    startSearchAt = foundIndex + needle.Length;
                }
                return result.ToArray();
            }

            const string AttributeName = "sameLengthAttributeName";
            var attrIndexes = FindAllIndexes(Xml2, AttributeName);
            XmlDocumentSyntax previousDocument = null;
            for (int i = 1; i <= AttributeName.Length; i++)
            {
                var currentText = Xml2;
                var changes = new List<TextChangeRange>();
                // Reconstruct the intermediary attributes
                for (int j = attrIndexes.Length - 1; j >= 0; j--)
                {
                    currentText = currentText.Remove(attrIndexes[j], AttributeName.Length);
                    currentText = currentText.Insert(attrIndexes[j], AttributeName.Substring(0, i));
                    changes.Add(new TextChangeRange(new TextSpan(attrIndexes[j] + i - 1 - j * (AttributeName.Length - i), 0), 1));
                }
                changes.Reverse();

                // All changes should map to the same letter
                Assert.All (changes, c => Assert.Equal (currentText[c.Span.Start], currentText[changes[0].Span.Start]));

                var full = Parser.ParseText(currentText);
                var incremental = Parser.ParseIncremental(
                    currentText,
                    changes.ToArray (),
                    previousDocument
                );
                AssertSameNodes(full, incremental);
                previousDocument = incremental;
            }
        }

        [Theory]
        [InlineData(Xml)]
        [InlineData(Xml2)]
        [InlineData(Xml3)]
        public void IncrementalParsingIsSameAsFullParsing_Paste(string xml)
        {
            var incremental = Parser.ParseText(xml);
            string TextToChange = "Unrelated node";
            string TextToPaste = $"Completely{Environment.NewLine}unrelated{Environment.NewLine}node";
            var startIndex = xml.IndexOf(TextToChange);
            var newXml = xml.Replace(TextToChange, TextToPaste);
            incremental = Parser.ParseIncremental(
                newXml,
                new[] { new TextChangeRange(new TextSpan(startIndex, TextToChange.Length), TextToPaste.Length) },
                incremental);
            var full = Parser.ParseText(newXml);

            AssertSameNodes(full, incremental);
        }

        [Fact]
        public void IncrementalParsingIsSameAsFullParsing_LeadingTriviaEdgeCase ()
        {
            /* The formatting here is important,
             * we are trying to force a situation
             * where the spacing trivia for the leading
             * indentation is stored on the "ios" prefix
             * instead of the attribute name node we are
             * incrementally parsing
             */
            const string Full = @"<Node ios:otherAttr=""foobar""
        android:attribute
        ios:alpha=""1"" />";
            const string IncrementalBase = @"<Node ios:otherAttr=""foobar""
        a
        ios:alpha=""1"" />";

            var full = Parser.ParseText(Full);
            var incremental = Parser.ParseText(IncrementalBase);
            var additionalText = "ndroid:attribute";
            // Complete the attribute one character at a time incrementally
            for (int i = 1; i <= additionalText.Length; i++)
            {
                var insertionIndex = IncrementalBase.IndexOf(" a");
                var newIncrementalText = IncrementalBase.Replace(" a", " a" + additionalText.Substring(0, i));
                var change = new TextChangeRange(new TextSpan(insertionIndex + i + 1, 0), 1);
                incremental = Parser.ParseIncremental(new StringBuffer(newIncrementalText), new[] { change }, incremental);
            }

            Assert.Equal(full.ToFullString(), incremental.ToFullString());
            AssertSameNodes(full, incremental);
        }

        void AssertSameNodes (SyntaxNode root1, SyntaxNode root2)
        {
            var allNodes1 = root1.DescendantNodesAndSelf().GetEnumerator ();
            var allNodes2 = root2.DescendantNodesAndSelf().GetEnumerator ();

            while (true)
            {
                var mn1 = allNodes1.MoveNext();
                var mn2 = allNodes2.MoveNext();
                Assert.False(mn1 ^ mn2, "Different node collection length");

                if (!mn1 && !mn2)
                    return;

                var n1 = allNodes1.Current;
                var n2 = allNodes2.Current;
                Assert.Equal(n1.Kind, n2.Kind);
                Assert.Equal(n1.FullSpan, n2.FullSpan);
            }
        }
    }
}
