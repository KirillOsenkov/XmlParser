using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Language.Xml
{
    public class XmlDeclarationSyntax : XmlNodeSyntax
    {
        public PunctuationSyntax LessThanQuestionToken { get; set; }
        public SyntaxToken XmlKeyword { get; set; }
        public XmlDeclarationOptionSyntax Version { get; set; }
        public XmlDeclarationOptionSyntax Encoding { get; set; }
        public XmlDeclarationOptionSyntax Standalone { get; set; }
        public PunctuationSyntax QuestionGreaterThanToken { get; set; }

        public XmlDeclarationSyntax(SyntaxKind kind,
            PunctuationSyntax lessThanQuestionToken,
            SyntaxToken xmlKeyword,
            XmlDeclarationOptionSyntax version,
            XmlDeclarationOptionSyntax encoding,
            XmlDeclarationOptionSyntax standalone,
            PunctuationSyntax questionGreaterThanToken)
            : base(kind)
        {
            this.SlotCount = 6;
            this.LessThanQuestionToken = lessThanQuestionToken;
            this.XmlKeyword = xmlKeyword;
            this.Version = version;
            this.Encoding = encoding;
            this.Standalone = standalone;
            this.QuestionGreaterThanToken = questionGreaterThanToken;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0: return LessThanQuestionToken;
                case 1: return XmlKeyword;
                case 2: return Version;
                case 3: return Encoding;
                case 4: return Standalone;
                case 5: return QuestionGreaterThanToken;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlDeclaration(this);
        }
    }
}
