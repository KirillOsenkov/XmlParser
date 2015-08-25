namespace Microsoft.Language.Xml
{
    public class XmlCDataSectionSyntax : XmlNodeSyntax
    {
        public PunctuationSyntax BeginCData;
        public PunctuationSyntax EndCData;
        public SyntaxNode TextTokens;

        public XmlCDataSectionSyntax(
            SyntaxKind kind,
            PunctuationSyntax beginCData,
            SyntaxNode node,
            PunctuationSyntax endCData) : base(kind)
        {
            this.BeginCData = beginCData;
            this.TextTokens = node;
            this.EndCData = endCData;
            SlotCount = 3;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return BeginCData;
                case 1:
                    return TextTokens;
                case 2:
                    return EndCData;
                default:
                    throw null;
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlCDataSection(this);
        }
    }
}
