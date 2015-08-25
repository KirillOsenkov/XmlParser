using System;
using System.Diagnostics;

namespace Microsoft.Language.Xml
{
    internal class TriviaInfo
    {
        private TriviaInfo(SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
        {
            this._leadingTrivia = leadingTrivia;
            this._trailingTrivia = trailingTrivia;
        }

        private const int maximumCachedTriviaWidth = 40;
        private const int triviaInfoCacheSize = 64;
        private static readonly Func<SyntaxNode, int> triviaKeyHasher = (SyntaxNode key) => Hash.Combine(key.ToFullString(), ((short)key.Kind));
        private static readonly Func<SyntaxNode, TriviaInfo, bool> triviaKeyEquality = (SyntaxNode key, TriviaInfo value) => (key == value._leadingTrivia) || ((key.Kind == value._leadingTrivia.Kind) && (key.FullWidth == value._leadingTrivia.FullWidth) && (key.ToFullString() == value._leadingTrivia.ToFullString()));

        private static bool ShouldCacheTriviaInfo(SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
        {
            Debug.Assert(leadingTrivia != null);
            if (trailingTrivia == null)
            {
                return false;
            }
            else
            {
                return leadingTrivia.Kind == SyntaxKind.WhitespaceTrivia &&
                    trailingTrivia.Kind == SyntaxKind.WhitespaceTrivia &&
                    trailingTrivia.FullWidth == 1 &&
                    trailingTrivia.ToFullString() == " " &&
                    leadingTrivia.FullWidth <= maximumCachedTriviaWidth;
            }
        }

        public static TriviaInfo Create(SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
        {
            Debug.Assert(leadingTrivia != null);
            return new TriviaInfo(leadingTrivia, trailingTrivia);
        }

        public SyntaxNode _leadingTrivia;
        public SyntaxNode _trailingTrivia;
    }
}
