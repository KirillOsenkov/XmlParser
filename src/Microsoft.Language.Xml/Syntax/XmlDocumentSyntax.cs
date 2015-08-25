namespace Microsoft.Language.Xml
{
    public class XmlDocumentSyntax : XmlNodeSyntax
    {
        public XmlNodeSyntax Body { get; private set; }
        public SyntaxNode PrecedingMisc { get; private set; }
        public SyntaxNode FollowingMisc { get; private set; }
        public XmlDeclarationSyntax Prologue { get; private set; }
        public SyntaxToken Eof { get; set; }

        public XmlDocumentSyntax(
            SyntaxKind kind,
            XmlDeclarationSyntax prologue,
            SyntaxNode precedingMisc,
            XmlNodeSyntax body,
            SyntaxNode followingMisc,
            SyntaxToken eof) : base(kind)
        {
            this.Prologue = prologue;
            this.PrecedingMisc = precedingMisc;
            this.Body = body;
            this.FollowingMisc = followingMisc;
            this.Eof = eof;
            SlotCount = 5;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return Prologue;
                case 1:
                    return PrecedingMisc;
                case 2:
                    return Body;
                case 3:
                    return FollowingMisc;
                case 4:
                    return Eof;
                default:
                    throw null;
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlDocument(this);
        }
    }
}
