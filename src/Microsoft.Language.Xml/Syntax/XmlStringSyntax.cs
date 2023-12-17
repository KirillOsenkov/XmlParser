using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlStringSyntax : XmlNodeSyntax
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly PunctuationSyntax.Green startQuoteToken;
            readonly GreenNode value;
            readonly PunctuationSyntax.Green endQuoteToken;

            internal PunctuationSyntax.Green StartQuoteToken => startQuoteToken;
            internal GreenNode ValueNode => value;
            internal PunctuationSyntax.Green EndQuoteToken => endQuoteToken;

            internal InternalSyntax.SyntaxList<GreenNode> TextTokens => new InternalSyntax.SyntaxList<GreenNode>(value);

            internal Green(PunctuationSyntax.Green startQuoteToken, GreenNode value, PunctuationSyntax.Green endQuoteToken)
                : base(SyntaxKind.XmlString)
            {
                this.SlotCount = 3;
                this.startQuoteToken = startQuoteToken;
                AdjustWidth(startQuoteToken);
                this.value = value;
                AdjustWidth(value);
                this.endQuoteToken = endQuoteToken;
                AdjustWidth(endQuoteToken);
            }

            internal Green(PunctuationSyntax.Green startQuoteToken, GreenNode value, PunctuationSyntax.Green endQuoteToken, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(SyntaxKind.XmlString, diagnostics, annotations)
            {
                this.SlotCount = 3;
                this.startQuoteToken = startQuoteToken;
                AdjustWidth(startQuoteToken);
                this.value = value;
                AdjustWidth(value);
                this.endQuoteToken = endQuoteToken;
                AdjustWidth(endQuoteToken);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlStringSyntax(this, parent, position);

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return startQuoteToken;
                    case 1: return value;
                    case 2: return endQuoteToken;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlString(this);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new Green(startQuoteToken, value, endQuoteToken, diagnostics, GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new Green(startQuoteToken, value, endQuoteToken, GetDiagnostics(), annotations);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        PunctuationSyntax startQuoteToken;
        SyntaxNode textTokens;
        PunctuationSyntax endQuoteToken;

        public PunctuationSyntax StartQuoteToken => GetRed(ref startQuoteToken, 0);
        public SyntaxList<SyntaxNode> TextTokens => new SyntaxList<SyntaxNode>(GetRed(ref textTokens, 1));
        public PunctuationSyntax EndQuoteToken => GetRed(ref endQuoteToken, 2);

        internal XmlStringSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlString(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return startQuoteToken;
                case 1: return textTokens;
                case 2: return endQuoteToken;
                default: return null;
            }
        }

        internal override SyntaxNode GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return StartQuoteToken;
                case 1: return GetRed(ref textTokens, 1);
                case 2: return EndQuoteToken;
                default: return null;
            }
        }

        public XmlStringSyntax Update(PunctuationSyntax startQuoteToken, SyntaxList<SyntaxNode> textTokens, PunctuationSyntax endQuoteToken)
        {
            if (startQuoteToken != this.StartQuoteToken || textTokens != this.TextTokens || endQuoteToken != this.EndQuoteToken)
            {
                var newNode = SyntaxFactory.XmlString(startQuoteToken, textTokens, endQuoteToken);
                var annotations = this.GetAnnotations();
                if (annotations != null && annotations.Length > 0)
                    return newNode.WithAnnotations(annotations);
                return newNode;
            }

            return this;
        }

        public XmlStringSyntax WithStartQuoteToken(PunctuationSyntax startQuoteToken)
        {
            return Update(startQuoteToken, this.TextTokens, this.EndQuoteToken);
        }

        public XmlStringSyntax WithEndQuoteToken(PunctuationSyntax endQuoteToken)
        {
            return Update(this.StartQuoteToken, this.TextTokens, endQuoteToken);
        }

        public XmlStringSyntax WithTextTokens(SyntaxList<SyntaxNode> textTokens)
        {
            return Update(this.StartQuoteToken, textTokens, this.EndQuoteToken);
        }

        public XmlStringSyntax AddTextTokens(params SyntaxToken[] items)
        {
            return this.WithTextTokens(this.TextTokens.AddRange(items));
        }
    }
}
