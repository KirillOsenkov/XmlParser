using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlCDataSectionSyntax : XmlNodeSyntax
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly PunctuationSyntax.Green? beginCData;
            readonly GreenNode? value;
            readonly PunctuationSyntax.Green? endCData;

            internal PunctuationSyntax.Green? BeginCData => beginCData;
            internal InternalSyntax.SyntaxList<GreenNode> TextTokens => value;
            internal PunctuationSyntax.Green? EndCData => endCData;

            internal Green(PunctuationSyntax.Green? beginCData, GreenNode? value, PunctuationSyntax.Green? endCData)
                : base(SyntaxKind.XmlCDataSection)
            {
                this.SlotCount = 3;
                this.beginCData = beginCData;
                AdjustWidth(beginCData);
                this.value = value;
                AdjustWidth(value);
                this.endCData = endCData;
                AdjustWidth(endCData);
            }

            internal Green(PunctuationSyntax.Green? beginCData, GreenNode? value, PunctuationSyntax.Green? endCData, DiagnosticInfo[]? diagnostics, SyntaxAnnotation[] annotations)
                : base(SyntaxKind.XmlCDataSection, diagnostics, annotations)
            {
                this.SlotCount = 3;
                this.beginCData = beginCData;
                AdjustWidth(beginCData);
                this.value = value;
                AdjustWidth(value);
                this.endCData = endCData;
                AdjustWidth(endCData);
            }

            internal override SyntaxNode CreateRed(SyntaxNode? parent, int position) => new XmlCDataSectionSyntax(this, parent, position);

            internal override GreenNode? GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return beginCData;
                    case 1: return value;
                    case 2: return endCData;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlCDataSection(this);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[]? diagnostics)
            {
                return new Green(beginCData, value, endCData, diagnostics, GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new Green(beginCData, value, endCData, GetDiagnostics(), annotations);
            }
        }

        PunctuationSyntax? beginCData;
        SyntaxNode? textTokens;
        PunctuationSyntax? endCData;

        public PunctuationSyntax BeginCData => GetRed(ref beginCData, 0)!;
        public SyntaxList<SyntaxNode> TextTokens => new SyntaxList<SyntaxNode>(GetRed(ref textTokens, 1));
        public PunctuationSyntax EndCData => GetRed(ref endCData, 2)!;

        internal XmlCDataSectionSyntax(Green green, SyntaxNode? parent, int position)
            : base(green, parent, position)
        {

        }

        public string Value => TextTokens.Node?.ToFullString() ?? string.Empty;

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlCDataSection(this);
        }

        internal override SyntaxNode? GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return beginCData;
                case 1: return textTokens;
                case 2: return endCData;
                default: return null;
            }
        }

        internal override SyntaxNode? GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return BeginCData;
                case 1: return GetRed(ref textTokens, 1);
                case 2: return EndCData;
                default: return null;
            }
        }
    }
}
