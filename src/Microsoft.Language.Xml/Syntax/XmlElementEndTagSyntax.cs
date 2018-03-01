using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlElementEndTagSyntax : XmlNodeSyntax, INamedXmlNode
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly SyntaxToken.Green lessThanToken;
            readonly XmlNameSyntax.Green name;
            readonly SyntaxToken.Green slashGreaterThanToken;

            internal XmlNameSyntax.Green NameNode => name;
            internal SyntaxToken.Green LessThanSlashToken => lessThanToken;
            internal SyntaxToken.Green GreaterThanToken => slashGreaterThanToken;

            internal Green(SyntaxToken.Green lessThanToken, XmlNameSyntax.Green name, SyntaxToken.Green slashGreaterThanToken)
                : base(SyntaxKind.XmlElementEndTag)
            {
                this.SlotCount = 3;
                this.lessThanToken = lessThanToken;
                AdjustWidth(lessThanToken);
                this.name = name;
                AdjustWidth(name);
                this.slashGreaterThanToken = slashGreaterThanToken;
                AdjustWidth(slashGreaterThanToken);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlElementEndTagSyntax(this, parent, position);

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return lessThanToken;
                    case 1: return name;
                    case 2: return slashGreaterThanToken;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlElementEndTag(this);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        PunctuationSyntax lessThanToken;
        XmlNameSyntax nameNode;
        PunctuationSyntax slashGreaterThanToken;

        public PunctuationSyntax LessThanSlashToken => GetRed(ref lessThanToken, 0);
        public XmlNameSyntax NameNode => GetRed(ref nameNode, 1);
        public PunctuationSyntax GreaterThanToken => GetRed(ref slashGreaterThanToken, 2);

        public string Name => NameNode?.FullName;

        internal XmlElementEndTagSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlElementEndTag(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return lessThanToken;
                case 1: return nameNode;
                case 2: return slashGreaterThanToken;
                default: return null;
            }
        }

        internal override SyntaxNode GetNodeSlot(int index)
        {
            switch (index)
            {
                case 0: return LessThanSlashToken;
                case 1: return NameNode;
                case 2: return GreaterThanToken;
                default: return null;
            }
        }

        /*public override SyntaxNode WithLeadingTrivia(SyntaxNode trivia)
        {
            return new XmlElementEndTagSyntax(Kind,
                                              (PunctuationSyntax)LessThanSlashToken.WithLeadingTrivia(trivia),
                                              NameNode,
                                              GreaterThanToken);
        }

        public override SyntaxNode WithTrailingTrivia(SyntaxNode trivia)
        {
            return new XmlElementEndTagSyntax(Kind,
                                              LessThanSlashToken,
                                              NameNode,
                                              (PunctuationSyntax)GreaterThanToken.WithTrailingTrivia(trivia));
        }*/
    }
}
