namespace Microsoft.Language.Xml
{
    internal class KeywordSyntax : SyntaxToken
    {
        public KeywordSyntax(
            SyntaxKind kind,
            string text,
            SyntaxNode leadingTrivia,
            SyntaxNode trailingTrivia)
            : base(kind, text, leadingTrivia, trailingTrivia)
        {
        }

        public override SyntaxNode WithLeadingTrivia(SyntaxNode trivia)
        {
            return new KeywordSyntax(Kind, Text, trivia, GetTrailingTrivia());
        }

        public override SyntaxNode WithTrailingTrivia(SyntaxNode trivia)
        {
            return new KeywordSyntax(Kind, Text, GetLeadingTrivia(), trivia);
        }
    }
}
