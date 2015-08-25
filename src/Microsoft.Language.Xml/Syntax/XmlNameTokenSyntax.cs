namespace Microsoft.Language.Xml
{
    public class XmlNameTokenSyntax : SyntaxToken
    {
        public XmlNameTokenSyntax(
            string text, SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
            : base(SyntaxKind.XmlNameToken, text, leadingTrivia, trailingTrivia)
        {
        }

        public override SyntaxNode WithLeadingTrivia(SyntaxNode trivia)
        {
            return new XmlNameTokenSyntax(Text, trivia, GetTrailingTrivia());
        }

        public override SyntaxNode WithTrailingTrivia(SyntaxNode trivia)
        {
            return new XmlNameTokenSyntax(Text, GetLeadingTrivia(), trivia);
        }
    }
}
