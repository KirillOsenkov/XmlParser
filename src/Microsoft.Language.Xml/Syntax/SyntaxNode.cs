using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public abstract partial class SyntaxNode
    {
        public SyntaxNode Parent { get; }
        public int Start { get; }

        public SyntaxKind Kind => GreenNode.Kind;

        public int FullWidth => GreenNode.FullWidth;
        public int Width => GreenNode.Width;
        public int SpanStart => Start + GreenNode.GetLeadingTriviaWidth();
        public TextSpan FullSpan => new TextSpan(this.Start, this.GreenNode.FullWidth);
        public int End => Start + FullWidth;

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

        protected internal virtual SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes)
        {
            return SyntaxReplacer.ReplaceNodeInList(this, originalNode, replacementNodes);
        }

        protected internal virtual SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
        {
            return SyntaxReplacer.InsertNodeInList(this, nodeInList, nodesToInsert, insertBefore);
        }

        protected internal virtual SyntaxNode ReplaceTokenInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens)
        {
            return SyntaxReplacer.ReplaceTokenInList(this, originalToken, newTokens);
        }

        protected internal virtual SyntaxNode InsertTokensInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens, bool insertBefore)
        {
            return SyntaxReplacer.InsertTokenInList(this, originalToken, newTokens, insertBefore);
        }

        protected internal virtual SyntaxNode RemoveNodesCore(IEnumerable<SyntaxNode> nodes, SyntaxRemoveOptions options)
        {
            return SyntaxNodeRemover.RemoveNodes(this, nodes, options);
        }

        /*protected virtual SyntaxNode ReplaceTriviaInListCore (SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia)
		{
			return SyntaxReplacer.ReplaceTriviaInList (this, originalTrivia, newTrivia).AsRootOfNewTreeWithOptionsFrom (this.SyntaxTree);
		}

		protected virtual SyntaxNode InsertTriviaInListCore (SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore)
		{
			return SyntaxReplacer.InsertTriviaInList (this, originalTrivia, newTrivia, insertBefore).AsRootOfNewTreeWithOptionsFrom (this.SyntaxTree);
		}*/

        // Get the leading trivia a green array, recursively to first token.
        public virtual SyntaxTriviaList GetLeadingTrivia()
        {
            var firstToken = GetFirstToken();
            return firstToken == null ? default(SyntaxTriviaList) : firstToken.GetLeadingTrivia();
        }

        // Get the trailing trivia a green array, recursively to first token.
        public virtual SyntaxTriviaList GetTrailingTrivia()
        {
            var lastToken = GetLastToken();
            return lastToken == null ? default(SyntaxTriviaList) : lastToken.GetTrailingTrivia();
        }

        /// <summary>
        /// Get a list of all the trivia associated with the descendant nodes and tokens.
        /// </summary>
        public IEnumerable<SyntaxTrivia> DescendantTrivia(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantTriviaImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia);
        }

        /// <summary>
        /// Get a list of all the trivia associated with the descendant nodes and tokens.
        /// </summary>
        public IEnumerable<SyntaxTrivia> DescendantTrivia(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantTriviaImpl(span, descendIntoChildren, descendIntoTrivia);
        }

        public virtual void GetIndexAndOffset(int targetOffset, out int index, out int offset)
        {
            index = 0;
            offset = 0;
        }

        /// <summary>
        /// The list of child nodes and tokens of this node, where each element is a SyntaxNodeOrToken instance.
        /// </summary>
        public ChildSyntaxList ChildNodesAndTokens()
        {
            return new ChildSyntaxList(this);
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

        public IXmlElementSyntax ParentElement
        {
            get
            {
                var current = this.Parent;
                while (current != null)
                {
                    if (current.IsElement())
                    {
                        return (IXmlElementSyntax)current;
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

        /// <summary>
        /// Gets a list of ancestor nodes
        /// </summary>
        public IEnumerable<SyntaxNode> AncestorNodes(bool ascendOutOfTrivia = true)
        {
            return this.Parent?
                       .AncestorNodesAndSelf(ascendOutOfTrivia) ??
                       SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        /// <summary>
        /// Gets a list of ancestor nodes (including this node) 
        /// </summary>
        public IEnumerable<SyntaxNode> AncestorNodesAndSelf(bool ascendOutOfTrivia = true)
        {
            for (var node = this; node != null; node = GetParent(node, ascendOutOfTrivia))
            {
                yield return node;
            }
        }

        private static SyntaxNode GetParent(SyntaxNode node, bool ascendOutOfTrivia) => node.Parent;

        /// <summary>
        /// Gets the first node of type TNode that matches the predicate.
        /// </summary>
        public TNode FirstAncestorOrSelf<TNode>(Func<TNode, bool> predicate = null, bool ascendOutOfTrivia = true)
            where TNode : SyntaxNode
        {
            for (var node = this; node != null; node = GetParent(node, ascendOutOfTrivia))
            {
                var tnode = node as TNode;
                if (tnode != null && (predicate == null || predicate(tnode)))
                {
                    return tnode;
                }
            }

            return default(TNode);
        }

        /// <summary>
        /// Gets a list of descendant nodes in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodes(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia, includeSelf: false);
        }

        /// <summary>
        /// Gets a list of descendant nodes in prefix document order.
        /// </summary>
        /// <param name="span">The span the node's full span must intersect.</param>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodes(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesImpl(span, descendIntoChildren, descendIntoTrivia, includeSelf: false);
        }

        /// <summary>
        /// Gets a list of descendant nodes (including this node) in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodesAndSelf(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia, includeSelf: true);
        }

        /// <summary>
        /// Gets a list of descendant nodes (including this node) in prefix document order.
        /// </summary>
        /// <param name="span">The span the node's full span must intersect.</param>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodesAndSelf(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesImpl(span, descendIntoChildren, descendIntoTrivia, includeSelf: true);
        }

        /// <summary>
        /// Gets a list of descendant nodes and tokens in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodesAndTokens(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesAndTokensImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia, includeSelf: false);
        }

        /// <summary>
        /// Gets a list of the descendant nodes and tokens in prefix document order.
        /// </summary>
        /// <param name="span">The span the node's full span must intersect.</param>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodesAndTokens(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesAndTokensImpl(span, descendIntoChildren, descendIntoTrivia, includeSelf: false);
        }

        /// <summary>
        /// Gets a list of descendant nodes and tokens (including this node) in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodesAndTokensAndSelf(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesAndTokensImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia, includeSelf: true);
        }

        /// <summary>
        /// Gets a list of the descendant nodes and tokens (including this node) in prefix document order.
        /// </summary>
        /// <param name="span">The span the node's full span must intersect.</param>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodesAndTokensAndSelf(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesAndTokensImpl(span, descendIntoChildren, descendIntoTrivia, includeSelf: true);
        }

        public virtual string ToFullString()
        {
            return GreenNode.ToFullString();
        }

        internal void WriteTo(TextWriter writer)
        {
            GreenNode.WriteTo(writer);
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

        internal DiagnosticInfo[] GetDiagnostics() => GreenNode.GetDiagnostics();

        public bool ContainsDiagnostics => GreenNode.ContainsDiagnostics;

        #region Annotations

        /// <summary>
        /// Determines whether this node or any sub node, token or trivia has annotations.
        /// </summary>
        public bool ContainsAnnotations
        {
            get { return this.GreenNode.ContainsAnnotations; }
        }

        /// <summary>
        /// Determines whether this node has any annotations with the specific annotation kind.
        /// </summary>
        public bool HasAnnotations(string annotationKind)
        {
            return this.GreenNode.HasAnnotations(annotationKind);
        }

        /// <summary>
        /// Determines whether this node has any annotations with any of the specific annotation kinds.
        /// </summary>
        public bool HasAnnotations(IEnumerable<string> annotationKinds)
        {
            return this.GreenNode.HasAnnotations(annotationKinds);
        }

        /// <summary>
        /// Determines whether this node has the specific annotation.
        /// </summary>
        public bool HasAnnotation(SyntaxAnnotation annotation)
        {
            return this.GreenNode.HasAnnotation(annotation);
        }

        /// <summary>
        /// Gets all the annotations with the specified annotation kind. 
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind)
        {
            return this.GreenNode.GetAnnotations(annotationKind);
        }

        /// <summary>
        /// Gets all the annotations with the specified annotation kinds. 
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds)
        {
            return this.GreenNode.GetAnnotations(annotationKinds);
        }

        internal SyntaxAnnotation[] GetAnnotations()
        {
            return this.GreenNode.GetAnnotations();
        }

        /// <summary>
        /// Gets all nodes and tokens with an annotation of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxNode> GetAnnotatedNodesAndTokens(string annotationKind)
        {
            return this.DescendantNodesAndTokensAndSelf(n => n.ContainsAnnotations, descendIntoTrivia: true)
                .Where(t => t.HasAnnotations(annotationKind));
        }

        /// <summary>
        /// Gets all nodes and tokens with an annotation of the specified annotation kinds.
        /// </summary>
        public IEnumerable<SyntaxNode> GetAnnotatedNodesAndTokens(params string[] annotationKinds)
        {
            return this.DescendantNodesAndTokensAndSelf(n => n.ContainsAnnotations, descendIntoTrivia: true)
                .Where(t => t.HasAnnotations(annotationKinds));
        }

        /// <summary>
        /// Gets all nodes and tokens with the specified annotation.
        /// </summary>
        public IEnumerable<SyntaxNode> GetAnnotatedNodesAndTokens(SyntaxAnnotation annotation)
        {
            return this.DescendantNodesAndTokensAndSelf(n => n.ContainsAnnotations, descendIntoTrivia: true)
                .Where(t => t.HasAnnotation(annotation));
        }

        /// <summary>
        /// Gets all nodes with the specified annotation.
        /// </summary>
        public IEnumerable<SyntaxNode> GetAnnotatedNodes(SyntaxAnnotation syntaxAnnotation)
        {
            return this.GetAnnotatedNodesAndTokens(syntaxAnnotation).Where(n => n.IsNode);
        }

        /// <summary>
        /// Gets all nodes with the specified annotation kind.
        /// </summary>
        /// <param name="annotationKind"></param>
        /// <returns></returns>
        public IEnumerable<SyntaxNode> GetAnnotatedNodes(string annotationKind)
        {
            return this.GetAnnotatedNodesAndTokens(annotationKind).Where(n => !n.IsToken);
        }

        /// <summary>
        /// Gets all tokens with the specified annotation.
        /// </summary>
        public IEnumerable<SyntaxToken> GetAnnotatedTokens(SyntaxAnnotation syntaxAnnotation)
        {
            return this.GetAnnotatedNodesAndTokens(syntaxAnnotation).Where(n => n.IsToken).Cast<SyntaxToken>();
        }

        /// <summary>
        /// Gets all tokens with the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxToken> GetAnnotatedTokens(string annotationKind)
        {
            return this.GetAnnotatedNodesAndTokens(annotationKind).Where(n => n.IsToken).Cast<SyntaxToken>();
        }

        /// <summary>
        /// Gets all trivia with an annotation of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(string annotationKind)
        {
            return this.DescendantTrivia(n => n.ContainsAnnotations, descendIntoTrivia: true)
                       .Where(tr => tr.HasAnnotations(annotationKind));
        }

        /// <summary>
        /// Gets all trivia with an annotation of the specified annotation kinds.
        /// </summary>
        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(params string[] annotationKinds)
        {
            return this.DescendantTrivia(n => n.ContainsAnnotations, descendIntoTrivia: true)
                       .Where(tr => tr.HasAnnotations(annotationKinds));
        }

        /// <summary>
        /// Gets all trivia with the specified annotation.
        /// </summary>
        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(SyntaxAnnotation annotation)
        {
            return this.DescendantTrivia(n => n.ContainsAnnotations, descendIntoTrivia: true)
                       .Where(tr => tr.HasAnnotation(annotation));
        }

        internal SyntaxNode WithAdditionalAnnotationsInternal(IEnumerable<SyntaxAnnotation> annotations)
        {
            return this.GreenNode.WithAdditionalAnnotationsGreen(annotations).CreateRed();
        }

        internal SyntaxNode GetNodeWithoutAnnotations(IEnumerable<SyntaxAnnotation> annotations)
        {
            return this.GreenNode.WithoutAnnotationsGreen(annotations).CreateRed();
        }

        /// <summary>
        /// Copies all SyntaxAnnotations, if any, from this SyntaxNode instance and attaches them to a new instance based on <paramref name="node" />.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If no annotations are copied, just returns <paramref name="node" />.
        /// </para>
        /// <para>
        /// It can also be used manually to preserve annotations in a more complex tree
        /// modification, even if the type of a node changes.
        /// </para>
        /// </remarks>
        public T CopyAnnotationsTo<T>(T node) where T : SyntaxNode
        {
            if (node == null)
            {
                return default(T);
            }

            var annotations = this.GreenNode.GetAnnotations();
            if (annotations?.Length > 0)
            {
                return (T)(node.GreenNode.WithAdditionalAnnotationsGreen(annotations)).CreateRed();
            }
            return node;
        }

        #endregion

        public bool IsList => GreenNode.IsList;

        internal bool IsMissing => GreenNode.IsMissing;

        public virtual bool IsToken => GreenNode.IsToken;

        public bool IsNode => !IsToken;
    }
}
