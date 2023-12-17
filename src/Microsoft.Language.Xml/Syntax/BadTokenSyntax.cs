using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class BadTokenSyntax : PunctuationSyntax
    {
        internal new class Green : PunctuationSyntax.Green
        {
            public SyntaxSubKind SubKind { get; }

            internal Green(SyntaxSubKind subKind, string name, GreenNode leadingTrivia, GreenNode trailingTrivia)
                : base(SyntaxKind.BadToken, name, leadingTrivia, trailingTrivia)
            {
                SubKind = subKind;
            }

            internal Green(SyntaxSubKind subKind, string name, GreenNode leadingTrivia, GreenNode trailingTrivia, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(SyntaxKind.BadToken, name, leadingTrivia, trailingTrivia, diagnostics, annotations)
            {
                SubKind = subKind;
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new BadTokenSyntax(this, parent, position);

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new Green(SubKind, Text, LeadingTrivia, TrailingTrivia, diagnostics, GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new Green(SubKind, Text, LeadingTrivia, TrailingTrivia, GetDiagnostics(), annotations);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        public SyntaxSubKind SubKind => GreenNode.SubKind;

        internal BadTokenSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }
    }
}
