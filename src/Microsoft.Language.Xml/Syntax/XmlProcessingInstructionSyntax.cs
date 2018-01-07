using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlProcessingInstructionSyntax : XmlNodeSyntax
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly PunctuationSyntax.Green lessThanQuestionToken;
            readonly XmlNameTokenSyntax.Green name;
            readonly GreenNode textTokens;
            readonly PunctuationSyntax.Green questionGreaterThanToken;

            internal PunctuationSyntax.Green LessThanQuestionToken => lessThanQuestionToken;
            internal XmlNameTokenSyntax.Green Name => name;
            internal InternalSyntax.SyntaxList<GreenNode> TextTokens => textTokens;
            internal PunctuationSyntax.Green QuestionGreaterThanToken => questionGreaterThanToken;

            internal Green(PunctuationSyntax.Green lessThanQuestionToken,
                            XmlNameTokenSyntax.Green name,
                            GreenNode textTokens,
                            PunctuationSyntax.Green questionGreaterThanToken)
                : base(SyntaxKind.XmlProcessingInstruction)
            {
                this.SlotCount = 4;
                this.lessThanQuestionToken = lessThanQuestionToken;
                AdjustWidth(lessThanQuestionToken);
                this.name = name;
                AdjustWidth(name);
                this.textTokens = textTokens;
                AdjustWidth(textTokens);
                this.questionGreaterThanToken = questionGreaterThanToken;
                AdjustWidth(questionGreaterThanToken);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new XmlProcessingInstructionSyntax(this, parent, position);

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return lessThanQuestionToken;
                    case 1: return name;
                    case 2: return textTokens;
                    case 3: return questionGreaterThanToken;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlProcessingInstruction(this);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        PunctuationSyntax lessThanQuestionToken;
        XmlNameTokenSyntax name;
        SyntaxNode textTokens;
        PunctuationSyntax questionGreaterThanToken;

        public PunctuationSyntax LessThanQuestionToken => GetRed(ref lessThanQuestionToken, 0);
        public XmlNameTokenSyntax Name => GetRed(ref name, 1);
        public SyntaxList<SyntaxNode> TextTokens => new SyntaxList<SyntaxNode>(GetRed(ref textTokens, 2));
        public PunctuationSyntax QuestionGreaterThanToken => GetRed(ref questionGreaterThanToken, 3);

        internal XmlProcessingInstructionSyntax(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlProcessingInstruction(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return lessThanQuestionToken;
                case 1: return name;
                case 2: return textTokens;
                case 3: return questionGreaterThanToken;
                default: return null;
            }
        }

        internal override SyntaxNode GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return LessThanQuestionToken;
                case 1: return Name;
                case 2: return GetRed(ref textTokens, 2);
                case 3: return QuestionGreaterThanToken;
                default: return null;
            }
        }
    }
}
