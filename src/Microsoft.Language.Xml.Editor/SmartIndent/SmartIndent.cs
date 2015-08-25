using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Language.Xml.Editor
{
    public class SmartIndent : ISmartIndent
    {
        private ITextView textView;
        private readonly ParserService parserService;

        public SmartIndent(ITextView textView, ParserService parserService)
        {
            this.textView = textView;
            this.parserService = parserService;
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            var snapshot = line.Snapshot;
            var treeTask = parserService.GetSyntaxTree(snapshot);
            treeTask.Wait(100);

            if (!treeTask.IsCompleted)
            {
                return null;
            }

            var root = treeTask.Result;
            var lineStartPosition = line.Start.Position;
            var indent = FindTotalParentChainIndent(root, lineStartPosition, 0, 0);
            return indent;
        }

        public static int FindTotalParentChainIndent(SyntaxNode node, int position, int currentPosition, int indent)
        {
            var leading = node.GetLeadingTriviaWidth();
            var trailing = node.GetTrailingTriviaWidth();
            var fullWidth = node.FullWidth;

            if (position < currentPosition + leading || position >= currentPosition + fullWidth - trailing)
            {
                return indent;
            }

            if (node is IXmlElement && !string.IsNullOrEmpty(((IXmlElement)node).Name))
            {
                indent += 4;
            }

            foreach (var child in node.ChildNodes)
            {
                int childWidth = child.FullWidth;
                if (position < currentPosition + childWidth)
                {
                    int result = indent;
                    result = FindTotalParentChainIndent(child, position, currentPosition, indent);

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
            foreach (var child in leadingTrivia.ChildNodes)
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
