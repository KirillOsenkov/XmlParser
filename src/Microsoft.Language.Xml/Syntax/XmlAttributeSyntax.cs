namespace Microsoft.Language.Xml
{
    public class XmlAttributeSyntax : XmlNodeSyntax
    {
        public new PunctuationSyntax Equals { get; set; }
        public XmlNameSyntax NameNode { get; set; }
        public XmlNodeSyntax ValueNode { get; set; }

        public XmlAttributeSyntax(XmlNameSyntax name, PunctuationSyntax equals, XmlNodeSyntax value)
            : base(SyntaxKind.XmlAttribute)
        {
            this.NameNode = name;
            this.Equals = equals;
            this.ValueNode = value;
            SlotCount = 3;
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

        public string Value
        {
            get
            {
                if (ValueNode == null)
                {
                    return null;
                }

                var xmlString = ValueNode as XmlStringSyntax;
                if (xmlString == null)
                {
                    return null;
                }

                return xmlString.TextTokens.Node.ToFullString();
            }
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return NameNode;
                case 1:
                    return Equals;
                case 2:
                    return ValueNode;
                default:
                    throw null;
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlAttribute(this);
        }
    }
}
