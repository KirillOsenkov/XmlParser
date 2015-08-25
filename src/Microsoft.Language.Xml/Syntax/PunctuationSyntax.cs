namespace Microsoft.Language.Xml
{
    public class PunctuationSyntax : SyntaxToken
    {
        public PunctuationSyntax(
            SyntaxKind kind,
            string text,
            SyntaxNode leadingTrivia,
            SyntaxNode trailingTrivia)
            : base(kind, text, leadingTrivia, trailingTrivia)
        {
        }

        public override SyntaxNode WithLeadingTrivia(SyntaxNode trivia)
        {
            return new PunctuationSyntax(Kind, Text, trivia, GetTrailingTrivia());
        }

        public override SyntaxNode WithTrailingTrivia(SyntaxNode trivia)
        {
            return new PunctuationSyntax(Kind, Text, GetLeadingTrivia(), trivia);
        }
    }
}
