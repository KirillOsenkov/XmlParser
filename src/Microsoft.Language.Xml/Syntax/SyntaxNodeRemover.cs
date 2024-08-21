using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Language.Xml
{
    public class SyntaxNodeRemover
    {
        internal static TRoot RemoveNodes<TRoot>(TRoot root, IEnumerable<SyntaxNode> nodes, SyntaxRemoveOptions options) where TRoot : SyntaxNode
        {
            SyntaxNode[] nodesToRemove = nodes.ToArray();
            if (nodesToRemove.Length == 0)
                return root;
            var remover = new SyntaxRemover(nodes.ToArray(), options);
            var result = remover.Visit(root);
            var residualTrivia = remover.ResidualTrivia;
            if (residualTrivia.Count > 0)
                result = result.WithTrailingTrivia(result.GetTrailingTrivia().Concat(residualTrivia));
            return (TRoot)result;
        }

        private class SyntaxRemover : SyntaxRewriter
        {
            private readonly HashSet<SyntaxNode> _nodesToRemove;
            private readonly SyntaxRemoveOptions _options;
            private readonly TextSpan _searchSpan;
            private readonly SyntaxTriviaListBuilder _residualTrivia;

            public SyntaxRemover(SyntaxNode[] nodes, SyntaxRemoveOptions options)
            {
                this._nodesToRemove = new HashSet<SyntaxNode>(nodes);
                this._options = options;
                this._searchSpan = ComputeTotalSpan(nodes);
                this._residualTrivia = SyntaxTriviaListBuilder.Create();
            }

            private static TextSpan ComputeTotalSpan(SyntaxNode[] nodes)
            {
                var span0 = nodes[0].FullSpan;
                int start = span0.Start;
                int end = span0.End;
                int i = 1;
                while (i < nodes.Length)
                {
                    var span = nodes[i].FullSpan;
                    start = Math.Min(start, span.Start);
                    end = Math.Max(end, span.End);
                    i = i + 1;
                }

                return new TextSpan(start, end - start);
            }

            internal SyntaxTriviaList ResidualTrivia
            {
                get
                {
                    if (this._residualTrivia != null)
                        return this._residualTrivia.ToList();
                    else
                        return default(SyntaxTriviaList);
                }
            }

            private void AddResidualTrivia(SyntaxTriviaList trivia, bool requiresNewLine = false)
            {
                if (requiresNewLine)
                    AddEndOfLine();
                this._residualTrivia.Add(trivia);
            }

            private void AddEndOfLine()
            {
                if (this._residualTrivia.Count == 0 || !IsEndOfLine(this._residualTrivia[this._residualTrivia.Count - 1]))
                    this._residualTrivia.Add(SyntaxFactory.CarriageReturnLineFeed);
            }

            private static bool IsEndOfLine(SyntaxTrivia trivia)
            {
                return trivia.Kind == SyntaxKind.EndOfLineTrivia;
            }

            private static bool HasEndOfLine(SyntaxTriviaList trivia)
            {
                return trivia.Any(t => IsEndOfLine(t));
            }

            private bool IsForRemoval(SyntaxNode node)
            {
                return this._nodesToRemove.Contains(node);
            }

            private bool ShouldVisit(SyntaxNode node)
            {
                return node.FullSpan.IntersectsWith(this._searchSpan) || (this._residualTrivia != null && this._residualTrivia.Count > 0);
            }

            [return: NotNullIfNotNull(nameof(node))]
            public override SyntaxNode? Visit(SyntaxNode? node)
            {
                var result = node;
                if (node != null)
                    if (this.IsForRemoval(node))
                    {
                        this.AddTrivia(node);
                        result = null /* TODO Change to default(_) if this is not a reference type */;
                    }
                    else if (this.ShouldVisit(node))
                        result = base.Visit(node);
                return result;
            }

            public override SyntaxToken VisitSyntaxToken(SyntaxToken token)
            {
                var result = token;
                if (result.Kind != SyntaxKind.None && this._residualTrivia != null && this._residualTrivia.Count > 0)
                {
                    this._residualTrivia.Add(result.GetLeadingTrivia());
                    result = result.WithLeadingTrivia(this._residualTrivia.ToList());
                    this._residualTrivia.Clear();
                }

                return result;
            }

            private void AddTrivia(SyntaxNode node)
            {
                if ((this._options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                    this.AddResidualTrivia(node.GetLeadingTrivia());
                else if ((this._options & SyntaxRemoveOptions.KeepEndOfLine) != 0 && HasEndOfLine(node.GetLeadingTrivia()))
                    this.AddEndOfLine();
                if ((this._options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                    this.AddResidualTrivia(node.GetTrailingTrivia());
                else if ((this._options & SyntaxRemoveOptions.KeepEndOfLine) != 0 && HasEndOfLine(node.GetTrailingTrivia()))
                    this.AddEndOfLine();
                /*if ((this._options & SyntaxRemoveOptions.AddElasticMarker) != 0)
                    this.AddResidualTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker));*/
            }

            private void AddTrivia(SyntaxToken token, SyntaxNode node)
            {
                if ((this._options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                {
                    this.AddResidualTrivia(token.GetLeadingTrivia());
                    this.AddResidualTrivia(token.GetTrailingTrivia());
                    this.AddResidualTrivia(node.GetLeadingTrivia());
                }
                else if ((this._options & SyntaxRemoveOptions.KeepEndOfLine) != 0 && (HasEndOfLine(token.GetLeadingTrivia()) || HasEndOfLine(token.GetTrailingTrivia()) || HasEndOfLine(node.GetLeadingTrivia())))
                    this.AddEndOfLine();

                if ((this._options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                    this.AddResidualTrivia(node.GetTrailingTrivia());
                else if ((this._options & SyntaxRemoveOptions.KeepEndOfLine) != 0 && HasEndOfLine(node.GetTrailingTrivia()))
                    this.AddEndOfLine();
            }

            private void AddTrivia(SyntaxNode node, SyntaxToken token)
            {
                if ((this._options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                    this.AddResidualTrivia(node.GetLeadingTrivia());
                else if ((this._options & SyntaxRemoveOptions.KeepEndOfLine) != 0 && HasEndOfLine(node.GetLeadingTrivia()))
                    this.AddEndOfLine();

                if ((this._options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                {
                    this.AddResidualTrivia(node.GetTrailingTrivia());
                    this.AddResidualTrivia(token.GetLeadingTrivia());
                    this.AddResidualTrivia(token.GetTrailingTrivia());
                }
                else if ((this._options & SyntaxRemoveOptions.KeepEndOfLine) != 0 && (HasEndOfLine(node.GetTrailingTrivia()) || HasEndOfLine(token.GetLeadingTrivia()) || HasEndOfLine(token.GetTrailingTrivia())))
                    this.AddEndOfLine();
            }

            private TextSpan GetRemovedSpan(TextSpan span, TextSpan fullSpan)
            {
                var removedSpan = fullSpan;
                if ((this._options & SyntaxRemoveOptions.KeepLeadingTrivia) != 0)
                    removedSpan = TextSpan.FromBounds(span.Start, removedSpan.End);
                if ((this._options & SyntaxRemoveOptions.KeepTrailingTrivia) != 0)
                    removedSpan = TextSpan.FromBounds(removedSpan.Start, span.End);
                return removedSpan;
            }
        }
    }

    [Flags]
    public enum SyntaxRemoveOptions
    {
        /// <summary>
        /// None of the trivia associated with the node or token is kept.
        /// </summary>
        KeepNoTrivia = 0x0,

        /// <summary>
        /// The leading trivia associated with the node or token is kept.
        /// </summary>
        KeepLeadingTrivia = 0x1,

        /// <summary>
        /// The trailing trivia associated with the node or token is kept.
        /// </summary>
        KeepTrailingTrivia = 0x2,

        /// <summary>
        /// The leading and trailing trivia associated with the node or token is kept.
        /// </summary>
        KeepExteriorTrivia = KeepLeadingTrivia | KeepTrailingTrivia,

        /// <summary>
        /// Ensure that at least one EndOfLine trivia is kept if one was present
        /// </summary>
        KeepEndOfLine = 0x10,

        /// <summary>
        /// Adds elastic marker trivia
        /// </summary>
        //AddElasticMarker = 0x20
    }
}
