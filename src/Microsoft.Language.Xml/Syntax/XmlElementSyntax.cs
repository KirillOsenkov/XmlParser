using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlElementSyntax : XmlNodeSyntax, IXmlElement, IXmlElementSyntax, INamedXmlNode
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly XmlElementStartTagSyntax.Green startTag;
            readonly GreenNode content;
            readonly XmlElementEndTagSyntax.Green endTag;

            internal XmlElementStartTagSyntax.Green StartTag => startTag;
            internal GreenNode Content => content;
            internal XmlElementEndTagSyntax.Green EndTag => endTag;

            internal Green(XmlElementStartTagSyntax.Green startTag, GreenNode content, XmlElementEndTagSyntax.Green endTag)
                : base(SyntaxKind.XmlElement)
            {
                this.SlotCount = 3;
                this.startTag = startTag;
                AdjustWidth(startTag);
                this.content = content;
                AdjustWidth(content);
                this.endTag = endTag;
                AdjustWidth(endTag);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlElementSyntax(this, parent, position);

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return startTag;
                    case 1: return content;
                    case 2: return endTag;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlElement(this);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        XmlElementStartTagSyntax startTag;
        SyntaxNode content;
        XmlElementEndTagSyntax endTag;

        public XmlElementStartTagSyntax StartTag => GetRed(ref startTag, 0);
        public XmlElementEndTagSyntax EndTag => GetRed(ref endTag, 2);
        public SyntaxList<SyntaxNode> Content => new SyntaxList<SyntaxNode>(GetRed(ref content, 1));

        internal XmlElementSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlElement(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return startTag;
                case 1: return content;
                case 2: return endTag;
                default: return null;
            }
        }

        internal override SyntaxNode GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return StartTag;
                case 1: return GetRed(ref content, 1);
                case 2: return EndTag;
                default: return null;
            }
        }

        public XmlNameSyntax NameNode => StartTag?.NameNode;
        public string Name => StartTag?.Name;

        public string TextContent => string.Empty;

        public IEnumerable<IXmlElementSyntax> Elements
        {
            get
            {
                if (Content.Node is SyntaxList)
                {
                    return ((SyntaxList)Content.Node).ChildNodes.OfType<IXmlElementSyntax>();
                }
                else if (Content.Node is IXmlElementSyntax)
                {
                    return new IXmlElementSyntax[] { (IXmlElementSyntax)Content.Node };
                }

                return Enumerable.Empty<IXmlElementSyntax>();
            }
        }

        public XmlAttributeSyntax this[string attributeName] => StartTag.AttributesNode.FirstOrDefault(attr => string.Equals(attr.Name, attributeName, StringComparison.Ordinal));

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
                if (StartTag?.AttributesNode == null)
                {
                    yield break;
                }

                var singleAttribute = StartTag?.AttributesNode.Node as XmlAttributeSyntax;
                if (singleAttribute != null)
                {
                    yield return new KeyValuePair<string, string>(singleAttribute.Name, singleAttribute.Value);
                    yield break;
                }

                foreach (var attribute in StartTag?.AttributesNode.OfType<XmlAttributeSyntax>())
                {
                    yield return new KeyValuePair<string, string>(attribute.Name, attribute.Value);
                }
            }
        }

        IXmlElementSyntax IXmlElement.AsSyntaxElement => this;

        string IXmlElement.this[string attributeName] => GetAttributeValue(attributeName);
        #endregion

        #region IXmlElementSyntax

        IEnumerable<XmlAttributeSyntax> IXmlElementSyntax.Attributes => (IEnumerable<XmlAttributeSyntax>)StartTag?.AttributesNode;
        IXmlElementSyntax IXmlElementSyntax.Parent => ParentElement.AsSyntaxElement;

        #endregion

        /*public override SyntaxNode WithLeadingTrivia(SyntaxNode trivia)
        {
            return new XmlElementSyntax((XmlElementStartTagSyntax)StartTag.WithLeadingTrivia(trivia),
                                        Content,
                                        EndTag);
        }

        public override SyntaxNode WithTrailingTrivia(SyntaxNode trivia)
        {
            return new XmlElementSyntax(StartTag,
                                        Content,
                                        (XmlElementEndTagSyntax)EndTag.WithTrailingTrivia(trivia));
        }*/
    }
}
