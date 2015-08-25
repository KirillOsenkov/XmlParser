using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Language.Xml
{
    public class XmlProcessingInstructionSyntax : XmlNodeSyntax
    {
        public PunctuationSyntax LessThanQuestionToken { get; set; }
        public XmlNameTokenSyntax Name { get; set; }
        public SyntaxNode TextTokens { get; set; }
        public PunctuationSyntax QuestionGreaterThanToken { get; set; }

        public XmlProcessingInstructionSyntax(
            PunctuationSyntax beginProcessingInstruction,
            XmlNameTokenSyntax name,
            SyntaxList<SyntaxNode> toList,
            PunctuationSyntax endProcessingInstruction)
            : base(SyntaxKind.XmlProcessingInstruction)
        {
            LessThanQuestionToken = beginProcessingInstruction;
            Name = name;
            TextTokens = toList.Node;
            QuestionGreaterThanToken = endProcessingInstruction;
            SlotCount = 4;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return LessThanQuestionToken;
                case 1:
                    return Name;
                case 2:
                    return TextTokens;
                case 3:
                    return QuestionGreaterThanToken;
                default:
                    throw null;
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlProcessingInstruction(this);
        }
    }
}
