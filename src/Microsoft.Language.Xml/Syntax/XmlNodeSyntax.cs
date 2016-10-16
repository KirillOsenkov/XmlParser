namespace Microsoft.Language.Xml
{
    public abstract class XmlNodeSyntax : SyntaxNode
    {
        public XmlNodeSyntax(SyntaxKind kind) : base(kind)
        {
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlNode(this);
        }
    }
}
