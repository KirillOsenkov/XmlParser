namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class KeywordSyntax : SyntaxToken
    {
        internal new class Green : SyntaxToken.Green
        {
            internal Green(string name, GreenNode leadingTrivia, GreenNode trailingTrivia)
                : base(SyntaxKind.XmlKeyword, name, leadingTrivia, trailingTrivia)
            {
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new KeywordSyntax(this, parent, position);

            public override SyntaxToken.Green WithLeadingTrivia(GreenNode trivia)
            {
                return new Green(Text, trivia, TrailingTrivia);
            }

            public override SyntaxToken.Green WithTrailingTrivia(GreenNode trivia)
            {
                return new Green(Text, LeadingTrivia, trivia);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        public string Keyword => Text;

        internal KeywordSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxToken WithLeadingTrivia(SyntaxNode trivia)
        {
			return (KeywordSyntax)new Green(Text, trivia.GreenNode, GetTrailingTrivia().Node?.GreenNode).CreateRed(Parent, Start);
        }

        public override SyntaxToken WithTrailingTrivia(SyntaxNode trivia)
        {
			return (KeywordSyntax)new Green(Text, GetLeadingTrivia().Node?.GreenNode, trivia.GreenNode).CreateRed(Parent, Start);
        }
    }
}
