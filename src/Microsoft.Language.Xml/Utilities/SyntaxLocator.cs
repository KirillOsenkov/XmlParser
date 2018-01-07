using System;
using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    public static class SyntaxLocator
    {
        public static TextSpan GetLeadingTriviaSpan(this SyntaxNode node)
        {
            var leadingTrivia = node.GetLeadingTrivia();
            return leadingTrivia != null ?
                new TextSpan(node.Start, leadingTrivia.Width) :
                new TextSpan(node.Start, 0);
        }

        public static TextSpan GetTrailingTriviaSpan(this SyntaxNode node)
        {
            var trailingTrivia = node.GetTrailingTrivia();
            return trailingTrivia != null ?
                new TextSpan(node.Start + node.FullWidth - trailingTrivia.Width, trailingTrivia.Width) :
                new TextSpan(node.Start + node.FullWidth, 0);
        }

        public static SyntaxNode FindNode(
            this SyntaxNode node,
            int position,
            Func<SyntaxNode, bool> descendIntoChildren = null)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            int offset = position;
            bool searchChildren = true;

            while (searchChildren)
            {
                if (descendIntoChildren?.Invoke(node) == false)
                {
                    break;
                }

                // set to false, so loop will only continue if explicitly
                // specified in a later stage
                searchChildren = false;

                int index;
                node.GetIndexAndOffset(offset, out index, out offset);
                if (index > 0)
                {
                    index--;
                }

                var slotCount = node.GetSlotCountIncludingTrivia();

                for (int i = index; i < slotCount; i++)
                {
                    var child = node.GetSlotIncludingTrivia(i);
                    if (child != null)
                    {
                        if (child.Start > position)
                        {
                            // child is after position
                            break;
                        }
                        else if ((child.Start + child.FullWidth) <= position)
                        {
                            // child ends before position, go to next child
                            continue;
                        }
                        else
                        {
                            node = child;
                            searchChildren = true;
                            break;
                        }
                    }
                }
            }

            return node;
        }

        private struct VisitState
        {
            public SyntaxNode node;
            public int i;
        }

        public static IEnumerable<SyntaxNode> Tokens(this SyntaxNode root, TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null)
        {
            VisitState currentState = new VisitState() { node = root };
            Stack<VisitState> stateStack = new Stack<VisitState>();
            stateStack.Push(currentState);

            bool foundFirst = false;

            while (stateStack.Count != 0)
            {
                currentState = stateStack.Pop();

                if (currentState.i == 0 && currentState.node.SlotCount == 0)
                {
                    if (currentState.node.FullSpan.OverlapsWith(span))
                    {
                        foundFirst = true;
                        yield return currentState.node;
                    }
                }
                else if (currentState.i != 0 || (descendIntoChildren?.Invoke(currentState.node) != false))
                {
                    if (!foundFirst && currentState.i == 0)
                    {
                        int offset;
                        currentState.node.GetIndexAndOffset(span.Start - currentState.node.Start, out currentState.i, out offset);
                        if (currentState.i > 0)
                        {
                            // The element is the first element to start after the start position. We want
                            // the first element containing the start position so back track by one to ensure that is
                            // included.
                            currentState.i--;
                        }
                    }

                    while (currentState.i < currentState.node.SlotCount)
                    {
                        var child = currentState.node.GetNodeSlot(currentState.i);
                        currentState.i++;

                        if (child != null)
                        {
                            var childSpan = child.Span;
                            if (childSpan.Start > span.End)
                            {
                                break;
                            }
                            else if (childSpan.End < span.Start)
                            {
                                continue;
                            }
                            else
                            {
                                stateStack.Push(currentState);
                                stateStack.Push(new VisitState() { node = child });
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<SyntaxNode> VisitTerminalsRecursive(this SyntaxNode node, TextSpan span)
        {
            if (node.SlotCount == 0)
            {
                if (node.FullSpan.OverlapsWith(span))
                {
                    yield return node;
                }
            }
            else
            {
                for (int i = 0; i < node.SlotCount; i++)
                {
                    var child = node.GetNodeSlot(i);
                    if (child != null)
                    {
                        if (child.Start > span.End)
                        {
                            break;
                        }
                        else if (child.FullSpan.End < span.Start)
                        {
                            continue;
                        }
                        else
                        {
                            foreach (var terminal in child.VisitTerminalsRecursive(span))
                            {
                                yield return terminal;
                            }
                        }
                    }
                }
            }
        }
    }
}
