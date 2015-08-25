namespace Microsoft.Language.Xml
{
    public class XmlTextTokenSyntax : SyntaxToken
    {
        public string Value { get; private set; }

        public XmlTextTokenSyntax(
            SyntaxKind kind,
            string text,
            SyntaxNode leadingTrivia,
            SyntaxNode trailingTrivia,
            string value)
            : base(kind, text, leadingTrivia, trailingTrivia)
        {
            this.Value = value;
        }
    }
}
