using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Microsoft.Language.Xml
{
    public abstract class SyntaxNode
    {
        public SyntaxKind Kind { get; protected set; }
        public SyntaxNode Parent { get; internal set; }
        public int Start { get; internal set; }

        private byte slotCount;
        private int fullWidth;

        public SyntaxNode(SyntaxKind kind)
        {
            this.Kind = kind;
            this.fullWidth = -1;
        }

        public int FullWidth
        {
            get
            {
                if (fullWidth == -1)
                {
                    throw new InvalidOperationException();
                }

                return fullWidth;
            }
        }

        public virtual int Width
        {
            get
            {
                return FullWidth - (GetLeadingTriviaWidth() + GetTrailingTriviaWidth());
            }
        }

        public TextSpan Span
        {
            get
            {
                return new TextSpan(Start + GetLeadingTriviaWidth(), Width);
            }
        }

        public TextSpan FullSpan
        {
            get
            {
                return new TextSpan(Start, FullWidth);
            }
        }

        public int End => Start + FullWidth;

        public virtual int GetSlotCountIncludingTrivia()
        {
            return SlotCount;
        }

        public virtual SyntaxNode GetSlotIncludingTrivia(int index)
        {
            return GetSlot(index);
        }

        public abstract SyntaxNode Accept(SyntaxVisitor visitor);

        private struct ComputeFullWidthState
        {
            public SyntaxNode Node;
            public SyntaxNode Parent;
            public int Index;
        }

        internal int ComputeFullWidthIterative(int start = 0)
        {
            return ComputeFullWidthIterative(this, start);
        }

        protected virtual int GetTextWidth()
        {
            return 0;
        }

        private static int ComputeFullWidthIterative(SyntaxNode node, int start = 0)
        {
            if (node.fullWidth >= 0)
            {
                return node.fullWidth;
            }

            node.Start = start;

            Stack<ComputeFullWidthState> nodes = new Stack<ComputeFullWidthState>();
            nodes.Push(new ComputeFullWidthState()
            {
                Node = node
            });

            while (nodes.Count != 0)
            {
                var state = nodes.Pop();

                AfterPopState:
                node = state.Node;
                if (node.fullWidth == -1)
                {
                    node.fullWidth = node.GetTextWidth();

                    // Node full width is now zero or the node is a token/trivial
                    // and the full width represents the width of the text
                    // for the token/trivia. Therefore, start is only incremented
                    // for tokens and trivia.
                    start += node.fullWidth;
                }

                var parent = state.Parent;

                var slotCount = node.GetSlotCountIncludingTrivia();
                for (; state.Index < slotCount; state.Index++)
                {
                    var child = node.GetSlotIncludingTrivia(state.Index);
                    if (child == null)
                    {
                        continue;
                    }

                    child.Parent = node;
                    child.Start = start;

                    if (child.fullWidth == -1)
                    {
                        state.Index++;
                        nodes.Push(state);

                        state = new ComputeFullWidthState()
                        {
                            Node = child,
                            Parent = node
                        };

                        goto AfterPopState;
                    }
                    else
                    {
                        start += child.fullWidth;
                        node.fullWidth += child.fullWidth;
                    }
                }

                if (parent != null)
                {
                    if (parent.fullWidth == -1)
                    {
                        parent.fullWidth = 0;
                    }

                    parent.fullWidth += node.fullWidth;
                }
            }

            return node.fullWidth;
        }

        private int ComputeFullWidth()
        {
            int width = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                var slot = GetSlot(i);
                if (slot != null)
                {
                    width += slot.FullWidth;
                }
            }

            return width;
        }

        // Get the leading trivia a green array, recursively to first token.
        public virtual SyntaxNode GetLeadingTrivia()
        {
            var possibleFirstChild = GetFirstToken();
            if (possibleFirstChild != null)
            {
                return possibleFirstChild.GetLeadingTrivia();
            }
            else
            {
                return null;
            }
        }

        public virtual void GetIndexAndOffset(int targetOffset, out int index, out int offset)
        {
            index = 0;
            offset = 0;
        }

        public virtual SyntaxNode WithLeadingTrivia(SyntaxNode trivia)
        {
            return this;
        }

        public virtual SyntaxNode WithTrailingTrivia(SyntaxNode trivia)
        {
            return this;
        }

        public IEnumerable<SyntaxNode> ChildNodes
        {
            get
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    var child = GetSlot(i);
                    if (child != null)
                    {
                        yield return child;
                    }
                }
            }
        }

        public SyntaxNode GetParent(int parentChainLength = 1)
        {
            var current = this;
            for (int i = 0; i < parentChainLength; i++)
            {
                if (current == null)
                {
                    return null;
                }

                current = current.Parent;
            }

            return current;
        }

        public IXmlElement ParentElement
        {
            get
            {
                var current = this.Parent;
                while (current != null)
                {
                    if (current is IXmlElement)
                    {
                        return (IXmlElement)current;
                    }

                    current = current.Parent;
                }

                return null;
            }
        }

        public IEnumerable<IXmlElement> GetDescendants()
        {
            List<IXmlElement> result = new List<IXmlElement>();
            AddDescendants(this, result);
            return result;
        }

        private static void AddDescendants(SyntaxNode node, List<IXmlElement> resultList)
        {
            if (node is IXmlElement)
            {
                resultList.Add((IXmlElement)node);
            }

            foreach (var child in node.ChildNodes)
            {
                AddDescendants(child, resultList);
            }
        }

        public virtual string ToFullString()
        {
            var builder = PooledStringBuilder.GetInstance();
            var writer = new StringWriter(builder, CultureInfo.InvariantCulture);
            WriteTo(writer);
            return builder.ToStringAndFree();
        }

        /*  <summary>
        ''' Append the full text of this node including children and trivia to the given stringbuilder.
        ''' </summary>
        */
        public virtual void WriteTo(TextWriter writer)
        {
            var stack = new Stack<SyntaxNode>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                ((SyntaxNode)stack.Pop()).WriteToOrFlatten(writer, stack);
            }
        }

        /*  <summary>
        ''' NOTE: the method should write OR push children, but never do both
        ''' </summary>
        */
        internal virtual void WriteToOrFlatten(TextWriter writer, Stack<SyntaxNode> stack)
        {
            // By default just push children to the stack
            for (var i = this.SlotCount - 1; i >= 0; i--)
            {
                var node = GetSlot(i);
                if (node != null)
                {
                    stack.Push(GetSlot(i));
                }
            }
        }

        // Get the trailing trivia a green array, recursively to first token.
        public virtual SyntaxNode GetTrailingTrivia()
        {
            var possibleLastChild = GetLastToken();
            if (possibleLastChild != null)
            {
                return possibleLastChild.GetTrailingTrivia();
            }
            else
            {
                return null;
            }
        }

        internal bool IsMissing
        {
            get
            {
                // flag has reversed meaning hence "=="
                return false; // (this.flags & NodeFlags.IsNotMissing) == 0;
            }
        }

        public virtual int GetLeadingTriviaWidth()
        {
            return this.GetFirstTerminal().GetLeadingTriviaWidth();
        }

        public virtual int GetTrailingTriviaWidth()
        {
            return this.GetLastTerminal().GetTrailingTriviaWidth();
        }

        public bool HasLeadingTrivia
        {
            get
            {
                return this.GetLeadingTrivia() != null;
            }
        }

        public bool HasTrailingTrivia
        {
            get
            {
                return this.GetTrailingTrivia() != null;
            }
        }

        internal SyntaxToken GetFirstToken()
        {
            return ((SyntaxToken)this.GetFirstTerminal());
        }

        internal SyntaxToken GetLastToken()
        {
            return ((SyntaxToken)this.GetLastTerminal());
        }

        public SyntaxNode GetFirstTerminal()
        {
            var node = this;

            do
            {
                bool foundChild = false;
                for (int i = 0, n = node.SlotCount; i < n; i++)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        node = child;
                        foundChild = true;
                        break;
                    }
                }

                if (!foundChild)
                {
                    return null;
                }
            }
            while (node.SlotCount != 0);

            return node;
        }

        internal SyntaxNode AddError(DiagnosticInfo diagnostic)
        {
            return this;
        }

        public virtual SyntaxNode SetDiagnostics(params DiagnosticInfo[] diagnostics)
        {
            return this;
        }

        /*  <summary>
         ''' Add all the tokens in this node and children to the build token list builder. While doing this, add any
         ''' diagnostics not on tokens to the given diagnostic info list.
         ''' </summary>
        */
        internal virtual void CollectConstituentTokensAndDiagnostics(SyntaxListBuilder<SyntaxToken> tokenListBuilder, IList<DiagnosticInfo> nonTokenDiagnostics)
        {
            DiagnosticInfo[] diagnostics = this.GetDiagnostics();
            if (diagnostics != null && diagnostics.Length > 0)
            {
                foreach (var diag in diagnostics)
                {
                    nonTokenDiagnostics.Add(diag);
                }
            }

            // Recurse to subtrees.
            for (var i = 0; i < SlotCount; i++)
            {
                var green = GetSlot(i);
                if (green != null)
                {
                    ((SyntaxNode)green).CollectConstituentTokensAndDiagnostics(tokenListBuilder, nonTokenDiagnostics);
                }
            }
        }

        private static readonly DiagnosticInfo[] NoDiagnostics = new DiagnosticInfo[0];

        internal DiagnosticInfo[] GetDiagnostics()
        {
            ////if (this.ContainsDiagnostics)
            ////{
            ////    DiagnosticInfo[] diags;
            ////    if (diagnosticsTable.TryGetValue(this, out diags))
            ////    {
            ////        return diags;
            ////    }
            ////}

            return NoDiagnostics;
        }

        public SyntaxNode GetLastTerminal()
        {
            var node = this;

            do
            {
                for (int i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        node = child;
                        break;
                    }
                }
            } while (node.slotCount != 0);

            return node;
        }

        public int SlotCount
        {
            get
            {
                int count = this.slotCount;
                if (count == byte.MaxValue)
                {
                    count = GetSlotCount();
                }

                return count;
            }

            protected set
            {
                this.slotCount = (byte)value;
            }
        }

        public bool IsList
        {
            get
            {
                return Kind == SyntaxKind.List;
            }
        }

        public virtual bool IsToken { get { return false; } }

        // for slot count's >= byte.MaxValue
        protected virtual int GetSlotCount()
        {
            return this.slotCount;
        }

        public abstract SyntaxNode GetSlot(int index);

        protected virtual void AdjustWidth(SyntaxNode node)
        {
            this.fullWidth = FullWidth + node.FullWidth;
        }
    }
}
