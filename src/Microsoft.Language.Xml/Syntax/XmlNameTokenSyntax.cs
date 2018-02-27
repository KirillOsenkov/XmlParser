using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlNameTokenSyntax : SyntaxToken
    {
        internal new class Green : SyntaxToken.Green
        {
            internal Green(string name, GreenNode leadingTrivia, GreenNode trailingTrivia)
                : base(SyntaxKind.XmlNameToken, name, leadingTrivia, trailingTrivia)
            {
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlNameTokenSyntax(this, parent, position);

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

        public string Name => Text;

        internal XmlNameTokenSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        internal override SyntaxToken WithLeadingTriviaCore(SyntaxNode trivia)
        {
			return (XmlNameTokenSyntax)new Green(Text, trivia.GreenNode, GetTrailingTrivia().Node?.GreenNode).CreateRed(Parent, Start);
        }

        internal override SyntaxToken WithTrailingTriviaCore(SyntaxNode trivia)
        {
			return (XmlNameTokenSyntax)new Green(Text, GetLeadingTrivia().Node?.GreenNode, trivia.GreenNode).CreateRed(Parent, Start);
        }
    }
}
