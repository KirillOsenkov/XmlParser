using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class XmlDeclarationSyntax : XmlNodeSyntax
    {
        internal new class Green : XmlNodeSyntax.Green
        {
            readonly PunctuationSyntax.Green? lessThanQuestionToken;
            readonly GreenNode? xmlKeyword;
            readonly XmlDeclarationOptionSyntax.Green? version;
            readonly XmlDeclarationOptionSyntax.Green? encoding;
            readonly XmlDeclarationOptionSyntax.Green? standalone;
            readonly PunctuationSyntax.Green? questionGreaterThanToken;

            internal PunctuationSyntax.Green? LessThanQuestionToken => lessThanQuestionToken;
            internal GreenNode? XmlKeyword => xmlKeyword;
            internal XmlDeclarationOptionSyntax.Green? Version => version;
            internal XmlDeclarationOptionSyntax.Green? Encoding => encoding;
            internal XmlDeclarationOptionSyntax.Green? Standalone => standalone;
            internal PunctuationSyntax.Green? QuestionGreaterThanToken => questionGreaterThanToken;

            internal Green(PunctuationSyntax.Green? lessThanQuestionToken,
                           GreenNode? xmlKeyword, XmlDeclarationOptionSyntax.Green? version,
                           XmlDeclarationOptionSyntax.Green? encoding,
                           XmlDeclarationOptionSyntax.Green? standalone,
                           PunctuationSyntax.Green? questionGreaterThanToken)
                : base(SyntaxKind.XmlDeclaration)
            {
                this.SlotCount = 6;
                this.lessThanQuestionToken = lessThanQuestionToken;
                AdjustWidth(lessThanQuestionToken);
                this.xmlKeyword = xmlKeyword;
                AdjustWidth(xmlKeyword);
                this.version = version;
                AdjustWidth(version);
                this.encoding = encoding;
                AdjustWidth(encoding);
                this.standalone = standalone;
                AdjustWidth(standalone);
                this.questionGreaterThanToken = questionGreaterThanToken;
                AdjustWidth(questionGreaterThanToken);
            }

            internal Green(PunctuationSyntax.Green? lessThanQuestionToken,
                           GreenNode? xmlKeyword, XmlDeclarationOptionSyntax.Green? version,
                           XmlDeclarationOptionSyntax.Green? encoding,
                           XmlDeclarationOptionSyntax.Green? standalone,
                           PunctuationSyntax.Green? questionGreaterThanToken,
                           DiagnosticInfo[]? diagnostics, SyntaxAnnotation[] annotations)
                : base(SyntaxKind.XmlDeclaration, diagnostics, annotations)
            {
                this.SlotCount = 6;
                this.lessThanQuestionToken = lessThanQuestionToken;
                AdjustWidth(lessThanQuestionToken);
                this.xmlKeyword = xmlKeyword;
                AdjustWidth(xmlKeyword);
                this.version = version;
                AdjustWidth(version);
                this.encoding = encoding;
                AdjustWidth(encoding);
                this.standalone = standalone;
                AdjustWidth(standalone);
                this.questionGreaterThanToken = questionGreaterThanToken;
                AdjustWidth(questionGreaterThanToken);
            }

            internal override SyntaxNode CreateRed(SyntaxNode? parent, int position) => new XmlDeclarationSyntax(this, parent, position);

            internal override GreenNode? GetSlot(int index)
            {
                switch (index)
                {
                    case 0: return lessThanQuestionToken;
                    case 1: return xmlKeyword;
                    case 2: return version;
                    case 3: return encoding;
                    case 4: return standalone;
                    case 5: return questionGreaterThanToken;
                }
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlDeclaration(this);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[]? diagnostics)
            {
                return new Green(lessThanQuestionToken, xmlKeyword, version, encoding, standalone, questionGreaterThanToken, diagnostics, GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new Green(lessThanQuestionToken, xmlKeyword, version, encoding, standalone, questionGreaterThanToken, GetDiagnostics(), annotations);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        PunctuationSyntax? lessThanQuestionToken;
        SyntaxToken? xmlKeyword;
        XmlDeclarationOptionSyntax? version;
        XmlDeclarationOptionSyntax? encoding;
        XmlDeclarationOptionSyntax? standalone;
        PunctuationSyntax? questionGreaterThanToken;

        public PunctuationSyntax? LessThanQuestionToken => GetRed(ref lessThanQuestionToken, 0);
        public SyntaxToken? XmlKeyword => GetRed(ref xmlKeyword, 1);
        public XmlDeclarationOptionSyntax? Version => GetRed(ref version, 2);
        public XmlDeclarationOptionSyntax? Encoding => GetRed(ref encoding, 3);
        public XmlDeclarationOptionSyntax? Standalone => GetRed(ref standalone, 4);
        public PunctuationSyntax? QuestionGreaterThanToken => GetRed(ref questionGreaterThanToken, 5);

        internal XmlDeclarationSyntax(Green green, SyntaxNode? parent, int position)
            : base(green, parent, position)
        {

        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlDeclaration(this);
        }

        internal override SyntaxNode? GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return lessThanQuestionToken;
                case 1: return xmlKeyword;
                case 2: return version;
                case 3: return encoding;
                case 4: return standalone;
                case 5: return questionGreaterThanToken;
                default: return null;
            }
        }

        internal override SyntaxNode? GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return LessThanQuestionToken;
                case 1: return XmlKeyword;
                case 2: return Version;
                case 3: return Encoding;
                case 4: return Standalone;
                case 5: return QuestionGreaterThanToken;
                default: return null;
            }
        }
    }
}
