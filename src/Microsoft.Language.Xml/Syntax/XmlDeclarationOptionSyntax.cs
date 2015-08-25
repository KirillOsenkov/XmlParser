using System;

namespace Microsoft.Language.Xml
{
    public class XmlDeclarationOptionSyntax : XmlNodeSyntax
    {
        public XmlStringSyntax Value { get; set; }
        public new PunctuationSyntax Equals { get; set; }
        public XmlNameTokenSyntax Name { get; set; }

        public XmlDeclarationOptionSyntax(SyntaxKind kind, XmlNameTokenSyntax name, PunctuationSyntax equals, XmlStringSyntax value)
            : base(kind)
        {
            this.Name = name;
            this.Equals = equals;
            this.Value = value;
            this.SlotCount = 3;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0: return Name;
                case 1: return Equals;
                case 2: return Value;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlDeclarationOption(this);
        }
    }
}
