using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlElementStartTagSyntax : XmlNodeSyntax, INamedXmlNode
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly SyntaxToken.Green lessThanToken;
            readonly XmlNameSyntax.Green name;
            readonly GreenNode attributes;
            readonly SyntaxToken.Green slashGreaterThanToken;

            internal XmlNameSyntax.Green NameNode => name;
            internal SyntaxToken.Green LessThanToken => lessThanToken;
            internal GreenNode Attributes => attributes;
            internal SyntaxToken.Green GreaterThanToken => slashGreaterThanToken;

            internal Green(SyntaxToken.Green lessThanToken, XmlNameSyntax.Green name, GreenNode attributes, SyntaxToken.Green slashGreaterThanToken)
                : base(SyntaxKind.XmlElementStartTag)
            {
                this.SlotCount = 4;
                this.lessThanToken = lessThanToken;
                AdjustWidth(lessThanToken);
                this.name = name;
                AdjustWidth(name);
                this.attributes = attributes;
                AdjustWidth(attributes);
                this.slashGreaterThanToken = slashGreaterThanToken;
                AdjustWidth(slashGreaterThanToken);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlElementStartTagSyntax(this, parent, position);

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return lessThanToken;
                    case 1: return name;
                    case 2: return attributes;
                    case 3: return slashGreaterThanToken;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlElementStartTag(this);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        PunctuationSyntax lessThanToken;
        XmlNameSyntax nameNode;
        SyntaxNode attributesNode;
        PunctuationSyntax greaterThanToken;

        public PunctuationSyntax LessThanToken => GetRed(ref lessThanToken, 0);
        public XmlNameSyntax NameNode => GetRed(ref nameNode, 1);
        public SyntaxList<XmlAttributeSyntax> AttributesNode => new SyntaxList<XmlAttributeSyntax>(GetRed(ref attributesNode, 2));
        public PunctuationSyntax GreaterThanToken => GetRed(ref greaterThanToken, 3);

        public string Name => NameNode?.FullName;

        internal XmlElementStartTagSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlElementStartTag(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return lessThanToken;
                case 1: return nameNode;
                case 2: return attributesNode;
                case 3: return greaterThanToken;
                default: return null;
            }
        }

        internal override SyntaxNode GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return LessThanToken;
                case 1: return NameNode;
                case 2: return GetRed(ref attributesNode, 2);
                case 3: return GreaterThanToken;
                default: return null;
            }
        }

        public XmlElementStartTagSyntax Update(PunctuationSyntax lessThanToken, XmlNameSyntax name, SyntaxList<XmlAttributeSyntax> attributes, PunctuationSyntax greaterThanToken)
        {
            if (lessThanToken != this.LessThanToken || name != this.NameNode || attributes != this.AttributesNode || greaterThanToken != this.GreaterThanToken)
            {
                var newNode = SyntaxFactory.XmlElementStartTag(lessThanToken, name, attributes, greaterThanToken);
                /*var annotations = this.GetAnnotations ();
				if (annotations != null && annotations.Length > 0)
					return newNode.WithAnnotations (annotations);*/
                return newNode;
            }

            return this;
        }

        public XmlElementStartTagSyntax WithLessThanToken(PunctuationSyntax lessThanToken)
        {
            return this.Update(lessThanToken, this.NameNode, this.AttributesNode, this.GreaterThanToken);
        }

        public XmlElementStartTagSyntax WithName(XmlNameSyntax name)
        {
            return this.Update(this.LessThanToken, name, this.AttributesNode, this.GreaterThanToken);
        }

        public XmlElementStartTagSyntax WithAttributes(SyntaxList<XmlAttributeSyntax> attributes)
        {
            return this.Update(this.LessThanToken, this.NameNode, attributes, this.GreaterThanToken);
        }

        public XmlElementStartTagSyntax WithGreaterThanToken(PunctuationSyntax greaterThanToken)
        {
            return this.Update(this.LessThanToken, this.NameNode, this.AttributesNode, greaterThanToken);
        }

        public XmlElementStartTagSyntax AddAttributes(params XmlAttributeSyntax[] items)
        {
            return this.WithAttributes(this.AttributesNode.AddRange(items));
        }

        /*public override SyntaxNode WithLeadingTrivia(SyntaxNode trivia)
        {
            return new XmlElementStartTagSyntax(Kind,
                                                (PunctuationSyntax)LessThanToken.WithLeadingTrivia(trivia),
                                                NameNode,
                                                Attributes,
                                                GreaterThanToken);
        }

        public override SyntaxNode WithTrailingTrivia(SyntaxNode trivia)
        {
            return new XmlElementStartTagSyntax(Kind,
                                                LessThanToken,
                                                NameNode,
                                                Attributes,
                                                (PunctuationSyntax)GreaterThanToken.WithTrailingTrivia(trivia));
        }*/
    }
}
