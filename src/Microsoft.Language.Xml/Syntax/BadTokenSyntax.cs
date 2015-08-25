namespace Microsoft.Language.Xml
{
    public class BadTokenSyntax : PunctuationSyntax
    {
        public BadTokenSyntax(SyntaxKind subkind, string text, SyntaxNode leading, SyntaxNode trailing)
            : base(subkind, text, leading, trailing)
        {
        }

        public SyntaxSubKind SubKind { get; set; }
    }
}
