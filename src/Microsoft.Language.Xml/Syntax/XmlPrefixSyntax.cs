using System;

namespace Microsoft.Language.Xml
{
    public class XmlPrefixSyntax : SyntaxNode
    {
        public XmlNameTokenSyntax Name { get; private set; }
        public PunctuationSyntax ColonToken { get; private set; }

        public XmlPrefixSyntax(
            XmlNameTokenSyntax nameToken,
            PunctuationSyntax colonToken) : base(SyntaxKind.XmlPrefix)
        {
            Name = nameToken;
            ColonToken = colonToken;
            SlotCount = 2;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return Name;
                case 1:
                    return ColonToken;
            }

            throw new InvalidOperationException();
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlPrefix(this);
        }
    }
}
