using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlPrefixSyntax : SyntaxNode
    {
        internal class Green : GreenNode
        {
            readonly XmlNameTokenSyntax.Green name;
            readonly PunctuationSyntax.Green colonToken;

            internal XmlNameTokenSyntax.Green Name => name;

            internal Green(XmlNameTokenSyntax.Green name, PunctuationSyntax.Green colonToken)
                : base(SyntaxKind.XmlPrefix)
            {
                this.SlotCount = 2;
                this.name = name;
                AdjustWidth(name);
                this.colonToken = colonToken;
                AdjustWidth(colonToken);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlPrefixSyntax(this, parent, position);

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return name;
                    case 1: return colonToken;
                }

                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlPrefix(this);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        XmlNameTokenSyntax name;
        PunctuationSyntax colonToken;

        public XmlNameTokenSyntax Name => GetRed(ref name, 0);
        public PunctuationSyntax ColonToken => GetRed(ref colonToken, 1);

        internal XmlPrefixSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlPrefix(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return name;
                case 1: return colonToken;
                default: return null;
            }
        }

        internal override SyntaxNode GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return Name;
                case 1: return ColonToken;
                default: return null;
            }
        }

        public XmlPrefixSyntax Update(XmlNameTokenSyntax name, PunctuationSyntax colonToken)
        {
            if (name != this.Name || colonToken != this.ColonToken)
            {
                var newNode = SyntaxFactory.XmlPrefix(name, colonToken);
                return newNode;
            }

            return this;
        }

        public XmlPrefixSyntax WithName(XmlNameTokenSyntax name)
        {
            return Update(name, ColonToken);
        }

        public XmlPrefixSyntax WithColonToken(PunctuationSyntax colonToken)
        {
            return Update(Name, colonToken);
        }
    }
}
