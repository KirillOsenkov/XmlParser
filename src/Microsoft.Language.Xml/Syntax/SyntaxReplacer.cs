using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    internal static class SyntaxReplacer
    {
        internal static SyntaxNode Replace<TNode>(
            SyntaxNode root,
            IEnumerable<TNode> nodes = null,
            Func<TNode, TNode, SyntaxNode> computeReplacementNode = null,
            IEnumerable<SyntaxToken> tokens = null,
            Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null,
            IEnumerable<SyntaxTrivia> trivia = null,
            Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null)
            where TNode : SyntaxNode
        {
            var replacer = new Replacer<TNode>(
                nodes, computeReplacementNode,
                tokens, computeReplacementToken,
                trivia, computeReplacementTrivia);

            if (replacer.HasWork)
            {
                return replacer.Visit(root);
            }
            else
            {
                return root;
            }
        }

        internal static SyntaxToken Replace(
            SyntaxToken root,
            IEnumerable<SyntaxNode> nodes = null,
            Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode = null,
            IEnumerable<SyntaxToken> tokens = null,
            Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null,
            IEnumerable<SyntaxTrivia> trivia = null,
            Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null)
        {
            var replacer = new Replacer<SyntaxNode>(
                nodes, computeReplacementNode,
                tokens, computeReplacementToken,
                trivia, computeReplacementTrivia);

            if (replacer.HasWork)
            {
                return replacer.VisitSyntaxToken(root);
            }
            else
            {
                return root;
            }
        }

        private class Replacer<TNode> : SyntaxRewriter where TNode : SyntaxNode
        {
            private readonly Func<TNode, TNode, SyntaxNode> _computeReplacementNode;
            private readonly Func<SyntaxToken, SyntaxToken, SyntaxToken> _computeReplacementToken;
            private readonly Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> _computeReplacementTrivia;

            private readonly HashSet<SyntaxNode> _nodeSet;
            private readonly HashSet<SyntaxToken> _tokenSet;
            private readonly HashSet<SyntaxTrivia> _triviaSet;
            private readonly HashSet<TextSpan> _spanSet;

            private readonly TextSpan _totalSpan;
            private readonly bool _shouldVisitTrivia;

            public Replacer(
                IEnumerable<TNode> nodes,
                Func<TNode, TNode, SyntaxNode> computeReplacementNode,
                IEnumerable<SyntaxToken> tokens,
                Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken,
                IEnumerable<SyntaxTrivia> trivia,
                Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia)
            {
                _computeReplacementNode = computeReplacementNode;
                _computeReplacementToken = computeReplacementToken;
                _computeReplacementTrivia = computeReplacementTrivia;

                _nodeSet = nodes != null ? new HashSet<SyntaxNode>(nodes) : s_noNodes;
                _tokenSet = tokens != null ? new HashSet<SyntaxToken>(tokens) : s_noTokens;
                _triviaSet = trivia != null ? new HashSet<SyntaxTrivia>(trivia) : s_noTrivia;

                _spanSet = new HashSet<TextSpan>(
                    _nodeSet.Select(n => n.FullSpan).Concat(
                    _tokenSet.Select(t => t.FullSpan).Concat(
                    _triviaSet.Select(t => t.FullSpan))));

                _totalSpan = ComputeTotalSpan(_spanSet);

                _shouldVisitTrivia = _triviaSet.Count > 0;
            }

            private static readonly HashSet<SyntaxNode> s_noNodes = new HashSet<SyntaxNode>();
            private static readonly HashSet<SyntaxToken> s_noTokens = new HashSet<SyntaxToken>();
            private static readonly HashSet<SyntaxTrivia> s_noTrivia = new HashSet<SyntaxTrivia>();

            public bool HasWork
            {
                get
                {
                    return _nodeSet.Count + _tokenSet.Count + _triviaSet.Count > 0;
                }
            }

            private static TextSpan ComputeTotalSpan(IEnumerable<TextSpan> spans)
            {
                bool first = true;
                int start = 0;
                int end = 0;

                foreach (var span in spans)
                {
                    if (first)
                    {
                        start = span.Start;
                        end = span.End;
                        first = false;
                    }
                    else
                    {
                        start = Math.Min(start, span.Start);
                        end = Math.Max(end, span.End);
                    }
                }

                return new TextSpan(start, end - start);
            }

            private bool ShouldVisit(TextSpan span)
            {
                // first do quick check against total span
                if (!span.IntersectsWith(_totalSpan))
                {
                    // if the node is outside the total span of the nodes to be replaced
                    // then we won't find any nodes to replace below it.
                    return false;
                }

                foreach (var s in _spanSet)
                {
                    if (span.IntersectsWith(s))
                    {
                        // node's full span intersects with at least one node to be replaced
                        // so we need to visit node's children to find it.
                        return true;
                    }
                }

                return false;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                SyntaxNode rewritten = node;

                if (node != null)
                {
                    if (this.ShouldVisit(node.FullSpan))
                    {
                        rewritten = base.Visit(node);
                    }

                    if (_nodeSet.Contains(node) && _computeReplacementNode != null)
                    {
                        rewritten = _computeReplacementNode((TNode)node, (TNode)rewritten);
                    }
                }

                return rewritten;
            }

            public override SyntaxToken VisitSyntaxToken(SyntaxToken token)
            {
                var rewritten = token;

                if (_shouldVisitTrivia && this.ShouldVisit(token.FullSpan))
                {
                    rewritten = base.VisitSyntaxToken(token);
                }

                if (_tokenSet.Contains(token) && _computeReplacementToken != null)
                {
                    rewritten = _computeReplacementToken(token, rewritten);
                }

                return rewritten;
            }
        }

        internal static SyntaxNode ReplaceNodeInList(SyntaxNode root, SyntaxNode originalNode, IEnumerable<SyntaxNode> newNodes)
        {
            return new NodeListEditor(originalNode, newNodes, ListEditKind.Replace).Visit(root);
        }

        internal static SyntaxNode InsertNodeInList(SyntaxNode root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
        {
            return new NodeListEditor(nodeInList, nodesToInsert, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).Visit(root);
        }

        public static SyntaxNode ReplaceTokenInList(SyntaxNode root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
        {
            return new TokenListEditor(tokenInList, newTokens, ListEditKind.Replace).Visit(root);
        }

        public static SyntaxNode InsertTokenInList(SyntaxNode root, SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens, bool insertBefore)
        {
            return new TokenListEditor(tokenInList, newTokens, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).Visit(root);
        }

        private enum ListEditKind
        {
            InsertBefore,
            InsertAfter,
            Replace
        }

        private static InvalidOperationException GetItemNotListElementException()
        {
            return new InvalidOperationException("Missing list item");
        }

        private abstract class BaseListEditor : SyntaxRewriter
        {
            private readonly TextSpan _elementSpan;
            private readonly bool _visitTrivia;
            private readonly bool _visitIntoStructuredTrivia;

            protected readonly ListEditKind editKind;

            public BaseListEditor(
                TextSpan elementSpan,
                ListEditKind editKind,
                bool visitTrivia,
                bool visitIntoStructuredTrivia)
            {
                _elementSpan = elementSpan;
                this.editKind = editKind;
                _visitTrivia = visitTrivia || visitIntoStructuredTrivia;
                _visitIntoStructuredTrivia = visitIntoStructuredTrivia;
            }

            private bool ShouldVisit(TextSpan span)
            {
                if (span.IntersectsWith(_elementSpan))
                {
                    // node's full span intersects with at least one node to be replaced
                    // so we need to visit node's children to find it.
                    return true;
                }

                return false;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                SyntaxNode rewritten = node;

                if (node != null)
                {
                    if (this.ShouldVisit(node.FullSpan))
                    {
                        rewritten = base.Visit(node);
                    }
                }

                return rewritten;
            }

            public override SyntaxToken VisitSyntaxToken(SyntaxToken token)
            {
                var rewritten = token;

                if (_visitTrivia && this.ShouldVisit(token.FullSpan))
                {
                    rewritten = base.VisitSyntaxToken(token);
                }

                return rewritten;
            }
        }

        private class NodeListEditor : BaseListEditor
        {
            private readonly SyntaxNode _originalNode;
            private readonly IEnumerable<SyntaxNode> _newNodes;

            public NodeListEditor(
                SyntaxNode originalNode,
                IEnumerable<SyntaxNode> replacementNodes,
                ListEditKind editKind)
                : base(originalNode.Span, editKind, false, false)
            {
                _originalNode = originalNode;
                _newNodes = replacementNodes;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == _originalNode)
                {
                    throw GetItemNotListElementException();
                }

                return base.Visit(node);
            }

            public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
            {
                if (_originalNode is TNode)
                {
                    var index = list.IndexOf((TNode)_originalNode);
                    if (index >= 0 && index < list.Count)
                    {
                        switch (this.editKind)
                        {
                            case ListEditKind.Replace:
                                return list.ReplaceRange((TNode)_originalNode, _newNodes.Cast<TNode>());

                            case ListEditKind.InsertAfter:
                                return list.InsertRange(index + 1, _newNodes.Cast<TNode>());

                            case ListEditKind.InsertBefore:
                                return list.InsertRange(index, _newNodes.Cast<TNode>());
                        }
                    }
                }

                return base.VisitList<TNode>(list);
            }
        }

        private class TokenListEditor : BaseListEditor
        {
            private readonly SyntaxToken _originalToken;
            private readonly IEnumerable<SyntaxToken> _newTokens;

            public TokenListEditor(
                SyntaxToken originalToken,
                IEnumerable<SyntaxToken> newTokens,
                ListEditKind editKind)
                : base(originalToken.Span, editKind, false, false)
            {
                _originalToken = originalToken;
                _newTokens = newTokens;
            }

            public override SyntaxToken VisitSyntaxToken(SyntaxToken token)
            {
                if (token == _originalToken)
                {
                    throw GetItemNotListElementException();
                }

                return base.VisitSyntaxToken(token);
            }

            // TODO the TNode type makes it hard to constrain this to SyntaxToken only
            /*public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
			{
				var index = list.IndexOf ((SyntaxNode)_originalToken);
				if (index >= 0 && index < list.Count) {
					switch (this.editKind) {
					case ListEditKind.Replace:
						return list.ReplaceRange (_originalToken, _newTokens);

					case ListEditKind.InsertAfter:
						return list.InsertRange (index + 1, _newTokens);

					case ListEditKind.InsertBefore:
						return list.InsertRange (index, _newTokens);
					}
				}

				return base.VisitList (list);
			}*/
        }
    }
}
