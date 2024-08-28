using System;
using System.Linq;
using System.Collections.Generic;

#pragma warning disable CS8602

namespace Microsoft.Language.Xml
{
    public static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Creates a new tree of nodes with the specified nodes, tokens and trivia replaced.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="nodes">The nodes to be replaced.</param>
        /// <param name="computeReplacementNode">A function that computes a replacement node for the
        /// argument nodes. The first argument is the original node. The second argument is the same
        /// node potentially rewritten with replaced descendants.</param>
        /// <param name="tokens">The tokens to be replaced.</param>
        /// <param name="computeReplacementToken">A function that computes a replacement token for
        /// the argument tokens. The first argument is the original token. The second argument is
        /// the same token potentially rewritten with replaced trivia.</param>
        /// <param name="trivia">The trivia to be replaced.</param>
        /// <param name="computeReplacementTrivia">A function that computes replacement trivia for
        /// the specified arguments. The first argument is the original trivia. The second argument is
        /// the same trivia with potentially rewritten sub structure.</param>
        public static TRoot ReplaceSyntax<TRoot>(
            this TRoot root,
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken,
            IEnumerable<SyntaxTrivia> trivia,
            Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore(
                nodes: nodes, computeReplacementNode: computeReplacementNode,
                tokens: tokens, computeReplacementToken: computeReplacementToken,
                trivia: trivia, computeReplacementTrivia: computeReplacementTrivia);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <typeparam name="TNode">The type of the nodes being replaced.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="nodes">The nodes to be replaced; descendants of the root node.</param>
        /// <param name="computeReplacementNode">A function that computes a replacement node for the
        /// argument nodes. The first argument is the original node. The second argument is the same
        /// node potentially rewritten with replaced descendants.</param>
        public static TRoot ReplaceNodes<TRoot, TNode>(this TRoot root, IEnumerable<TNode> nodes, Func<TNode, TNode, SyntaxNode> computeReplacementNode)
            where TRoot : SyntaxNode
            where TNode : SyntaxNode
        {
            return (TRoot)root.ReplaceCore(nodes: nodes, computeReplacementNode: computeReplacementNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="oldNode">The node to be replaced; a descendant of the root node.</param>
        /// <param name="newNode">The new node to use in the new tree in place of the old node.</param>
        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, SyntaxNode newNode)
            where TRoot : SyntaxNode
        {
            if (oldNode == newNode)
            {
                return root;
            }

            return (TRoot)root.ReplaceCore(nodes: new[] { oldNode }, computeReplacementNode: (o, r) => newNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with specified old node replaced with a new nodes.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="oldNode">The node to be replaced; a descendant of the root node and an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to use in the tree in place of the old node.</param>
        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceNodeInListCore(oldNode, newNodes);
        }

        /// <summary>
        /// Creates a new tree of nodes with new nodes inserted before the specified node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="nodeInList">The node to insert before; a descendant of the root node an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to insert into the tree immediately before the specified node.</param>
        public static TRoot InsertNodesBefore<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertNodesInListCore(nodeInList, newNodes, insertBefore: true);
        }

        /// <summary>
        /// Creates a new tree of nodes with new nodes inserted after the specified node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="nodeInList">The node to insert after; a descendant of the root node an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to insert into the tree immediately after the specified node.</param>
        public static TRoot InsertNodesAfter<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertNodesInListCore(nodeInList, newNodes, insertBefore: false);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old token replaced with a new token.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="oldToken">The token to be replaced.</param>
        /// <param name="newToken">The new token to use in the new tree in place of the old
        /// token.</param>
        public static TRoot ReplaceToken<TRoot>(this TRoot root, SyntaxToken oldToken, SyntaxToken newToken)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore<SyntaxNode>(tokens: new[] { oldToken }, computeReplacementToken: (o, r) => newToken);
        }

        /// <summary>
        /// Creates a new tree of nodes with new tokens inserted before the specified token.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="tokenInList">The token to insert before; a descendant of the root node and an element of a list member.</param>
        /// <param name="newTokens">A sequence of tokens to insert into the tree immediately before the specified token.</param>
        public static TRoot InsertTokensBefore<TRoot>(this TRoot root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertTokensInListCore(tokenInList, newTokens, insertBefore: true);
        }

        /// <summary>
        /// Creates a new tree of nodes with new tokens inserted after the specified token.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="tokenInList">The token to insert after; a descendant of the root node and an element of a list member.</param>
        /// <param name="newTokens">A sequence of tokens to insert into the tree immediately after the specified token.</param>
        public static TRoot InsertTokensAfter<TRoot>(this TRoot root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertTokensInListCore(tokenInList, newTokens, insertBefore: false);
        }

        /*
		/// <summary>
		/// Creates a new tree of nodes with the specified old trivia replaced with new trivia.
		/// </summary>
		/// <typeparam name="TRoot">The type of the root node.</typeparam>
		/// <param name="root">The root of the tree of nodes.</param>
		/// <param name="oldTrivia">The trivia to be replaced; a descendant of the root node.</param>
		/// <param name="newTrivia">A sequence of trivia to use in the tree in place of the specified trivia.</param>
		public static TRoot ReplaceTrivia<TRoot> (this TRoot root, SyntaxTrivia oldTrivia, IEnumerable<SyntaxTrivia> newTrivia)
			where TRoot : SyntaxNode
		{
			return (TRoot)root.ReplaceTriviaInListCore (oldTrivia, newTrivia);
		}

		/// <summary>
		/// Creates a new tree of nodes with new trivia inserted before the specified trivia.
		/// </summary>
		/// <typeparam name="TRoot">The type of the root node.</typeparam>
		/// <param name="root">The root of the tree of nodes.</param>
		/// <param name="trivia">The trivia to insert before; a descendant of the root node.</param>
		/// <param name="newTrivia">A sequence of trivia to insert into the tree immediately before the specified trivia.</param>
		public static TRoot InsertTriviaBefore<TRoot> (this TRoot root, SyntaxTrivia trivia, IEnumerable<SyntaxTrivia> newTrivia)
			where TRoot : SyntaxNode
		{
			return (TRoot)root.InsertTriviaInListCore (trivia, newTrivia, insertBefore: true);
		}

		/// <summary>
		/// Creates a new tree of nodes with new trivia inserted after the specified trivia.
		/// </summary>
		/// <typeparam name="TRoot">The type of the root node.</typeparam>
		/// <param name="root">The root of the tree of nodes.</param>
		/// <param name="trivia">The trivia to insert after; a descendant of the root node.</param>
		/// <param name="newTrivia">A sequence of trivia to insert into the tree immediately after the specified trivia.</param>
		public static TRoot InsertTriviaAfter<TRoot> (this TRoot root, SyntaxTrivia trivia, IEnumerable<SyntaxTrivia> newTrivia)
			where TRoot : SyntaxNode
		{
			return (TRoot)root.InsertTriviaInListCore (trivia, newTrivia, insertBefore: false);
		}*/

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="tokens">The token to be replaced; descendants of the root node.</param>
        /// <param name="computeReplacementToken">A function that computes a replacement token for
        /// the argument tokens. The first argument is the original token. The second argument is
        /// the same token potentially rewritten with replaced trivia.</param>
        public static TRoot ReplaceTokens<TRoot>(this TRoot root, IEnumerable<SyntaxToken> tokens, Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore<SyntaxNode>(tokens: tokens, computeReplacementToken: computeReplacementToken);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified trivia replaced with new trivia.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="trivia">The trivia to be replaced; descendants of the root node.</param>
        /// <param name="computeReplacementTrivia">A function that computes replacement trivia for
        /// the specified arguments. The first argument is the original trivia. The second argument is
        /// the same trivia with potentially rewritten sub structure.</param>
        public static TRoot ReplaceTrivia<TRoot>(this TRoot root, IEnumerable<SyntaxTrivia> trivia, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore<SyntaxNode>(trivia: trivia, computeReplacementTrivia: computeReplacementTrivia);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified trivia replaced with new trivia.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="trivia">The trivia to be replaced.</param>
        /// <param name="newTrivia">The new trivia to use in the new tree in place of the old trivia.</param>
        public static TRoot ReplaceTrivia<TRoot>(this TRoot root, SyntaxTrivia trivia, SyntaxTrivia newTrivia)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore<SyntaxNode>(trivia: new[] { trivia }, computeReplacementTrivia: (o, r) => newTrivia);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified node removed.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node from which to remove a descendant node from.</param>
        /// <param name="node">The node to remove.</param>
        /// <param name="options">Options that determine how the node's trivia is treated.</param>
        public static TRoot RemoveNode<TRoot>(this TRoot root,
            SyntaxNode node,
            SyntaxRemoveOptions options)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.RemoveNodesCore(new[] { node }, options);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified nodes removed.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node from which to remove a descendant node from.</param>
        /// <param name="nodes">The nodes to remove.</param>
        /// <param name="options">Options that determine how the nodes' trivia is treated.</param>
        public static TRoot RemoveNodes<TRoot>(
            this TRoot root,
            IEnumerable<SyntaxNode> nodes,
            SyntaxRemoveOptions options)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.RemoveNodesCore(nodes, options);
        }

        /// <summary>
        /// Creates a new node from this node with the leading trivia replaced.
        /// </summary>
        public static TSyntax WithLeadingTrivia<TSyntax>(
            this TSyntax node,
            SyntaxNode trivia) where TSyntax : SyntaxNode
        {
            var first = node.GetFirstToken();
            var newFirst = first.WithLeadingTrivia(trivia);
            return node.ReplaceToken(first, newFirst);
        }

        /// <summary>
        /// Creates a new node from this node with the leading trivia replaced.
        /// </summary>
        public static TSyntax WithLeadingTrivia<TSyntax>(
            this TSyntax node,
            IEnumerable<SyntaxTrivia>? trivia) where TSyntax : SyntaxNode
        {
            var first = node.GetFirstToken();
            var newFirst = first.WithLeadingTrivia(trivia);
            return node.ReplaceToken(first, newFirst);
        }

        /// <summary>
        /// Creates a new node from this node with the leading trivia replaced.
        /// </summary>
        public static TSyntax WithLeadingTrivia<TSyntax>(
            this TSyntax node,
            params SyntaxTrivia[] trivia) where TSyntax : SyntaxNode
        {
            return node.WithLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        /// <summary>
        /// Creates a new node from this node with the leading trivia removed.
        /// </summary>
        public static TSyntax WithoutLeadingTrivia<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode
        {
            return node.WithLeadingTrivia((IEnumerable<SyntaxTrivia>?)null);
        }

        /// <summary>
        /// Creates a new node from this node with the trailing trivia replaced.
        /// </summary>
        public static TSyntax WithTrailingTrivia<TSyntax>(
            this TSyntax node,
            SyntaxNode trivia) where TSyntax : SyntaxNode
        {
            var last = node.GetLastToken();
            var newLast = last.WithTrailingTrivia(trivia);
            return node.ReplaceToken(last, newLast);
        }

        /// <summary>
        /// Creates a new node from this node with the trailing trivia replaced.
        /// </summary>
        public static TSyntax WithTrailingTrivia<TSyntax>(
            this TSyntax node,
            IEnumerable<SyntaxTrivia>? trivia) where TSyntax : SyntaxNode
        {
            var last = node.GetLastToken();
            var newLast = last.WithTrailingTrivia(trivia);
            return node.ReplaceToken(last, newLast);
        }

        /// <summary>
        /// Creates a new node from this node with the trailing trivia replaced.
        /// </summary>
        public static TSyntax WithTrailingTrivia<TSyntax>(
            this TSyntax node,
            params SyntaxTrivia[] trivia) where TSyntax : SyntaxNode
        {
            return node.WithTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        /// <summary>
        /// Creates a new node from this node with the leading trivia removed.
        /// </summary>
        public static TSyntax WithoutTrailingTrivia<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode
        {
            return node.WithTrailingTrivia((IEnumerable<SyntaxTrivia>?)null);
        }

        /// <summary>
        /// Produces a pessimistic list of spans that denote the regions of text in this tree that
        /// are changed from the text of the old tree.
        /// </summary>
        /// <param name="oldTree">The old tree. Cannot be <c>null</c>.</param>
        /// <remarks>The list is pessimistic because it may claim more or larger regions than actually changed.</remarks>
        public static IList<TextSpan> GetChangedSpans(this SyntaxNode newTree, SyntaxNode oldTree)
        {
            if (oldTree == null)
            {
                throw new ArgumentNullException(nameof(oldTree));
            }

            return SyntaxDiffer.GetPossiblyDifferentTextSpans(oldTree, newTree);
        }

        /// <summary>
        /// Gets a list of text changes that when applied to the old tree produce this tree.
        /// </summary>
        /// <param name="oldTree">The old tree. Cannot be <c>null</c>.</param>
        /// <remarks>The list of changes may be different than the original changes that produced this tree.</remarks>
        public static IList<TextChange> GetChanges(this SyntaxNode newTree, SyntaxNode oldTree)
        {
            if (oldTree == null)
            {
                throw new ArgumentNullException(nameof(oldTree));
            }

            return SyntaxDiffer.GetTextChanges(oldTree, newTree);
        }

        /// <summary>
        /// Returns true if the node is a XmlElementSyntax or XmlEmptyElementSyntax
        /// </summary>
        public static bool IsElement(this SyntaxNode node)
        {
            return node.Kind == SyntaxKind.XmlElement || node.Kind == SyntaxKind.XmlEmptyElement;
        }

        public static TNode WithAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : SyntaxNode
        {
            return (TNode)node.GreenNode.SetAnnotations(annotations).CreateRed();
        }

        /// <summary>
        /// Creates a new node identical to this node with the specified annotations attached.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotations">Annotations to be added to the new node.</param>
        public static TNode WithAdditionalAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations)
            where TNode : SyntaxNode
        {
            return (TNode)node.WithAdditionalAnnotationsInternal(annotations);
        }

        /// <summary>
        /// Creates a new node identical to this node with the specified annotations attached.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotations">Annotations to be added to the new node.</param>
        public static TNode WithAdditionalAnnotations<TNode>(this TNode node, IEnumerable<SyntaxAnnotation> annotations)
            where TNode : SyntaxNode
        {
            return (TNode)node.WithAdditionalAnnotationsInternal(annotations);
        }

        /// <summary>
        /// Creates a new node identical to this node with the specified annotations removed.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotations">Annotations to be removed from the new node.</param>
        public static TNode WithoutAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations)
            where TNode : SyntaxNode
        {
            return (TNode)node.GetNodeWithoutAnnotations(annotations);
        }

        /// <summary>
        /// Creates a new node identical to this node with the specified annotations removed.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotations">Annotations to be removed from the new node.</param>
        public static TNode WithoutAnnotations<TNode>(this TNode node, IEnumerable<SyntaxAnnotation> annotations)
            where TNode : SyntaxNode
        {
            return (TNode)node.GetNodeWithoutAnnotations(annotations);
        }

        /// <summary>
        /// Creates a new node identical to this node with the annotations of the specified kind removed.
        /// </summary>
        /// <param name="node">Original node.</param>
        /// <param name="annotationKind">The kind of annotation to remove.</param>
        public static TNode WithoutAnnotations<TNode>(this TNode node, string annotationKind)
            where TNode : SyntaxNode
        {
            if (node.HasAnnotations(annotationKind))
            {
                return node.WithoutAnnotations<TNode>(node.GetAnnotations(annotationKind).ToArray());
            }
            else
            {
                return node;
            }
        }

        /// <summary>
        /// create a new root node from the given root after adding annotations to the tokens
        /// 
        /// tokens should belong to the given root
        /// </summary>
        public static SyntaxNode AddAnnotations(this SyntaxNode root, IEnumerable<Tuple<SyntaxToken, SyntaxAnnotation>> pairs)
        {
            var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
            return root.ReplaceTokens(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
        }

        /// <summary>
        /// create a new root node from the given root after adding annotations to the nodes
        /// 
        /// nodes should belong to the given root
        /// </summary>
        public static SyntaxNode AddAnnotations(this SyntaxNode root, IEnumerable<Tuple<SyntaxNode, SyntaxAnnotation>> pairs)
        {
            var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
            return root.ReplaceNodes(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
        }

        public static IEnumerable<T> GetAnnotatedNodes<T>(this SyntaxNode node, SyntaxAnnotation syntaxAnnotation) where T : SyntaxNode
        {
            return node.GetAnnotatedNodesAndTokens(syntaxAnnotation).OfType<T>();
        }

        public static IEnumerable<IXmlElementSyntax> AncestorsAndSelf(this SyntaxNode node)
        {
            return node.AncestorNodesAndSelf().Where(n => n.IsElement()).Cast<IXmlElementSyntax>();
        }

        public static IEnumerable<IXmlElementSyntax> Ancestors(this SyntaxNode node)
        {
            return node.AncestorNodes().Where(n => n.IsElement()).Cast<IXmlElementSyntax>();
        }

        public static IEnumerable<IXmlElementSyntax> AncestorsAndSelf(this IXmlElementSyntax element)
        {
            return element.AsNode.AncestorsAndSelf();
        }

        public static IEnumerable<IXmlElementSyntax> Ancestors(this IXmlElementSyntax element)
        {
            return element.AsNode.Ancestors();
        }

        public static IEnumerable<IXmlElementSyntax> DescendantsAndSelf(this SyntaxNode node)
        {
            return node.DescendantNodesAndSelf().Where(n => n.IsElement()).Cast<IXmlElementSyntax>();
        }

        public static IEnumerable<IXmlElementSyntax> Descendants(this SyntaxNode node)
        {
            return node.DescendantNodes().Where(n => n.IsElement()).Cast<IXmlElementSyntax>();
        }

        public static IEnumerable<IXmlElementSyntax> DescendantsAndSelf(this IXmlElementSyntax element)
        {
            return element.AsNode.DescendantsAndSelf();
        }

        public static IEnumerable<IXmlElementSyntax> Descendants(this IXmlElementSyntax element)
        {
            return element.AsNode.Descendants();
        }

        public static int GetLeadingTriviaWidth(this SyntaxNode node) => node.GetLeadingTriviaSpan().Length;
        public static int GetTrailingTriviaWidth(this SyntaxNode node) => node.GetTrailingTriviaSpan().Length;
    }
}
