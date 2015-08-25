using System;

namespace Microsoft.Language.Xml
{
    public class XmlElementEndTagSyntax : XmlNodeSyntax
    {
        public PunctuationSyntax GreaterThanToken { get; set; }
        public PunctuationSyntax LessThanSlashToken { get; set; }
        public XmlNameSyntax NameNode { get; set; }

        public XmlElementEndTagSyntax(SyntaxKind kind, PunctuationSyntax lessThanSlashToken, XmlNameSyntax name, PunctuationSyntax greaterThanToken) : base(kind)
        {
            this.LessThanSlashToken = lessThanSlashToken;
            this.NameNode = name;
            this.GreaterThanToken = greaterThanToken;
            this.SlotCount = 3;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0: return LessThanSlashToken;
                case 1: return NameNode;
                case 2: return GreaterThanToken;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlElementEndTag(this);
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
