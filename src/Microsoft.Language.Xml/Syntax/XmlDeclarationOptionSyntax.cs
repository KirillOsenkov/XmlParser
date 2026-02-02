using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlDeclarationOptionSyntax : XmlNodeSyntax, INamedXmlNode
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly XmlNameTokenSyntax.Green? name;
            readonly PunctuationSyntax.Green? equals;
            readonly XmlStringSyntax.Green? value;

            internal XmlNameTokenSyntax.Green? Name => name;
            internal new PunctuationSyntax.Green? Equals => equals;
            internal XmlStringSyntax.Green? Value => value;

            internal Green(XmlNameTokenSyntax.Green? name, PunctuationSyntax.Green? equals, XmlStringSyntax.Green? value)
                : base(SyntaxKind.XmlDeclarationOption)
            {
                this.SlotCount = 3;
                this.name = name;
                AdjustWidth(name);
                this.equals = equals;
                AdjustWidth(equals);
                this.value = value;
                AdjustWidth(value);
            }

            internal Green(XmlNameTokenSyntax.Green? name, PunctuationSyntax.Green? equals, XmlStringSyntax.Green? value, DiagnosticInfo[]? diagnostics, SyntaxAnnotation[] annotations)
                : base(SyntaxKind.XmlDeclarationOption, diagnostics, annotations)
            {
                this.SlotCount = 3;
                this.name = name;
                AdjustWidth(name);
                this.equals = equals;
                AdjustWidth(equals);
                this.value = value;
                AdjustWidth(value);
            }

            internal override SyntaxNode CreateRed(SyntaxNode? parent, int position) => new XmlDeclarationOptionSyntax(this, parent, position);

            internal override GreenNode? GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return name;
                    case 1: return equals;
                    case 2: return value;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlDeclarationOption(this);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[]? diagnostics)
            {
                return new Green(name, equals, value, diagnostics, GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new Green(name, equals, value, GetDiagnostics(), annotations);
            }
        }

        internal new Green GreenNode => (Green)GreenNode;

        XmlNameTokenSyntax? nameNode;
        PunctuationSyntax? equals;
        XmlStringSyntax? value;

        public XmlNameTokenSyntax NameNode => GetRed(ref nameNode, 0)!;
        public new PunctuationSyntax Equals => GetRed(ref equals, 1)!;
        public XmlStringSyntax Value => GetRed(ref value, 2)!;

        public string Name => NameNode.Name;

        internal XmlDeclarationOptionSyntax(Green green, SyntaxNode? parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlDeclarationOption(this);
        }

        internal override SyntaxNode? GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return nameNode;
                case 1: return equals;
                case 2: return value;
                default: return null;
            }
        }

        internal override SyntaxNode? GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return NameNode;
                case 1: return Equals;
                case 2: return Value;
                default: return null;
            }
        }
    }
}
