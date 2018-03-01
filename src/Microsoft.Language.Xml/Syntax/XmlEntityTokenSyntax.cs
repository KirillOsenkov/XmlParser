namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlEntityTokenSyntax : XmlTextTokenSyntax
    {
        internal new class Green : XmlTextTokenSyntax.Green
        {
            public string Value { get; }

            internal Green(string text, string value, GreenNode leadingTrivia, GreenNode trailingTrivia)
                : base(SyntaxKind.XmlEntityLiteralToken, text, leadingTrivia, trailingTrivia)
            {
                Value = value;
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlEntityTokenSyntax(this, parent, position);
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        public string Entity => Text;
        public string EntityValue => GreenNode.Value;

        internal XmlEntityTokenSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        internal override SyntaxToken WithLeadingTriviaCore(SyntaxNode trivia)
        {
            return (XmlEntityTokenSyntax)new Green(Entity, EntityValue, trivia.GreenNode, GetTrailingTrivia().Node?.GreenNode).CreateRed(Parent, Start);
        }

        internal override SyntaxToken WithTrailingTriviaCore(SyntaxNode trivia)
        {
            return (XmlEntityTokenSyntax)new Green(Entity, EntityValue, GetLeadingTrivia().Node?.GreenNode, trivia.GreenNode).CreateRed(Parent, Start);
        }
    }
}
