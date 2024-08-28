namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class PunctuationSyntax : SyntaxToken
    {
        internal new class Green : SyntaxToken.Green
        {
            internal Green(SyntaxKind kind, string name, GreenNode? leadingTrivia, GreenNode? trailingTrivia)
                : base(kind, name, leadingTrivia, trailingTrivia)
            {
            }

            internal Green(SyntaxKind kind, string name, GreenNode? leadingTrivia, GreenNode? trailingTrivia, DiagnosticInfo[]? diagnostics, SyntaxAnnotation[] annotations)
                : base(kind, name, leadingTrivia, trailingTrivia, diagnostics, annotations)
            {
            }

            internal override SyntaxNode CreateRed(SyntaxNode? parent, int position) => new PunctuationSyntax(this, parent, position);

            public override SyntaxToken.Green WithLeadingTrivia(GreenNode? trivia)
            {
                return new Green(Kind, Text, trivia, TrailingTrivia);
            }

            public override SyntaxToken.Green WithTrailingTrivia(GreenNode? trivia)
            {
                return new Green(Kind, Text, LeadingTrivia, trivia);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[]? diagnostics)
            {
                return new Green(Kind, Text, LeadingTrivia, TrailingTrivia, diagnostics, GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new Green(Kind, Text, LeadingTrivia, TrailingTrivia, GetDiagnostics(), annotations);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        public string Punctuation => Text;

        internal PunctuationSyntax(Green green, SyntaxNode? parent, int position)
            : base(green, parent, position)
        {

        }

        internal override SyntaxToken WithLeadingTriviaCore(SyntaxNode? trivia)
        {
            return (PunctuationSyntax)new Green(Kind, Text, trivia?.GreenNode, GetTrailingTrivia().Node?.GreenNode).CreateRed(Parent, Start);
        }

        internal override SyntaxToken WithTrailingTriviaCore(SyntaxNode? trivia)
        {
            return (PunctuationSyntax)new Green(Kind, Text, GetLeadingTrivia().Node?.GreenNode, trivia?.GreenNode).CreateRed(Parent, Start);
        }
    }
}
