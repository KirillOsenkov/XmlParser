using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlTextSyntax : XmlNodeSyntax
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly GreenNode value;

            internal InternalSyntax.SyntaxList<GreenNode> TextTokens => new InternalSyntax.SyntaxList<GreenNode>(value);

            internal Green(GreenNode value)
                : base(SyntaxKind.XmlText)
            {
                this.SlotCount = 1;
                this.value = value;
                AdjustWidth(value);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlTextSyntax(this, parent, position);

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return value;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlText(this);
            }
        }

        SyntaxNode textTokens;

        public SyntaxList<SyntaxNode> TextTokens => new SyntaxList<SyntaxNode>(GetRed(ref textTokens, 0));

        internal XmlTextSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public string Value => TextTokens.Node?.ToFullString() ?? string.Empty;

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlText(this);
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
