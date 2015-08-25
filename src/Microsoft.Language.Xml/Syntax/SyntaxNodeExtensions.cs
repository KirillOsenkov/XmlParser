using System;
using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    public class DiagnosticInfo
    {
    }

    public static class SyntaxNodeExtensions
    {
        internal static TSyntax AddLeadingTrivia<TSyntax>(this TSyntax node, SyntaxList<SyntaxNode> trivia) where TSyntax : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (!trivia.Any())
            {
                return node;
            }

            var tk = (node as SyntaxToken);
            TSyntax result;
            if (tk != null)
            {
                // Cannot add unexpected tokens as leading trivia on a missing token since
                // if the unexpected tokens end with a statement terminator, the missing
                // token would follow the statement terminator. That would result in an
                // incorrect syntax tree and if this missing token is the end of an expression,
                // and the expression represents a transition between VB and XML, the
                // terminator will be overlooked (see ParseXmlEmbedded for instance).
                if (IsMissingToken(tk))
                {
                    var leadingTrivia = trivia.GetStartOfTrivia();
                    var trailingTrivia = trivia.GetEndOfTrivia();
                    tk = SyntaxToken.AddLeadingTrivia(tk, leadingTrivia).AddTrailingTrivia(trailingTrivia);
                }
                else
                {
                    tk = SyntaxToken.AddLeadingTrivia(tk, trivia);
                }

                result = ((TSyntax)((object)tk));
            }
            else
            {
                result = FirstTokenReplacer.Replace(node, t => SyntaxToken.AddLeadingTrivia(t, trivia));
            }

            return result;
        }

        internal static SyntaxList<SyntaxNode> GetStartOfTrivia(this SyntaxList<SyntaxNode> trivia)
        {
            return trivia.GetStartOfTrivia(trivia.GetIndexOfEndOfTrivia());
        }

        internal static SyntaxList<SyntaxNode> GetStartOfTrivia(this SyntaxList<SyntaxNode> trivia, int indexOfEnd)
        {
            if (indexOfEnd == 0)
            {
                return null;
            }
            else if (indexOfEnd == trivia.Count)
            {
                return trivia;
            }
            else
            {
                var builder = SyntaxListBuilder<SyntaxNode>.Create();
                for (var i = 0; i < indexOfEnd; i++)
                {
                    builder.Add(trivia[i]);
                }

                return builder.ToList();
            }
        }

        internal static SyntaxList<SyntaxNode> GetEndOfTrivia(this SyntaxList<SyntaxNode> trivia)
        {
            return trivia.GetEndOfTrivia(trivia.GetIndexOfEndOfTrivia());
        }

        /*  <summary>
         ''' Return the index within the trivia of what would be considered trailing
         ''' single-line trivia by the Scanner. This behavior must match ScanSingleLineTrivia.
         ''' In short, search walks backwards and stops at the second terminator
         ''' (colon or EOL) from the end, ignoring EOLs preceeded by line continuations.
         ''' </summary>
        */
        private static int GetIndexOfEndOfTrivia(this SyntaxList<SyntaxNode> trivia)
        {
            var n = trivia.Count;
            if (n > 0)
            {
                var i = n - 1;
                switch (trivia[i].Kind)
                {
                    case SyntaxKind.EndOfLineTrivia:
                        if (i > 0)
                        {
                            switch (trivia[i - 1].Kind)
                            {
                                default:
                                    return i;
                            }
                        }
                        else
                        {
                            return i;
                        }
                }
            }

            return n;
        }

        internal static SyntaxList<SyntaxNode> GetEndOfTrivia(this SyntaxList<SyntaxNode> trivia, int indexOfEnd)
        {
            if (indexOfEnd == 0)
            {
                return trivia;
            }
            else if (indexOfEnd == trivia.Count)
            {
                return null;
            }
            else
            {
                var builder = SyntaxListBuilder<SyntaxNode>.Create();
                for (var i = indexOfEnd; i < trivia.Count; i++)
                {
                    builder.Add(trivia[i]);
                }

                return builder.ToList();
            }
        }

        internal static bool IsMissingToken(SyntaxToken token)
        {
            return token.FullWidth == 0;
        }

        internal static TSyntax AddLeadingSyntax<TSyntax>(this TSyntax node, SyntaxNode unexpected, ERRID errorId) where TSyntax : SyntaxNode
        {
            var diagnostic = ErrorFactory.ErrorInfo(errorId);
            if (unexpected != null)
            {
                SyntaxList<SyntaxNode> trivia = CreateSkippedTrivia(
                    unexpected,
                    preserveDiagnostics: false,
                    addDiagnosticToFirstTokenOnly: false,
                    addDiagnostic: diagnostic);
                return AddLeadingTrivia(node, trivia);
            }
            else
            {
                return ((TSyntax)node.AddError(diagnostic));
            }
        }

        internal static TSyntax AddLeadingSyntax<TSyntax>(this TSyntax node, SyntaxList<SyntaxToken> unexpected, ERRID errorId) where TSyntax : SyntaxNode
        {
            return AddLeadingSyntax(node, unexpected.Node, errorId);
        }

        internal static TSyntax AddTrailingSyntax<TSyntax>(this TSyntax node, SyntaxNode unexpected, ERRID errorId) where TSyntax : SyntaxNode
        {
            var diagnostic = ErrorFactory.ErrorInfo(errorId);
            if (unexpected != null)
            {
                SyntaxList<SyntaxNode> trivia = CreateSkippedTrivia(
                    unexpected,
                    preserveDiagnostics: false,
                    addDiagnosticToFirstTokenOnly: false,
                    addDiagnostic: diagnostic);
                return AddTrailingTrivia(node, trivia);
            }
            else
            {
                return ((TSyntax)node.AddError(diagnostic));
            }
        }

        internal static TSyntax AddTrailingSyntax<TSyntax>(this TSyntax node, SyntaxList<SyntaxToken> unexpected) where TSyntax : SyntaxNode
        {
            return node.AddTrailingSyntax(unexpected.Node);
        }

        internal static TSyntax AddTrailingSyntax<TSyntax>(this TSyntax node, SyntaxNode unexpected) where TSyntax : SyntaxNode
        {
            if (unexpected != null)
            {
                SyntaxList<SyntaxNode> trivia = CreateSkippedTrivia(
                    unexpected,
                    preserveDiagnostics: true,
                    addDiagnosticToFirstTokenOnly: false,
                    addDiagnostic: null);
                return AddTrailingTrivia(node, trivia);
            }

            return node;
        }

        internal static TSyntax AddTrailingTrivia<TSyntax>(this TSyntax node, SyntaxList<SyntaxNode> trivia) where TSyntax : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            var tk = (node as SyntaxToken);
            TSyntax result;
            if (tk != null)
            {
                result = ((TSyntax)((object)SyntaxToken.AddTrailingTrivia(tk, trivia)));
            }
            else
            {
                result = LastTokenReplacer.Replace(node, t => SyntaxToken.AddTrailingTrivia(t, trivia));
            }

            return result;
        }

        // From a syntax node, create a list of trivia node that encapsulates the same text. We use SkippedTokens trivia
        // to encapsulate the tokens, plus extract trivia from those tokens into the trivia list because:
        //    - We want leading trivia and trailing trivia to be directly visible in the trivia list, not on the tokens
        //      inside the skipped tokens trivia.
        //    - We have to expose structured trivia directives.
        //
        // Several options controls how diagnostics are handled:
        //   "preserveDiagnostics" means existing diagnostics are preserved, otherwise they are thrown away
        //   "addDiagnostic", if not Nothing, is added as a diagnostics
        //   "addDiagnosticsToFirstTokenOnly" means that "addDiagnostics" is attached only to the first token, otherwise
        //    it is attached to all tokens.
        private static SyntaxList<SyntaxNode> CreateSkippedTrivia(SyntaxNode node, bool preserveDiagnostics, bool addDiagnosticToFirstTokenOnly, DiagnosticInfo addDiagnostic)
        {
            if (node.Kind == SyntaxKind.SkippedTokensTrivia)
            {
                // already skipped trivia
                if (addDiagnostic != null)
                {
                    ////node = node.AddError(addDiagnostic); TODO
                }

                return node;
            }

            IList<DiagnosticInfo> diagnostics = new List<DiagnosticInfo>();
            var tokenListBuilder = SyntaxListBuilder<SyntaxToken>.Create();
            node.CollectConstituentTokensAndDiagnostics(tokenListBuilder, diagnostics);
            // Adjust diagnostics based on input.
            if (!preserveDiagnostics)
            {
                diagnostics.Clear();
            }

            if (addDiagnostic != null)
            {
                diagnostics.Add(addDiagnostic);
            }

            var skippedTriviaBuilder = new SkippedTriviaBuilder(preserveDiagnostics, addDiagnosticToFirstTokenOnly, diagnostics);

            // Get through each token and add it.
            for (int i = 0; i < tokenListBuilder.Count; i++)
            {
                SyntaxToken currentToken = tokenListBuilder[i];
                skippedTriviaBuilder.AddToken(currentToken, isFirst: (i == 0), isLast: (i == tokenListBuilder.Count - 1));
            }

            return skippedTriviaBuilder.GetTriviaList();
        }

        public static bool ContainsWhitespaceTrivia(this SyntaxNode node)
        {
            if (node == null)
            {
                return false;
            }

            var trivia = new SyntaxList<SyntaxNode>(node);
            for (var i = 0; i < trivia.Count; i++)
            {
                var kind = trivia.ItemUntyped(i).Kind;
                if (kind == SyntaxKind.WhitespaceTrivia || kind == SyntaxKind.EndOfLineTrivia)
                {
                    return true;
                }
            }

            return false;
        }

        // In order to handle creating SkippedTokens trivia correctly, we need to know if any structured
        // trivia is present in a trivia list (because structured trivia can't contain structured trivia).
        public static bool TriviaListContainsStructuredTrivia(this SyntaxNode triviaList)
        {
            if (triviaList == null)
            {
                return false;
            }

            var trivia = new SyntaxList<SyntaxNode>(triviaList);
            for (var i = 0; i < trivia.Count; i++)
            {
                switch (trivia.ItemUntyped(i).Kind)
                {
                    case SyntaxKind.XmlDocument:
                    case SyntaxKind.SkippedTokensTrivia:
                        return true;
                }
            }

            return false;
        }

        public static TNode WithAdditionalDiagnostics<TNode>(this TNode node, params DiagnosticInfo[] diagnostics) where TNode : SyntaxNode
        {
            return node;
            ////DiagnosticInfo[] current = node.GetDiagnostics();
            ////if (current != null)
            ////{
            ////    return ((TNode)node.SetDiagnostics(current.Concat(diagnostics).ToArray()));
            ////}
            ////else
            ////{
            ////    return node.WithDiagnostics(diagnostics);
            ////}
        }
    }
}
