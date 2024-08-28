using System;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable CS8602
#pragma warning disable CS8604

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    internal sealed class Blender : Scanner
    {
        private readonly Stack<GreenNode> _nodeStack = new Stack<GreenNode>();
        private readonly TextChangeRange[]? _changes;
        private readonly TextSpan[]? _affectedRanges;
        private GreenNode? _currentNode;
        private int _curNodeStart;
        private int _curNodeLength;
        private readonly SyntaxNode _baseTreeRoot;

        internal Blender(Buffer newText, TextChangeRange[] changes, SyntaxNode root) : base(newText)
        {
            _baseTreeRoot = root;
            _currentNode = root.GreenNode;
            _curNodeStart = 0;
            _curNodeLength = 0;
            TryCrumbleOnce();
            if (_currentNode == null)
                return;
            _changes = changes;
            _affectedRanges = ExpandToParentChain(_baseTreeRoot, _changes);
        }

        /// <summary>
        /// Foreach changes that occured, include every start element that contains them recursively
        /// so that those elements are not automatically picked up from the cached syntaxes
        /// </summary>
        /// <remarks>The changes parameter is assumed to be sorted in document order</remarks>
        /// <returns>The list of text spans where syntax node are stale in newText coordinates</returns>
        private static TextSpan[] ExpandToParentChain(SyntaxNode root, TextChangeRange[] changes)
        {
            var allSpans = new HashSet<TextSpan>();

            for (int i = 0; i < changes.Length; i++)
            {
                var change = changes[i];
                var node = root.FindNode(change.Span.Start, includeTrivia: false);
                MarkNodeHierarchyDirty(allSpans, node, changes, i);
                /* Check if the position maps to the start of a node with leading trivia
                 * or if the change affected the tail end of the buffer. In both those
                 * cases it's more likely that the node affected by the change is
                 * actually the previous one. In that situation we also mark that chain
                 * as well so that we don't end up with an incorrect parsing
                 */
                if ((change.Span.Start > 0 && node.HasLeadingTrivia && node.Start == change.Span.Start)
                    || change.Span.Start >= root.FullWidth)
                {
                    node = root.FindNode(change.Span.Start - 1, includeTrivia: false);
                    MarkNodeHierarchyDirty(allSpans, node, changes, i);
                }

                // Add the changed node span itself
                var changeSpan = new TextSpan(change.Span.Start, change.NewLength);
                // expand it by the lookahead and lookbehind values
                allSpans.Add(ExpandByLookAheadAndBehind(root, changeSpan));
            }

            var spans = new TextSpan[allSpans.Count];
            allSpans.CopyTo(spans);
            Array.Sort(spans);

            return spans;
        }

        private static void MarkNodeHierarchyDirty (HashSet<TextSpan> allSpans, SyntaxNode node, TextChangeRange[] changes, int currentChangeIndex)
        {
            /* Find all parent of the node and mark the '<' of their start element
             * or the start of the whole attribute as dirty
             */
            foreach (var parent in node.GetParentElementsAndAttributes())
            {
                var parentStart = parent.Start;
                for (int j = currentChangeIndex - 1; j >= 0; j--)
                {
                    var previousChange = changes[j];
                    if (previousChange.Span.Start < parentStart)
                        parentStart += previousChange.NewLength - previousChange.Span.Length;
                }
                allSpans.Add (new TextSpan(parentStart, 0));
            }
            // Add the node
            allSpans.Add(new TextSpan(node.Span.Start, 0));
        }

        private static TextSpan ExpandByLookAheadAndBehind (SyntaxNode root, TextSpan span)
        {
            var fullWidth = root.FullWidth;
            var start = Math.Max(0, span.Start - Scanner.MaxTokensLookAheadBeyondEOL);
            var end = Math.Min(fullWidth - 1, span.End + Scanner.MaxCharsLookBehind);
            return TextSpan.FromBounds(start, end);
        }

        internal override GreenNode? GetCurrentSyntaxNode()
        {
            if (_currentNode == null)
                return null;
            var start = _currentToken.Position;

            if (_affectedRanges.AnyContainsPosition(start))
                return null;
            var nonterminal = GetCurrentNode(start);
            return nonterminal;
        }

        internal override bool TryCrumbleOnce()
        {
            if (_currentNode == null)
                return false;
            if (_currentNode.SlotCount == 0)
            {
                // We only care about non-terminals
                return false;
            }
            if (!ShouldCrumble(_currentNode))
                return false;
            PushReverseNonterminal(_nodeStack, _currentNode);

            _curNodeLength = 0;
            //_nextPreprocessorStateGetter = default (NextPreprocessorStateGetter);
            return TryPopNode();
        }

        private static void PushReverseNonterminal(Stack<GreenNode> stack, GreenNode nonterminal)
        {
            var cnt = nonterminal.SlotCount;
            for (int i = 1; i <= cnt; i++)
            {
                var child = nonterminal.GetSlot(cnt - i);
                PushChildReverse(stack, child);
            }
        }

        private static void PushReverseTerminal(Stack<GreenNode> stack, SyntaxToken.Green tk)
        {
            var trivia = tk.GetTrailingTrivia();
            if (trivia != null)
                PushChildReverse(stack, trivia);
            PushChildReverse(stack, tk.WithLeadingTrivia(null).WithTrailingTrivia(null));
            trivia = tk.GetLeadingTrivia();
            if (trivia != null)
                PushChildReverse(stack, trivia);
        }

        private static void PushChildReverse(Stack<GreenNode> stack, GreenNode child)
        {
            if (child != null)
            {
                if (child.IsList)
                    PushReverseNonterminal(stack, child);
                else
                    stack.Push(child);
            }
        }

        private int MapNewPositionToOldTree(int position)
        {
            foreach (var change in _changes)
            {
                if (position < change.Span.Start)
                    return position;
                if (position >= change.Span.Start + change.NewLength)
                    return position - change.NewLength + change.Span.Length;
            }
            return -1;
        }

        private bool TryPopNode()
        {
            if (_nodeStack.Count > 0)
            {
                var node = _nodeStack.Pop();
                _currentNode = node;
                _curNodeStart = _curNodeStart + _curNodeLength;
                _curNodeLength = node.FullWidth;
                return true;
            }
            else
            {
                _currentNode = null;
                return false;
            }
        }

        private static bool ShouldCrumble(GreenNode node) => true;

        private GreenNode? GetCurrentNode(int position)
        {
            Debug.Assert(_currentNode != null);
            var mappedPosition = MapNewPositionToOldTree(position);
            if (mappedPosition == -1)
                return null;

            do
            {
                if (_curNodeStart > mappedPosition)
                    return null;
                if ((_curNodeStart + _curNodeLength) <= mappedPosition)
                {
                    if (TryPopNode())
                        continue;
                    return null;
                }
                if (_curNodeStart == mappedPosition && CanReuseNode(_currentNode))
                    break;
                if (!TryCrumbleOnce())
                    return null;
            } while (true);

            Debug.Assert(_currentNode.FullWidth > 0, "reusing zero-length nodes?");
            return _currentNode;
        }

        private bool CanReuseNode(GreenNode? node)
        {
            if (node == null)
                return false;
            if (node.SlotCount == 0)
                return false;
            /*if (node.ContainsDiagnostics)
				return false;
			if (node.ContainsAnnotations)
				return false;*/
            /*var _curNodeSpan = new TextSpan(_curNodeStart, _curNodeLength);
            Debug.Assert(_curNodeSpan.Length > 0);
            if (_curNodeSpan.OverlapsWithAny(_affectedRanges))
                return false;*/
            if (_currentNode.IsMissing)
                return false;
            return true;
        }

        internal override void MoveToNextSyntaxNode(ScannerState withState)
        {
            if (_currentNode == null)
                return;
            Debug.Assert(CanReuseNode(_currentNode), "this node could not have been used.");
            _lineBufferOffset = _currentToken.Position + _curNodeLength;
            base.MoveToNextSyntaxNode(withState);
            TryPopNode();
        }
    }
}
