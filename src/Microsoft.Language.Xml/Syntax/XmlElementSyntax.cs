using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlElementSyntax : XmlNodeSyntax, IXmlElement, IXmlElementSyntax, INamedXmlNode
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly XmlElementStartTagSyntax.Green? startTag;
            readonly GreenNode? content;
            readonly XmlElementEndTagSyntax.Green? endTag;

            internal XmlElementStartTagSyntax.Green? StartTag => startTag;
            internal GreenNode? Content => content;
            internal XmlElementEndTagSyntax.Green? EndTag => endTag;

            internal Green(XmlElementStartTagSyntax.Green? startTag, GreenNode? content, XmlElementEndTagSyntax.Green? endTag)
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

            internal Green(XmlElementStartTagSyntax.Green? startTag, GreenNode? content, XmlElementEndTagSyntax.Green? endTag, DiagnosticInfo[]? diagnostics, SyntaxAnnotation[] annotations)
                : base(SyntaxKind.XmlElement, diagnostics, annotations)
            {
                this.SlotCount = 3;
                this.startTag = startTag;
                AdjustWidth(startTag);
                this.content = content;
                AdjustWidth(content);
                this.endTag = endTag;
                AdjustWidth(endTag);
            }

            internal override SyntaxNode CreateRed(SyntaxNode? parent, int position) => new XmlElementSyntax(this, parent, position);

            internal override GreenNode? GetSlot(int index)
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

            internal override GreenNode SetDiagnostics(DiagnosticInfo[]? diagnostics)
            {
                return new Green(startTag, content, endTag, diagnostics, GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new Green(startTag, content, endTag, GetDiagnostics(), annotations);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        XmlElementStartTagSyntax? startTag;
        SyntaxNode? content;
        XmlElementEndTagSyntax? endTag;

        public XmlElementStartTagSyntax StartTag => GetRed(ref startTag, 0)!;
        public SyntaxList<SyntaxNode> Content => new SyntaxList<SyntaxNode>(GetRed(ref content, 1));
        public XmlElementEndTagSyntax EndTag => GetRed(ref endTag, 2)!;

        internal XmlElementSyntax(Green green, SyntaxNode? parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlElement(this);
        }

        internal override SyntaxNode? GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return startTag;
                case 1: return content;
                case 2: return endTag;
                default: return null;
            }
        }

        internal override SyntaxNode? GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return StartTag;
                case 1: return GetRed(ref content, 1);
                case 2: return EndTag;
                default: return null;
            }
        }

        public XmlNameSyntax NameNode => StartTag.NameNode;
        public string Name => StartTag.Name;

        public IEnumerable<IXmlElementSyntax> Elements
        {
            get
            {
                if (Content.Node is SyntaxList list)
                {
                    return list.ChildNodes.OfType<IXmlElementSyntax>();
                }
                else if (Content.Node is IXmlElementSyntax elementSyntax)
                {
                    return new IXmlElementSyntax[] { elementSyntax };
                }

                return SpecializedCollections.EmptyEnumerable<IXmlElementSyntax>();
            }
        }

        public XmlAttributeSyntax? GetAttribute(string localName, string? prefix = null) => StartTag?.AttributesNode.FirstOrDefault(
            attr => string.Equals(attr.NameNode?.LocalName, localName, StringComparison.Ordinal) && string.Equals(attr.NameNode?.Prefix, prefix, StringComparison.Ordinal)
        );

        public string? GetAttributeValue(string localName, string? prefix = null) => GetAttribute(localName, prefix)?.Value;

        public IXmlElement AsElement => this;
        public IXmlElementSyntax AsSyntaxElement => this;

        #region IXmlElement
        int IXmlElement.Start => Start;

        int IXmlElement.FullWidth => FullWidth;

        string? IXmlElement.Name => Name;

        string IXmlElement.Value => Content.ToFullString();

        IXmlElement? IXmlElement.Parent => Parent as IXmlElement;

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
                    yield return new KeyValuePair<string, string>(singleAttribute.Name ?? string.Empty, singleAttribute.Value ?? string.Empty);
                    yield break;
                }

                foreach (var attribute in StartTag!.AttributesNode.OfType<XmlAttributeSyntax>())
                {
                    yield return new KeyValuePair<string, string>(attribute.Name ?? string.Empty, attribute.Value ?? string.Empty);
                }
            }
        }

        IXmlElementSyntax IXmlElement.AsSyntaxElement => this;

        string? IXmlElement.this[string attributeName] => GetAttributeValue(attributeName);
        #endregion

        #region IXmlElementSyntax

        IEnumerable<XmlAttributeSyntax> IXmlElementSyntax.Attributes => (IEnumerable<XmlAttributeSyntax>)StartTag.AttributesNode;
        IXmlElementSyntax? IXmlElementSyntax.Parent => ParentElement;
        XmlNodeSyntax IXmlElementSyntax.AsNode => this;
        SyntaxList<XmlAttributeSyntax> IXmlElementSyntax.AttributesNode => StartTag.AttributesNode;

        IXmlElementSyntax IXmlElementSyntax.WithName(XmlNameSyntax newName) => WithStartTag(StartTag.WithName(newName));

        IXmlElementSyntax IXmlElementSyntax.WithContent(SyntaxList<SyntaxNode> newContent) => Update(StartTag, newContent, EndTag);

        IXmlElementSyntax IXmlElementSyntax.WithAttributes(IEnumerable<XmlAttributeSyntax> newAttributes) => WithStartTag(StartTag.WithAttributes(new SyntaxList<XmlAttributeSyntax>(newAttributes)));
        IXmlElementSyntax IXmlElementSyntax.WithAttributes(SyntaxList<XmlAttributeSyntax> newAttributes) => WithStartTag(StartTag.WithAttributes(newAttributes));

        #endregion

        public XmlElementSyntax Update(XmlElementStartTagSyntax startTag, SyntaxList<SyntaxNode> content, XmlElementEndTagSyntax endTag)
        {
            if (startTag != this.StartTag || content != this.Content || endTag != this.EndTag)
            {
                var newNode = SyntaxFactory.XmlElement(startTag, content, endTag);
                var annotations = this.GetAnnotations();
                if (annotations != null && annotations.Length > 0)
                    return newNode.WithAnnotations(annotations);
                return newNode;
            }

            return this;
        }

        public XmlElementSyntax WithStartTag(XmlElementStartTagSyntax startTag)
        {
            return this.Update(startTag, this.Content, this.EndTag);
        }

        public XmlElementSyntax WithContent(SyntaxList<SyntaxNode> content)
        {
            return this.Update(this.StartTag, content, this.EndTag);
        }

        public XmlElementSyntax WithEndTag(XmlElementEndTagSyntax endTag)
        {
            return this.Update(this.StartTag, this.Content, endTag);
        }

        public XmlElementSyntax AddStartTagAttributes(params XmlAttributeSyntax[] items)
        {
            Debug.Assert(StartTag != null);
            return this.WithStartTag(this.StartTag.WithAttributes(this.StartTag.AttributesNode.AddRange(items)));
        }

        public XmlElementSyntax AddContent(params XmlNodeSyntax[] items)
        {
            return this.WithContent(this.Content.AddRange(items));
        }

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
