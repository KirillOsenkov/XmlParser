using System.Diagnostics;

namespace Microsoft.Language.Xml
{
    internal abstract partial class SyntaxVisitor
    {
        public virtual SyntaxNode Visit(SyntaxNode node)
        {
            if (node != null)
            {
                return node.Accept(this);
            }

            return null;
        }

        public virtual SyntaxNode VisitSyntaxNode(SyntaxNode node)
        {
            return node;
        }

        public virtual SyntaxNode VisitXmlNode(XmlNodeSyntax node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual SyntaxNode VisitXmlDocument(XmlDocumentSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlDeclaration(XmlDeclarationSyntax node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual SyntaxNode VisitXmlDeclarationOption(XmlDeclarationOptionSyntax node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual SyntaxNode VisitXmlElement(XmlElementSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlText(XmlTextSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlElementStartTag(XmlElementStartTagSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlElementEndTag(XmlElementEndTagSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlEmptyElement(XmlEmptyElementSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlAttribute(XmlAttributeSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlString(XmlStringSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlName(XmlNameSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlPrefix(XmlPrefixSyntax node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual SyntaxNode VisitXmlComment(XmlCommentSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual SyntaxNode VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax node)
        {
            return VisitXmlNode(node);
        }

        public virtual SyntaxNode VisitXmlCDataSection(XmlCDataSectionSyntax node)
        {
            return VisitXmlNode(node);
        }

        ////public virtual SyntaxToken Visit(SyntaxToken token)
        ////{
        ////    return token;
        ////}

        ////public virtual SyntaxNode Visit(SyntaxList list)
        ////{
        ////    return list;
        ////}

        public virtual SyntaxToken VisitSyntaxToken(SyntaxToken token)
        {
            return token;
        }

        public virtual SyntaxTrivia VisitSyntaxTrivia(SyntaxTrivia trivia)
        {
            return trivia;
        }
    }
}
