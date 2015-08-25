using System;

namespace Microsoft.Language.Xml
{
    public class XmlNameSyntax : XmlNodeSyntax
    {
        public XmlNameTokenSyntax LocalName { get; set; }
        public XmlPrefixSyntax Prefix { get; set; }

        public XmlNameSyntax(XmlPrefixSyntax prefix, XmlNameTokenSyntax localName)
            : base(SyntaxKind.XmlName)
        {
            SlotCount = 2;
            this.Prefix = prefix;
            this.LocalName = localName;
        }

        public override SyntaxNode GetSlot(int index)
        {
            if (index == 0)
            {
                return Prefix;
            }
            else if (index == 1)
            {
                return LocalName;
            }

            throw new InvalidOperationException();
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlName(this);
        }

        public string Name
        {
            get
            {
                if (LocalName == null)
                {
                    return null;
                }

                return LocalName.Text;
            }
        }
    }
}
