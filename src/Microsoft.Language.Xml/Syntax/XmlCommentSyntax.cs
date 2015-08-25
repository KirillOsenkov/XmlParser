namespace Microsoft.Language.Xml
{
    public class XmlCommentSyntax : XmlNodeSyntax
    {
        public PunctuationSyntax BeginComment;
        public PunctuationSyntax EndComment;
        public SyntaxNode Content;

        public XmlCommentSyntax(SyntaxKind kind, PunctuationSyntax beginComment, SyntaxNode node, PunctuationSyntax endComment) : base(kind)
        {
            this.BeginComment = beginComment;
            this.Content = node;
            this.EndComment = endComment;
            SlotCount = 3;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return BeginComment;
                case 1:
                    return Content;
                case 2:
                    return EndComment;
                default:
                    throw null;
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlComment(this);
        }
    }
}
