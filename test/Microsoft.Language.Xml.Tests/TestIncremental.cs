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

        [Fact]
        public void IncrementalParsingIsSameAsFullParsing_AppendingToAttributeName()
        {
            const string xml = "<A><B Change/></A>";
            var full = Parser.ParseText(xml);

            // Attribute name needs to be >= 5 characters to trigger the bug.
            var textToEdit = "Change";
            var insertIndex = xml.IndexOf(textToEdit) + textToEdit.Length;
            var newXml = xml.Insert(insertIndex, "s");
            var change = new TextChangeRange(new TextSpan(insertIndex, 0), 1);
            var incremental = Parser.ParseIncremental(newXml, new[] { change }, full);
            var fullAfterModification = Parser.ParseText(newXml);

            AssertSameNodes(fullAfterModification, incremental);
        }

        [Fact]
        public void IncrementalParsingFuzz()
        {
            // Test that inserting every possible character at every position in
            // the document produces the same result via incremental and full
            // parsing. Uses characters that won't trigger CanParseIncrementally's
            // fallback to full reparse (which skips the incremental code path).
            const string xml =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Root>
  <ns:Element></ns:Element>
  <SelfClosing/>
  <Dotted.Name></Dotted.Name>
  <Node LongAttribute=""value"" Another=""test""></Node>
  <Node ShortAttr/>
  <A>&#x03C0;</A>
  <A>text &amp; more</A>
  <A><![CDATA[bar]]></A>
  <!-- comment -->
</Root>";

            var insertChars = new[] { 'a', 'z', 'M', '0', '9', ' ', '\n', '.', ':', '_', '-', '/' };
            var tree = Parser.ParseText(xml);

            for (int pos = 0; pos <= xml.Length; pos++)
            {
                foreach (var ch in insertChars)
                {
                    var newXml = xml.Insert(pos, ch.ToString());
                    var change = new TextChangeRange(new TextSpan(pos, 0), 1);
                    var incremental = Parser.ParseIncremental(newXml, new[] { change }, tree);
                    var full = Parser.ParseText(newXml);

                    try
                    {
                        AssertSameNodes(full, incremental);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            $"Failed inserting '{ch}' at position {pos}.\n" +
                            $"New XML: {newXml}",
                            ex);
                    }
                }
            }
        }

        [Fact]
        public void IncrementalParsingFuzz_ChainedEdits()
        {
            // Test chained random edits where each incremental parse builds on
            // the previous result. This explores accumulated parser states that
            // single-edit tests won't reach. Uses characters that keep the
            // parser on the incremental path (avoiding <, >, ", ' which cause
            // CanParseIncrementally to fall back to a full reparse).
            const string startXml =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Root>
  <ns:Element></ns:Element>
  <SelfClosing/>
  <Dotted.Name></Dotted.Name>
  <Node LongAttribute=""value"" Another=""test""></Node>
  <Node ShortAttr/>
  <A>&#x03C0;</A>
  <A>text &amp; more</A>
  <A><![CDATA[bar]]></A>
  <!-- comment -->
</Root>";

            var insertChars = new[]
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
                'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                'A', 'B', 'C', 'D', 'E', 'F',
                '0', '1', '2', '9',
                ' ', '\n', '.', ':', '_', '-',
            };
            var random = new Random(42);
            var currentXml = startXml;
            var currentTree = Parser.ParseText(currentXml);
            const int iterations = 500;

            for (int i = 0; i < iterations; i++)
            {
                string newXml;
                TextChangeRange change;

                if (currentXml.Length > 5 && random.Next(3) == 0)
                {
                    // Delete a character
                    var deleteIndex = random.Next(currentXml.Length);
                    newXml = currentXml.Remove(deleteIndex, 1);
                    change = new TextChangeRange(new TextSpan(deleteIndex, 1), 0);
                }
                else
                {
                    // Insert a character
                    var insertIndex = random.Next(currentXml.Length + 1);
                    var ch = insertChars[random.Next(insertChars.Length)];
                    newXml = currentXml.Insert(insertIndex, ch.ToString());
                    change = new TextChangeRange(new TextSpan(insertIndex, 0), 1);
                }

                var incremental = Parser.ParseIncremental(newXml, new[] { change }, currentTree);
                var full = Parser.ParseText(newXml);

                try
                {
                    AssertSameNodes(full, incremental);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"Fuzz iteration {i} failed.\n" +
                        $"Previous XML: {currentXml}\n" +
                        $"New XML: {newXml}\n" +
                        $"Change: Span({change.Span.Start}, {change.Span.Length}) NewLength={change.NewLength}",
                        ex);
                }

                currentXml = newXml;
                currentTree = incremental;
            }
        }

        [Fact]
        public void IncrementalIndentAwareness_InsertSameNameChild()
        {
            // Simulate inserting a new <Item> inside an existing <Item> element.
            var before = string.Join(Environment.NewLine, new[] {
                "<Root>",
                "  <Item>",
                "    <Foo />",
                "  </Item>",
                "</Root>"
            });
            var after = string.Join(Environment.NewLine, new[] {
                "<Root>",
                "  <Item>",
                "    <Item>",
                "    <Foo />",
                "  </Item>",
                "</Root>"
            });

            var insertedText = "    <Item>" + Environment.NewLine;
            var insertionPoint = before.IndexOf("    <Foo");

            var beforeTree = Parser.ParseText(before);
            var afterTreeFull = Parser.ParseText(after);
            var afterTreeIncremental = Parser.ParseIncremental(
                after,
                new[] { new TextChangeRange(new TextSpan(insertionPoint, 0), insertedText.Length) },
                beforeTree);

            Assert.Equal(after, afterTreeIncremental.ToFullString());
            AssertSameNodes(afterTreeFull, afterTreeIncremental);
        }

        [Fact]
        public void IncrementalIndentAwareness_TypeEndTagCharByChar()
        {
            // Simulate typing "</Item>" character by character inside a same-name parent.
            var baseText = string.Join(Environment.NewLine, new[] {
                "<Root>",
                "  <Item>",
                "    <Item>",
                "  ",
                "</Root>"
            });

            // The line "  " is where we'll type "</Item>" one char at a time.
            var insertionPoint = baseText.IndexOf("  " + Environment.NewLine + "</Root>") + 2;
            var textToType = "</Item>";
            var previousDocument = Parser.ParseText(baseText);

            for (int i = 1; i <= textToType.Length; i++)
            {
                var currentText = baseText.Insert(insertionPoint, textToType.Substring(0, i));
                var change = new TextChangeRange(new TextSpan(insertionPoint + i - 1, 0), 1);
                var full = Parser.ParseText(currentText);
                var incremental = Parser.ParseIncremental(currentText, new[] { change }, previousDocument);

                Assert.Equal(currentText, incremental.ToFullString());
                AssertSameNodes(full, incremental);
                previousDocument = incremental;
            }
        }

        [Fact]
        public void IncrementalIndentAwareness_ChangeIndentation()
        {
            // Change the indentation of an end tag so it matches a different start tag.
            var before = string.Join(Environment.NewLine, new[] {
                "<Root>",
                "  <A>",
                "    <A>",
                "    </A>",
                "</Root>"
            });
            var after = string.Join(Environment.NewLine, new[] {
                "<Root>",
                "  <A>",
                "    <A>",
                "  </A>",
                "</Root>"
            });

            // We're removing 2 spaces from "    </A>" to make it "  </A>"
            var changeStart = before.IndexOf("    </A>");
            var beforeTree = Parser.ParseText(before);
            var afterTreeFull = Parser.ParseText(after);
            var afterTreeIncremental = Parser.ParseIncremental(
                after,
                new[] { new TextChangeRange(new TextSpan(changeStart, 2), 0) },
                beforeTree);

            Assert.Equal(after, afterTreeIncremental.ToFullString());
            AssertSameNodes(afterTreeFull, afterTreeIncremental);
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
