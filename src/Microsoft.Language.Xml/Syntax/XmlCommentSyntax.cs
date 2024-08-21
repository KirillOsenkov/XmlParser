using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlCommentSyntax : XmlNodeSyntax
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly PunctuationSyntax.Green? beginComment;
            readonly GreenNode? content;
            readonly PunctuationSyntax.Green? endComment;

            internal PunctuationSyntax.Green? BeginComment => beginComment;
            internal GreenNode? Content => content;
            internal PunctuationSyntax.Green? EndComment => endComment;

            internal Green(PunctuationSyntax.Green? beginComment, GreenNode? content, PunctuationSyntax.Green? endComment)
                : base(SyntaxKind.XmlComment)
            {
                this.SlotCount = 3;
                this.beginComment = beginComment;
                AdjustWidth(beginComment);
                this.content = content;
                AdjustWidth(content);
                this.endComment = endComment;
                AdjustWidth(endComment);
            }

            internal Green(PunctuationSyntax.Green? beginComment, GreenNode? content, PunctuationSyntax.Green? endComment, DiagnosticInfo[]? diagnostics, SyntaxAnnotation[] annotations)
                : base(SyntaxKind.XmlComment, diagnostics, annotations)
            {
                this.SlotCount = 3;
                this.beginComment = beginComment;
                AdjustWidth(beginComment);
                this.content = content;
                AdjustWidth(content);
                this.endComment = endComment;
                AdjustWidth(endComment);
            }

            internal override SyntaxNode CreateRed(SyntaxNode? parent, int position) => new XmlCommentSyntax(this, parent, position);

            internal override GreenNode? GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return beginComment;
                    case 1: return content;
                    case 2: return endComment;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlNode(this);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[]? diagnostics)
            {
                return new Green(beginComment, content, endComment, diagnostics, GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new Green(beginComment, content, endComment, GetDiagnostics(), annotations);
            }
        }

        PunctuationSyntax? beginComment;
        SyntaxNode? content;
        PunctuationSyntax? endComment;

        public PunctuationSyntax? BeginComment => GetRed(ref beginComment, 0);
        public SyntaxList<SyntaxNode> Content => new SyntaxList<SyntaxNode>(GetRed(ref content, 1));
        public PunctuationSyntax? EndComment => GetRed(ref endComment, 2);

        internal XmlCommentSyntax(Green green, SyntaxNode? parent, int position)
            : base(green, parent, position)
        {

        }

        public string Value => Content.Node?.ToFullString() ?? string.Empty;

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlComment(this);
        }

        internal override SyntaxNode? GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return beginComment;
                case 1: return content;
                case 2: return endComment;
                default: return null;
            }
        }

        internal override SyntaxNode? GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return BeginComment;
                case 1: return GetRed(ref content, 1);
                case 2: return EndComment;
                default: return null;
            }
        }
    }
}
