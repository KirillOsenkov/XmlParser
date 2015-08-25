namespace Microsoft.Language.Xml
{
    public class XmlWhitespaceChecker
    {
        public XmlDeclarationSyntax Visit(XmlDeclarationSyntax node)
        {
            return node;
        }

        public XmlNodeSyntax Visit(XmlNodeSyntax node)
        {
            return node;
        }

        internal XmlElementEndTagSyntax Visit(XmlElementEndTagSyntax node)
        {
            return node;
        }
    }
}
