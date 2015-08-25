using System;

namespace Microsoft.Language.Xml
{
    public class XmlStringSyntax : XmlNodeSyntax
    {
        public PunctuationSyntax StartQuoteToken { get; private set; }
        public PunctuationSyntax EndQuoteToken { get; private set; }
        public SyntaxList<SyntaxNode> TextTokens { get; private set; }

        public XmlStringSyntax(SyntaxKind kind, PunctuationSyntax startQuoteToken, SyntaxList<SyntaxNode> textTokens, PunctuationSyntax endQuoteToken)
            : base(kind)
        {
            this.StartQuoteToken = startQuoteToken;
            this.TextTokens = textTokens;
            this.EndQuoteToken = endQuoteToken;
            this.SlotCount = 3;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0: return StartQuoteToken;
                case 1: return TextTokens.Node;
                case 2: return EndQuoteToken;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlString(this);
        }
    }
}
