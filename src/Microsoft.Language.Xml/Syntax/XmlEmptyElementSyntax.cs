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

        public string Name => NameNode?.FullName;

        public SyntaxList<SyntaxNode> Content => default(SyntaxList<SyntaxNode>);

        public IEnumerable<IXmlElementSyntax> Elements
        {
            get
            {
                return Enumerable.Empty<IXmlElementSyntax>();
            }
        }

        public XmlAttributeSyntax GetAttribute(string localName, string prefix = null) => AttributesNode.FirstOrDefault(
            attr => string.Equals(attr.NameNode.LocalName, localName, StringComparison.Ordinal) && string.Equals(attr.NameNode.Prefix, prefix, StringComparison.Ordinal)
        );

        public string GetAttributeValue(string localName, string prefix = null) => GetAttribute(localName, prefix)?.Value;

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
        IXmlElementSyntax IXmlElementSyntax.Parent => ParentElement;
        XmlNodeSyntax IXmlElementSyntax.AsNode => this;

        IXmlElementSyntax IXmlElementSyntax.WithName(XmlNameSyntax newName) => WithName(newName);

        IXmlElementSyntax IXmlElementSyntax.WithContent(SyntaxList<SyntaxNode> newContent) => WithContent(newContent);

        IXmlElementSyntax IXmlElementSyntax.WithAttributes(IEnumerable<XmlAttributeSyntax> newAttributes) => WithAttributes(new SyntaxList<XmlAttributeSyntax>(newAttributes));

        #endregion

        public XmlEmptyElementSyntax Update(PunctuationSyntax lessThanToken, XmlNameSyntax name, SyntaxList<XmlAttributeSyntax> attributes, PunctuationSyntax slashGreaterThanToken)
        {
            if (lessThanToken != this.LessThanToken || name != this.NameNode || attributes != this.AttributesNode || slashGreaterThanToken != this.SlashGreaterThanToken)
            {
                var newNode = SyntaxFactory.XmlEmptyElement(lessThanToken, name, attributes, slashGreaterThanToken);
                /*var annotations = this.GetAnnotations ();
				if (annotations != null && annotations.Length > 0)
					return newNode.WithAnnotations (annotations);*/
                return newNode;
            }

            return this;
        }

        public XmlEmptyElementSyntax WithLessThanToken(PunctuationSyntax lessThanToken)
        {
            return this.Update(lessThanToken, this.NameNode, this.AttributesNode, this.SlashGreaterThanToken);
        }

        public XmlEmptyElementSyntax WithName(XmlNameSyntax name)
        {
            return this.Update(this.LessThanToken, name, this.AttributesNode, this.SlashGreaterThanToken);
        }

        public XmlEmptyElementSyntax WithAttributes(SyntaxList<XmlAttributeSyntax> attributes)
        {
            return this.Update(this.LessThanToken, this.NameNode, attributes, this.SlashGreaterThanToken);
        }

        // This method has to convert to an XmlElementSyntax
        public XmlElementSyntax WithContent(SyntaxList<SyntaxNode> content)
        {
            var greaterThanToken = SyntaxFactory.Punctuation(SyntaxKind.GreaterThanToken, ">", null, null);
            var startTag = SyntaxFactory.XmlElementStartTag(this.LessThanToken, this.NameNode, this.AttributesNode, greaterThanToken);
            var lessThanSlashToken = SyntaxFactory.Punctuation(SyntaxKind.LessThanSlashToken, "</", null, null);
            var endTag = SyntaxFactory.XmlElementEndTag(lessThanToken, this.NameNode, greaterThanToken);

            return SyntaxFactory.XmlElement(startTag, content, endTag);
        }

        public XmlEmptyElementSyntax WithSlashGreaterThanToken(PunctuationSyntax slashGreaterThanToken)
        {
            return this.Update(this.LessThanToken, this.NameNode, this.AttributesNode, slashGreaterThanToken);
        }

        public XmlEmptyElementSyntax AddAttributes(params XmlAttributeSyntax[] items)
        {
            return this.WithAttributes(this.AttributesNode.AddRange(items));
        }
    }
}
