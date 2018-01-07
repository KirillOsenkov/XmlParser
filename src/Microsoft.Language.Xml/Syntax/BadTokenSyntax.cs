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

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new BadTokenSyntax(this, parent, position);
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        public SyntaxSubKind SubKind => GreenNode.SubKind;

        internal BadTokenSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }
    }
}
