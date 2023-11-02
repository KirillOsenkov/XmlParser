using System;

namespace Microsoft.Language.Xml
{
    using System.IO;
    using System.Text;
    using InternalSyntax;

    public class XmlAttributeSyntax : XmlNodeSyntax, INamedXmlNode
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly XmlNameSyntax.Green nameNode;
            readonly SyntaxToken.Green equalsSyntax;
            readonly XmlNodeSyntax.Green valueNode;

            internal XmlNameSyntax.Green NameNode => nameNode;
            internal new SyntaxToken.Green Equals => equalsSyntax;
            internal XmlNodeSyntax.Green ValueNode => valueNode;

            internal Green(XmlNameSyntax.Green nameNode, SyntaxToken.Green equalsSyntax, XmlNodeSyntax.Green valueNode)
                : base(SyntaxKind.XmlAttribute)
            {
                this.SlotCount = 3;
                this.nameNode = nameNode;
                AdjustWidth(nameNode);
                this.equalsSyntax = equalsSyntax;
                AdjustWidth(equalsSyntax);
                this.valueNode = valueNode;
                AdjustWidth(valueNode);
            }

            internal Green(XmlNameSyntax.Green nameNode, SyntaxToken.Green equalsSyntax, XmlNodeSyntax.Green valueNode, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(SyntaxKind.XmlAttribute, diagnostics, annotations)
            {
                this.SlotCount = 3;
                this.nameNode = nameNode;
                AdjustWidth(nameNode);
                this.equalsSyntax = equalsSyntax;
                AdjustWidth(equalsSyntax);
                this.valueNode = valueNode;
                AdjustWidth(valueNode);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlAttributeSyntax(this, parent, position);

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return nameNode;
                    case 1: return equalsSyntax;
                    case 2: return valueNode;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlAttribute(this);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new Green(nameNode, equalsSyntax, valueNode, diagnostics, GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new Green(nameNode, equalsSyntax, valueNode, GetDiagnostics(), annotations);
            }
        }

        XmlNameSyntax nameNode;
        PunctuationSyntax equalsSyntax;
        XmlStringSyntax valueNode;

        public XmlNameSyntax NameNode => GetRed(ref nameNode, 0);
        public new PunctuationSyntax Equals => GetRed(ref equalsSyntax, 1);
        public XmlStringSyntax ValueNode => GetRed(ref valueNode, 2);

        internal XmlAttributeSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public string Name => NameNode?.FullName;

        public bool IsNamespaceDeclaration => string.Equals(NameNode?.Prefix, "xmlns", StringComparison.Ordinal);

        public string Value
        {
            get
            {
                if (ValueNode == null)
                {
                    return null;
                }

                var xmlString = ValueNode;
                if (xmlString == null)
                {
                    return null;
                }

                if (xmlString.TextTokens.Node == null)
                {
                    return "";
                }

                return xmlString.TextTokens.Node.ToFullString();
            }
        }

        /// <summary>
        /// Get Normalized <see cref="Value"/> of <see cref="XmlStringSyntax"/>
        /// </summary>
        /// <remarks>
        /// Spec <seealso href="https://learn.microsoft.com/en-us/openspecs/ie_standards/ms-xml/389b8ef1-e19e-40ac-80de-eec2cd0c58ae">2.2.12 [XML] Section 3.3.3</seealso/>
        /// </remarks>
        public string NormalizedValue => Value.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlAttribute(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return nameNode;
                case 1: return equalsSyntax;
                case 2: return valueNode;
                default: return null;
            }
        }

        internal override SyntaxNode GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return NameNode;
                case 1: return Equals;
                case 2: return ValueNode;
                default: return null;
            }
        }

        public XmlAttributeSyntax Update(XmlNameSyntax name, PunctuationSyntax equalsToken, XmlStringSyntax value)
        {
            if (name != this.NameNode || equalsToken != this.Equals || value != this.ValueNode)
            {
                var newNode = SyntaxFactory.XmlAttribute(name, equalsToken, value);
                var annotations = this.GetAnnotations();
                if (annotations != null && annotations.Length > 0)
                    return newNode.WithAnnotations(annotations);
                return newNode;
            }

            return this;
        }

        public XmlAttributeSyntax WithName(XmlNameSyntax name)
        {
            return this.Update(name, this.Equals, this.ValueNode);
        }

        public XmlAttributeSyntax WithEqualsToken(PunctuationSyntax equalsToken)
        {
            return this.Update(this.NameNode, equalsToken, this.ValueNode);
        }

        public XmlAttributeSyntax WithValue(XmlStringSyntax value)
        {
            return this.Update(this.NameNode, this.Equals, value);
        }
    }
}
