using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlEmptyElementSyntax : XmlNodeSyntax, IXmlElement, IXmlElementSyntax, INamedXmlNode
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly PunctuationSyntax.Green lessThanToken;
            readonly XmlNameSyntax.Green name;
            readonly GreenNode attributes;
            readonly PunctuationSyntax.Green slashGreaterThanToken;

            internal PunctuationSyntax.Green LessThanToken => lessThanToken;
            internal XmlNameSyntax.Green NameNode => name;
            internal GreenNode AttributesNode => attributes;
            internal PunctuationSyntax.Green SlashGreaterThanToken => slashGreaterThanToken;

            internal Green(PunctuationSyntax.Green lessThanToken, XmlNameSyntax.Green name, GreenNode attributes, PunctuationSyntax.Green slashGreaterThanToken)
                : base(SyntaxKind.XmlEmptyElement)
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

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlEmptyElementSyntax(this, parent, position);

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
                return visitor.VisitXmlEmptyElement(this);
            }
        }

        PunctuationSyntax lessThanToken;
        XmlNameSyntax nameNode;
        SyntaxNode attributesNode;
        PunctuationSyntax slashGreaterThanToken;

        public PunctuationSyntax LessThanToken => GetRed(ref lessThanToken, 0);
        public XmlNameSyntax NameNode => GetRed(ref nameNode, 1);
        public SyntaxList<XmlAttributeSyntax> AttributesNode => new SyntaxList<XmlAttributeSyntax>(GetRed(ref attributesNode, 2));
        public PunctuationSyntax SlashGreaterThanToken => GetRed(ref slashGreaterThanToken, 3);

        internal XmlEmptyElementSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlEmptyElement(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return lessThanToken;
                case 1: return nameNode;
                case 2: return attributesNode;
                case 3: return slashGreaterThanToken;
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
                case 3: return SlashGreaterThanToken;
                default: return null;
            }
        }

        public string Name => NameNode?.Name;

        public SyntaxList<SyntaxNode> Content => default(SyntaxList<SyntaxNode>);

        public string TextContent => string.Empty;

        public IEnumerable<IXmlElementSyntax> Elements
        {
            get
            {
                return Enumerable.Empty<IXmlElementSyntax>();
            }
        }

        public XmlAttributeSyntax this[string attributeName] => AttributesNode.FirstOrDefault(attr => string.Equals(attr.Name, attributeName, StringComparison.Ordinal));

        public string GetAttributeValue(string attributeName) => this[attributeName]?.Value;

        public IXmlElement AsElement => this;
        public IXmlElementSyntax AsSyntaxElement => this;

        #region IXmlElement
        int IXmlElement.Start => Start;

        int IXmlElement.FullWidth => FullWidth;

        string IXmlElement.Name => Name;

        string IXmlElement.Value => Content.ToFullString();

        IXmlElement IXmlElement.Parent => Parent as IXmlElement;

        IEnumerable<IXmlElement> IXmlElement.Elements => Elements.Select(el => el.AsElement);

        IEnumerable<KeyValuePair<string, string>> IXmlElement.Attributes
        {
            get
            {
                if (AttributesNode == null)
                {
                    yield break;
                }

                var singleAttribute = AttributesNode.Node as XmlAttributeSyntax;
                if (singleAttribute != null)
                {
                    yield return new KeyValuePair<string, string>(singleAttribute.Name, singleAttribute.Value);
                    yield break;
                }

                foreach (var attribute in AttributesNode.OfType<XmlAttributeSyntax>())
                {
                    yield return new KeyValuePair<string, string>(attribute.Name, attribute.Value);
                }
            }
        }

        IXmlElementSyntax IXmlElement.AsSyntaxElement => this;

        string IXmlElement.this[string attributeName] => GetAttributeValue(attributeName);
        #endregion

        #region IXmlElementSyntax

        IEnumerable<XmlAttributeSyntax> IXmlElementSyntax.Attributes => (IEnumerable<XmlAttributeSyntax>)AttributesNode;
        IXmlElementSyntax IXmlElementSyntax.Parent => ParentElement.AsSyntaxElement;

        #endregion
    }
}
