using System.Diagnostics;

namespace Microsoft.Language.Xml.InternalSyntax
{
    internal abstract partial class SyntaxVisitor
    {
        public virtual GreenNode Visit(GreenNode node)
        {
            if (node != null)
            {
                return node.Accept(this);
            }

            return null;
        }

        public virtual GreenNode VisitSyntaxNode(GreenNode node)
        {
            return node;
        }

        public virtual GreenNode VisitXmlNode(XmlNodeSyntax.Green node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual GreenNode VisitXmlDocument(XmlDocumentSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlDeclaration(XmlDeclarationSyntax.Green node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual GreenNode VisitXmlDeclarationOption(XmlDeclarationOptionSyntax.Green node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual GreenNode VisitXmlElement(XmlElementSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlText(XmlTextSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlElementStartTag(XmlElementStartTagSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlElementEndTag(XmlElementEndTagSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlEmptyElement(XmlEmptyElementSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlAttribute(XmlAttributeSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlString(XmlStringSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlName(XmlNameSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlPrefix(XmlPrefixSyntax.Green node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual GreenNode VisitXmlComment(XmlCommentSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax.Green node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual GreenNode VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        public virtual GreenNode VisitXmlCDataSection(XmlCDataSectionSyntax.Green node)
        {
            return VisitXmlNode(node);
        }

        ////public virtual SyntaxToken Visit(SyntaxToken token)
        ////{
        ////    return token;
        ////}

        ////public virtual GreenNode Visit(SyntaxList list)
        ////{
        ////    return list;
        ////}

        public virtual SyntaxToken.Green VisitSyntaxToken(SyntaxToken.Green token)
        {
            return token;
        }

        public virtual SyntaxTrivia.Green VisitSyntaxTrivia(SyntaxTrivia.Green trivia)
        {
            return trivia;
        }
    }
}
