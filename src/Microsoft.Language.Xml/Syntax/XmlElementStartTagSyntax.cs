using System;

namespace Microsoft.Language.Xml
{
    public class XmlElementStartTagSyntax : XmlNodeSyntax
    {
        public SyntaxNode Attributes { get; set; }
        public PunctuationSyntax GreaterThanToken { get; set; }
        public PunctuationSyntax LessThanToken { get; set; }
        public XmlNameSyntax NameNode { get; set; }

        public XmlElementStartTagSyntax(
            SyntaxKind kind,
            PunctuationSyntax lessThanToken,
            XmlNameSyntax name,
            SyntaxNode attributes,
            PunctuationSyntax greaterThanToken)
            : base(kind)
        {
            this.LessThanToken = lessThanToken;
            this.NameNode = name;
            this.Attributes = attributes;
            this.GreaterThanToken = greaterThanToken;
            SlotCount = 4;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0: return LessThanToken;
                case 1: return NameNode;
                case 2: return Attributes;
                case 3: return GreaterThanToken;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlElementStartTag(this);
        }

        public string Name
        {
            get
            {
                if (NameNode == null)
                {
                    return null;
                }

                return NameNode.Name;
            }
        }
    }
}
