using System;
using System.Diagnostics;

namespace Microsoft.Language.Xml.InternalSyntax
{
    internal class TriviaInfo
    {
        private TriviaInfo(GreenNode leadingTrivia, GreenNode trailingTrivia)
        {
            this._leadingTrivia = leadingTrivia;
            this._trailingTrivia = trailingTrivia;
        }

        private const int maximumCachedTriviaWidth = 40;
        private const int triviaInfoCacheSize = 64;
        private static readonly Func<GreenNode, int> triviaKeyHasher = (GreenNode key) => Hash.Combine(key.ToFullString(), ((short)key.Kind));
        private static readonly Func<GreenNode, TriviaInfo, bool> triviaKeyEquality = (GreenNode key, TriviaInfo value) => (key == value._leadingTrivia) || ((key.Kind == value._leadingTrivia.Kind) && (key.FullWidth == value._leadingTrivia.FullWidth) && (key.ToFullString() == value._leadingTrivia.ToFullString()));

        private static bool ShouldCacheTriviaInfo(GreenNode leadingTrivia, GreenNode trailingTrivia)
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

        public static TriviaInfo Create(GreenNode leadingTrivia, GreenNode trailingTrivia)
        {
            Debug.Assert(leadingTrivia != null);
            return new TriviaInfo(leadingTrivia, trailingTrivia);
        }

        public GreenNode _leadingTrivia;
        public GreenNode _trailingTrivia;
    }
}
