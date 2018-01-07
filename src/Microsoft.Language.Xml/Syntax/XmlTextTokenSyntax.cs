namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlTextTokenSyntax : SyntaxToken
    {
        internal new class Green : SyntaxToken.Green
        {
            internal Green(string text, GreenNode leadingTrivia, GreenNode trailingTrivia)
                : base(SyntaxKind.XmlTextLiteralToken, text, leadingTrivia, trailingTrivia)
            {
            }

            protected Green(SyntaxKind kind, string name, GreenNode leadingTrivia, GreenNode trailingTrivia)
                : base(kind, name, leadingTrivia, trailingTrivia)
            {
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlTextTokenSyntax(this, parent, position);

            public override GreenNode WithLeadingTrivia(GreenNode trivia)
            {
                return new Green(Kind, Text, trivia, TrailingTrivia);
            }

            public override GreenNode WithTrailingTrivia(GreenNode trivia)
            {
                return new Green(Kind, Text, LeadingTrivia, trivia);
            }
        }

        public string Value => Text;

        internal XmlTextTokenSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxToken WithLeadingTrivia(SyntaxNode trivia)
        {
            return (XmlTextTokenSyntax)new Green(Text, trivia.GreenNode, GetTrailingTrivia()?.GreenNode).CreateRed(Parent, Start);
        }

        public override SyntaxToken WithTrailingTrivia(SyntaxNode trivia)
        {
            return (XmlTextTokenSyntax)new Green(Text, GetLeadingTrivia()?.GreenNode, trivia.GreenNode).CreateRed(Parent, Start);
        }
    }
}
