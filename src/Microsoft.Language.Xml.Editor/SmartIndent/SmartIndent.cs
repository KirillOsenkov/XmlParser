using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Language.Xml.Editor
{
    public class SmartIndent : ISmartIndent
    {
        private ITextView textView;
        private readonly ParserService parserService;
        private int indentSize;

        public SmartIndent(ITextView textView, ParserService parserService)
        {
            this.textView = textView;
            this.parserService = parserService;
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            var snapshot = line.Snapshot;
            var treeTask = parserService.GetSyntaxTree(snapshot);

            indentSize = textView.Options.GetOptionValue(DefaultOptions.IndentSizeOptionId);

            treeTask.Wait(100);

            if (!treeTask.IsCompleted)
            {
                return null;
            }

            var root = treeTask.Result;
            var lineStartPosition = line.Start.Position;
            var indent = FindTotalParentChainIndent(
                root,
                lineStartPosition,
                currentPosition: 0,
                indent: 0,
                indentSize: indentSize);
            return indent;
        }

        public static int FindTotalParentChainIndent(SyntaxNode node, int position, int currentPosition, int indent, int indentSize = 2)
        {
            var leading = node.GetLeadingTriviaWidth();
            var trailing = node.GetTrailingTriviaWidth();
            var fullWidth = node.FullWidth;

            if (position < currentPosition + leading || position >= currentPosition + fullWidth - trailing)
            {
                return indent;
            }

            bool isClosingTag = node is XmlElementEndTagSyntax;
            if (isClosingTag && indent >= indentSize)
            {
                return indent - indentSize;
            }

            bool isElementWithAName = node is IXmlElement &&
                !(node is XmlDocumentSyntax) &&
                !string.IsNullOrEmpty(((IXmlElement)node).Name);
            if (isElementWithAName)
            {
                indent += indentSize;
            }

            foreach (var child in node.ChildNodes)
            {
                int childWidth = child.FullWidth;
                if (position < currentPosition + childWidth)
                {
                    int result = indent;
                    result = FindTotalParentChainIndent(child, position, currentPosition, indent, indentSize);

                    return result;
                }
                else
                {
                    currentPosition += childWidth;
                }
            }

            return indent;
        }

        private static int GetLeadingWhitespaceLength(SyntaxNode node)
        {
            if (!(node is IXmlElement))
            {
                return 0;
            }

            var leadingTrivia = node.GetLeadingTrivia();
            if (leadingTrivia == null)
            {
                return 0;
            }

            int totalLength = 0;
            foreach (var child in leadingTrivia)
            {
                if (child.Kind == SyntaxKind.WhitespaceTrivia)
                {
                    totalLength += child.FullWidth;
                }
            }

            return totalLength;
        }

        public void Dispose()
        {
        }
    }
}
