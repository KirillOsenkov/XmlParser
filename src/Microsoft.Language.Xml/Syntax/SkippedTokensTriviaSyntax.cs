namespace Microsoft.Language.Xml
{
    public class SkippedTokensTriviaSyntax : StructuredTriviaSyntax
    {
        public SyntaxNode Tokens { get; set; }

        public SkippedTokensTriviaSyntax(
            SyntaxKind kind,
            SyntaxNode node)
            : base(kind)
        {
            this.Kind = kind;
            Tokens = node;
            SlotCount = 1;
        }

        public override SyntaxNode GetSlot(int index)
        {
            if (index == 0)
            {
                return Tokens;
            }

            throw null;
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitSkippedTokensTrivia(this);
        }
    }
}
