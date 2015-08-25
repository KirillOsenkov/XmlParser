using System;

namespace Microsoft.Language.Xml
{
    public class XmlTextSyntax : XmlNodeSyntax
    {
        public SyntaxNode TextTokens { get; set; }

        public XmlTextSyntax(SyntaxKind kind, SyntaxNode textTokens) : base(kind)
        {
            this.TextTokens = textTokens;
            this.SlotCount = 1;
        }

        public override SyntaxNode GetSlot(int index)
        {
            if (index == 0)
            {
                return TextTokens;
            }

            throw new InvalidOperationException();
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlText(this);
        }
    }
}
