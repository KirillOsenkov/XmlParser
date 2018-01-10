﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public abstract class SyntaxNode
    {
        public SyntaxNode Parent { get; }
        public int Start { get; }

        public SyntaxKind Kind => GreenNode.Kind;
        public int FullWidth => GreenNode.FullWidth;
        public int Width => GreenNode.Width;

        internal GreenNode GreenNode { get; }

        internal SyntaxNode(GreenNode green, SyntaxNode parent, int position)
        {
            this.GreenNode = green;
            this.Parent = parent;
            this.Start = position;
        }

        public TextSpan Span
        {
            get
            {
                // Start with the full span.
                var start = Start;
                var width = this.GreenNode.FullWidth;

                // adjust for preceding trivia (avoid calling this twice, do not call Green.Width)
                var precedingWidth = this.GreenNode.GetLeadingTriviaWidth();
                start += precedingWidth;
                width -= precedingWidth;

                // adjust for following trivia width
                width -= this.GreenNode.GetTrailingTriviaWidth();

                Debug.Assert(width >= 0);
                return new TextSpan(start, width);
            }
        }

        public int SpanStart => Start + GreenNode.GetLeadingTriviaWidth();

        public TextSpan FullSpan => new TextSpan(this.Start, this.GreenNode.FullWidth);

        public int End => Start + FullWidth;

        internal int SlotCount => GreenNode.SlotCount;

        public virtual int GetSlotCountIncludingTrivia()
        {
            return GreenNode.SlotCount;
        }

        public virtual SyntaxNode GetSlotIncludingTrivia(int index)
        {
            return GetNodeSlot(index);
        }

        public abstract SyntaxNode Accept(SyntaxVisitor visitor);

        protected virtual int GetTextWidth()
        {
            return 0;
        }

        internal SyntaxNode GetRed(ref SyntaxNode field, int slot)
        {
            var result = field;

            if (result == null)
            {
                var green = this.GreenNode.GetSlot(slot);
                if (green != null)
                {
                    Interlocked.CompareExchange(ref field, green.CreateRed(this, this.GetChildPosition(slot)), null);
                    result = field;
                }
            }

            return result;
        }

        protected T GetRed<T>(ref T field, int slot) where T : SyntaxNode
        {
            var result = field;

            if (result == null)
            {
                var green = this.GreenNode.GetSlot(slot);
                if (green != null)
                {
                    Interlocked.CompareExchange(ref field, (T)green.CreateRed(this, this.GetChildPosition(slot)), null);
                    result = field;
                }
            }

            return result;
        }

        internal SyntaxNode GetRedElement(ref SyntaxNode element, int slot)
        {
            Debug.Assert(this.IsList);

            var result = element;

            if (result == null)
            {
                var green = this.GreenNode.GetSlot(slot);
                // passing list's parent
                Interlocked.CompareExchange(ref element, green.CreateRed(this.Parent, this.GetChildPosition(slot)), null);
                result = element;
            }

            return result;
        }

        internal virtual int GetChildPosition(int index)
        {
            int offset = 0;
            var green = this.GreenNode;
            while (index > 0)
            {
                index--;
                var prevSibling = this.GetCachedSlot(index);
                if (prevSibling != null)
                {
                    return prevSibling.End + offset;
                }
                var greenChild = green.GetSlot(index);
                if (greenChild != null)
                {
                    offset += greenChild.FullWidth;
                }
            }

            return this.Start + offset;
        }

        /// <summary>
        /// Gets a node at given node index without forcing its creation.
        /// If node was not created it would return null.
        /// </summary>
        internal abstract SyntaxNode GetCachedSlot(int index);

        /// <summary>
        /// Creates a new tree of nodes with the specified nodes, tokens or trivia replaced.
        /// </summary>
        protected internal virtual SyntaxNode ReplaceCore<TNode>(
            IEnumerable<TNode> nodes = null,
            Func<TNode, TNode, SyntaxNode> computeReplacementNode = null,
            IEnumerable<SyntaxToken> tokens = null,
            Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null,
            IEnumerable<SyntaxTrivia> trivia = null,
            Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null) where TNode : SyntaxNode
        {
            return SyntaxReplacer.Replace(this, nodes, computeReplacementNode, tokens, computeReplacementToken, trivia, computeReplacementTrivia);
        }

        // Get the leading trivia a green array, recursively to first token.
        public virtual SyntaxNode GetLeadingTrivia()
        {
            return GetFirstToken()?.GetLeadingTrivia();
        }

        // Get the trailing trivia a green array, recursively to first token.
        public virtual SyntaxNode GetTrailingTrivia()
        {
            return GetLastToken()?.GetTrailingTrivia();
        }

        public virtual void GetIndexAndOffset(int targetOffset, out int index, out int offset)
        {
            index = 0;
            offset = 0;
        }

        internal abstract SyntaxNode GetNodeSlot(int index);

        public IEnumerable<SyntaxNode> ChildNodes
        {
            get
            {
                for (int i = 0; i < GreenNode.SlotCount; i++)
                {
                    var child = GetNodeSlot(i);
                    if (child != null)
                    {
                        yield return child;
                    }
                }
            }
        }

        public IEnumerable<IXmlElement> GetParents()
        {
            var parent = this.ParentElement;
            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
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
                    if (current.IsElement())
                    {
                        return (IXmlElement)current;
                    }

                    current = current.Parent;
                }

                return null;
            }
        }

        internal IEnumerable<XmlNodeSyntax> GetParentElementsAndAttributes()
        {
            var parent = Parent;
            while (parent != null && !parent.IsElement())
            {
                if (parent.Kind == SyntaxKind.XmlAttribute)
                    yield return (XmlNodeSyntax)parent;
                parent = parent.Parent;
            }
            if (parent == null)
                yield break;
            var parentElement = (XmlNodeSyntax)parent;
            while (parentElement != null)
            {
                yield return parentElement;
                parentElement = (XmlNodeSyntax)parentElement.ParentElement;
            }
        }

        public IEnumerable<IXmlElement> GetDescendantsAndSelf()
        {
            return GetDescendantsInternal(this, includeSelf: true);
        }

        public IEnumerable<IXmlElement> GetDescendants()
        {
            return GetDescendantsInternal(this, includeSelf: false);
        }

        private static IEnumerable<IXmlElement> GetDescendantsInternal(SyntaxNode node, bool includeSelf)
        {
            if (includeSelf && node is IXmlElement e)
            {
                yield return e;
            }

            var childStack = new Stack<SyntaxNode>();
            childStack.Push(node);

            while (childStack.Count > 0)
            {
                var c = childStack.Pop();
                for (int i = 0; i < c.GreenNode.SlotCount; i++)
                {
                    var child = c.GetNodeSlot(i);
                    if (child != null && child is IXmlElement childElement)
                    {
                        yield return childElement;
                    }
                }
                for (int i = c.GreenNode.SlotCount - 1; i >= 0; i--)
                {
                    var child = c.GetCachedSlot(i);
                    if (child != null)
                        childStack.Push(child);
                }
            }
        }

        public virtual string ToFullString()
        {
            return GreenNode.ToFullString();
        }

        public bool HasLeadingTrivia => GreenNode.HasLeadingTrivia;

        public bool HasTrailingTrivia => GreenNode.HasTrailingTrivia;

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
                    var child = node.GetNodeSlot(i);
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

            return node == this ? this : node;
        }

        public SyntaxNode GetLastTerminal()
        {
            var node = this;

            do
            {
                for (int i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetNodeSlot(i);
                    if (child != null)
                    {
                        node = child;
                        break;
                    }
                }
            } while (node.SlotCount != 0);

            return node == this ? this : node;
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
        /*internal virtual void CollectConstituentTokensAndDiagnostics(SyntaxListBuilder<SyntaxToken> tokenListBuilder, IList<DiagnosticInfo> nonTokenDiagnostics)
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
                    green.CollectConstituentTokensAndDiagnostics(tokenListBuilder, nonTokenDiagnostics);
                }
            }
        }*/

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

        public bool IsList => GreenNode.IsList;

        internal bool IsMissing => GreenNode.IsMissing;

        public virtual bool IsToken => GreenNode.IsToken;
    }
}
