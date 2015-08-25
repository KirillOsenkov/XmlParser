using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Language.Xml
{
    // Simple class to create the best representation of skipped trivia as a combination of "regular" trivia
    // and SkippedNode trivia. The initial trivia and trailing trivia are preserved as regular trivia, as well
    // as any structured trivia. We also remove any missing tokens and promote their trivia. Otherwise we try to put
    // as many consecutive tokens as possible into a SkippedTokens trivia node.
    internal class SkippedTriviaBuilder
    {
        private SyntaxListBuilder<SyntaxNode> triviaListBuilder = SyntaxListBuilder<SyntaxNode>.Create();
        private SyntaxListBuilder<SyntaxToken> skippedTokensBuilder = SyntaxListBuilder<SyntaxToken>.Create();
        private bool preserveExistingDiagnostics;
        private bool addDiagnosticsToFirstTokenOnly;
        private IEnumerable<DiagnosticInfo> diagnosticsToAdd;

        // Add a trivia to the triva we are accumulating.
        private void AddTrivia(SyntaxNode trivia)
        {
            FinishInProgressTokens();
            triviaListBuilder.AddRange(trivia);
        }

        // Create a SkippedTokens trivia from any tokens currently accumulated into the skippedTokensBuilder. If not,
        // don't do anything.
        private void FinishInProgressTokens()
        {
            if (skippedTokensBuilder.Count > 0)
            {
                var skippedTokensTrivia = SyntaxFactory.SkippedTokensTrivia(skippedTokensBuilder.ToList());
                if (diagnosticsToAdd != null)
                {
                    foreach (var d in diagnosticsToAdd)
                    {
                        ////skippedTokensTrivia = skippedTokensTrivia.AddError(d);
                    }

                    diagnosticsToAdd = null; // only add once.
                }

                triviaListBuilder.Add(skippedTokensTrivia);
                skippedTokensBuilder.Clear();
            }
        }

        public SkippedTriviaBuilder(bool preserveExistingDiagnostics, bool addDiagnosticsToFirstTokenOnly, IEnumerable<DiagnosticInfo> diagnosticsToAdd)
        {
            this.addDiagnosticsToFirstTokenOnly = addDiagnosticsToFirstTokenOnly;
            this.preserveExistingDiagnostics = preserveExistingDiagnostics;
            this.diagnosticsToAdd = diagnosticsToAdd;
        }

        // Process a token. and add to the list of triva/tokens we're accumulating.
        public void AddToken(SyntaxToken token, bool isFirst, bool isLast)
        {
            bool isMissing = token.IsMissing;
            if (token.HasLeadingTrivia && (isFirst || isMissing || token.GetLeadingTrivia().TriviaListContainsStructuredTrivia()))
            {
                FinishInProgressTokens();
                AddTrivia(token.GetLeadingTrivia());
                token = ((SyntaxToken)token.WithLeadingTrivia(null));
            }

            ////if (!preserveExistingDiagnostics)
            ////{
            ////    token = token.WithoutDiagnostics();
            ////}

            SyntaxNode trailingTrivia = null;
            if (token.HasTrailingTrivia && (isLast || isMissing || token.GetTrailingTrivia().TriviaListContainsStructuredTrivia()))
            {
                trailingTrivia = token.GetTrailingTrivia();
                token = ((SyntaxToken)token.WithTrailingTrivia(null));
            }

            if (isMissing)
            {
                // Don't add missing tokens to skipped tokens, but preserve their diagnostics.
                ////if (token.ContainsDiagnostics())
                ////{
                ////    // Move diagnostics on missing token to next token.
                ////    if (diagnosticsToAdd != null)
                ////    {
                ////        diagnosticsToAdd = diagnosticsToAdd.Concat(token.GetDiagnostics());
                ////    }
                ////    else
                ////    {
                ////        diagnosticsToAdd = token.GetDiagnostics();
                ////    }

                ////    addDiagnosticsToFirstTokenOnly = true;
                ////}
            }
            else
            {
                skippedTokensBuilder.Add(token);
            }

            if (trailingTrivia != null)
            {
                FinishInProgressTokens();
                AddTrivia(trailingTrivia);
            }

            if (isFirst && addDiagnosticsToFirstTokenOnly)
            {
                FinishInProgressTokens(); // implicitly adds the diagnostics.
            }
        }

        // Get the final list of trivia nodes we should attached.
        public SyntaxList<SyntaxNode> GetTriviaList()
        {
            FinishInProgressTokens();
            if (diagnosticsToAdd != null && diagnosticsToAdd.Any())
            {
                // Still have diagnostics. Add to the last item.
                if (triviaListBuilder.Count > 0)
                {
                    triviaListBuilder[triviaListBuilder.Count - 1] = triviaListBuilder[triviaListBuilder.Count - 1].WithAdditionalDiagnostics(diagnosticsToAdd.ToArray());
                }
            }

            return triviaListBuilder.ToList();
        }
    }
}
