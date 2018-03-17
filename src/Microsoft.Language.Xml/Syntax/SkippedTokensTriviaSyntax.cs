using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class SkippedTokensTriviaSyntax : SyntaxNode
    {
        internal class Green : GreenNode
        {
            readonly GreenNode tokens;

            internal Green(GreenNode tokens)
                : base(SyntaxKind.SkippedTokensTrivia)
            {
                this.SlotCount = 1;
                this.tokens = tokens;
                AdjustWidth(tokens);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new SkippedTokensTriviaSyntax(this, parent, position);

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return tokens;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitSkippedTokensTrivia(this);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        SyntaxNode textTokens;

        public SyntaxList<SyntaxToken> Tokens => new SyntaxList<SyntaxToken>(GetRed(ref textTokens, 0));

        internal SkippedTokensTriviaSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public string Value => Tokens.Node?.ToFullString() ?? string.Empty;

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitSkippedTokensTrivia(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return textTokens;
                default: return null;
            }
        }

        internal override SyntaxNode GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return GetRed(ref textTokens, 0);
                default: return null;
            }
        }
    }
}
