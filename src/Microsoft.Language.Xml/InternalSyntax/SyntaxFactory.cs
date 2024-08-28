using System;

namespace Microsoft.Language.Xml.InternalSyntax
{
    internal static class SyntaxFactory
    {
        #region Nodes
        internal static XmlDocumentSyntax.Green XmlDocument(
            XmlDeclarationSyntax.Green? prologue,
            SyntaxList<GreenNode> precedingMisc,
            XmlNodeSyntax.Green body,
            SyntaxList<GreenNode> followingMisc,
            SkippedTokensTriviaSyntax.Green? skippedTokens,
            SyntaxToken.Green eof)
        {
            return new XmlDocumentSyntax.Green(prologue, precedingMisc.Node, body, followingMisc.Node, skippedTokens, eof);
        }

        internal static XmlDocumentSyntax.Green XmlDocument(
            XmlDeclarationSyntax.Green? prologue,
            GreenNode precedingMisc,
            XmlNodeSyntax.Green body,
            GreenNode followingMisc,
            SkippedTokensTriviaSyntax.Green skippedTokens,
            SyntaxToken.Green eof)
        {
            return new XmlDocumentSyntax.Green(prologue, precedingMisc, body, followingMisc, skippedTokens, eof);
        }

        internal static XmlNodeSyntax.Green XmlElement(XmlElementStartTagSyntax.Green? startElement, GreenNode? content, XmlElementEndTagSyntax.Green endElement)
        {
            return new XmlElementSyntax.Green(startElement, content, endElement);
        }

        internal static XmlNodeSyntax.Green XmlEmptyElement(PunctuationSyntax.Green lessThanToken, XmlNameSyntax.Green name, GreenNode? attributes, PunctuationSyntax.Green slashGreaterThanToken)
        {
            return new XmlEmptyElementSyntax.Green(lessThanToken, name, attributes, slashGreaterThanToken);
        }

        internal static XmlElementStartTagSyntax.Green XmlElementStartTag(
            PunctuationSyntax.Green lessThanToken,
            XmlNameSyntax.Green name,
            GreenNode? attributes,
            PunctuationSyntax.Green greaterThanToken)
        {
            return new XmlElementStartTagSyntax.Green(lessThanToken, name, attributes, greaterThanToken);
        }

        internal static XmlElementEndTagSyntax.Green XmlElementEndTag(PunctuationSyntax.Green lessThanSlashToken,
                                                                       XmlNameSyntax.Green name,
                                                                       PunctuationSyntax.Green greaterThanToken)
        {
            return new XmlElementEndTagSyntax.Green(lessThanSlashToken, name, greaterThanToken);
        }

        internal static XmlAttributeSyntax.Green XmlAttribute(XmlNameSyntax.Green name, PunctuationSyntax.Green equals, XmlStringSyntax.Green value)
        {
            return new XmlAttributeSyntax.Green(name, equals, value);
        }

        internal static XmlPrefixSyntax.Green XmlPrefix(XmlNameTokenSyntax.Green prefixName, PunctuationSyntax.Green colon)
        {
            int hash;
            var cached = SyntaxNodeCache.TryGetNode (SyntaxKind.XmlPrefix, prefixName, colon, out hash);
            if (cached != null) return (XmlPrefixSyntax.Green)cached;

            var result = new XmlPrefixSyntax.Green (prefixName, colon);
            if (hash >= 0)
                SyntaxNodeCache.AddNode (result, hash);
            return result;
        }

        internal static XmlNameSyntax.Green XmlName(XmlPrefixSyntax.Green? prefix, XmlNameTokenSyntax.Green localName)
        {
            int hash;
            var cached = SyntaxNodeCache.TryGetNode(SyntaxKind.XmlName, prefix, localName, out hash);
            if (cached != null) return (XmlNameSyntax.Green)cached;

            var result = new XmlNameSyntax.Green(prefix, localName);
            if (hash >= 0)
                SyntaxNodeCache.AddNode(result, hash);
            return result;
        }

        internal static XmlDeclarationOptionSyntax.Green XmlDeclarationOption(XmlNameTokenSyntax.Green? name, PunctuationSyntax.Green? equals, XmlStringSyntax.Green? value)
        {
            return new XmlDeclarationOptionSyntax.Green(name, equals, value);
        }

        internal static XmlDeclarationSyntax.Green XmlDeclaration(PunctuationSyntax.Green lessThanQuestionToken, SyntaxToken.Green? xmlKeyword, XmlDeclarationOptionSyntax.Green? version, XmlDeclarationOptionSyntax.Green? encoding, XmlDeclarationOptionSyntax.Green? standalone, PunctuationSyntax.Green? questionGreaterThanToken)
        {
            return new XmlDeclarationSyntax.Green(lessThanQuestionToken, xmlKeyword, version, encoding, standalone, questionGreaterThanToken);
        }

        internal static XmlTextSyntax.Green XmlText(InternalSyntax.SyntaxList<SyntaxToken.Green> textTokens)
        {
            return new XmlTextSyntax.Green(textTokens.Node);
        }

        internal static XmlStringSyntax.Green XmlString(PunctuationSyntax.Green startQuoteToken, GreenNode? textTokens, PunctuationSyntax.Green endQuoteToken)
        {
            return new XmlStringSyntax.Green(startQuoteToken, textTokens, endQuoteToken);
        }

        internal static XmlNodeSyntax.Green XmlComment(PunctuationSyntax.Green beginComment, GreenNode comment, PunctuationSyntax.Green endComment)
        {
            return new XmlCommentSyntax.Green(beginComment, comment, endComment);
        }

        internal static XmlCDataSectionSyntax.Green XmlCDataSection(PunctuationSyntax.Green beginCData, GreenNode result, PunctuationSyntax.Green endCData)
        {
            return new XmlCDataSectionSyntax.Green(beginCData, result, endCData);
        }

        internal static XmlProcessingInstructionSyntax.Green XmlProcessingInstruction(PunctuationSyntax.Green beginProcessingInstruction,
                                                                                       XmlNameTokenSyntax.Green name,
                                                                                       GreenNode toList,
                                                                                       PunctuationSyntax.Green endProcessingInstruction)
        {
            return new XmlProcessingInstructionSyntax.Green(beginProcessingInstruction, name, toList, endProcessingInstruction);
        }
        #endregion

        #region Tokens / Trivia
        internal static readonly SyntaxToken.Green Eof = new PunctuationSyntax.Green(SyntaxKind.EndOfFileToken, string.Empty, null, null);
        internal static readonly SyntaxTrivia.Green LfEndOfLine = new SyntaxTrivia.Green(SyntaxKind.EndOfLineTrivia, "\n");
        internal static readonly SyntaxTrivia.Green CrLfEndOfLine = new SyntaxTrivia.Green(SyntaxKind.EndOfLineTrivia, "\r\n");

        internal static SyntaxToken.Green EofToken(SyntaxList<GreenNode> precedingTrivia)
        {
            return new PunctuationSyntax.Green(SyntaxKind.EndOfFileToken, "", precedingTrivia.Node, null);
        }

        internal static SyntaxToken.Green EndOfXml(SyntaxList<GreenNode> precedingTrivia)
        {
            return new PunctuationSyntax.Green(SyntaxKind.EndOfXmlToken, string.Empty, precedingTrivia.Node, null);
        }

        internal static SyntaxTrivia.Green EndOfLine(string text)
        {
            if (text.Length == 1 && text[0] == '\n')
                return LfEndOfLine;
            return new SyntaxTrivia.Green(SyntaxKind.EndOfLineTrivia, text);
        }

        internal static XmlNameTokenSyntax.Green XmlNameToken(string text, GreenNode? precedingTrivia, GreenNode? followingTrivia)
        {
            return new XmlNameTokenSyntax.Green(text, precedingTrivia, followingTrivia);
        }

        internal static XmlEntityTokenSyntax.Green XmlEntityToken(string text, string value, SyntaxList<GreenNode> precedingTrivia, SyntaxList<GreenNode> followingTrivia)
        {
            return new XmlEntityTokenSyntax.Green(text, value, precedingTrivia.Node, followingTrivia.Node);
        }

        internal static XmlTextTokenSyntax.Green XmlTextToken(string text, SyntaxList<GreenNode> precedingTrivia, SyntaxList<GreenNode> followingTrivia)
        {
            return new XmlTextTokenSyntax.Green(text, precedingTrivia.Node, followingTrivia.Node);
        }

        internal static BadTokenSyntax.Green BadToken(SyntaxSubKind subkind, string spelling, SyntaxList<GreenNode> precedingTrivia, SyntaxList<GreenNode> followingTrivia)
        {
            return new BadTokenSyntax.Green(subkind, spelling, precedingTrivia.Node, followingTrivia.Node);
        }

        internal static PunctuationSyntax.Green Punctuation(
            SyntaxKind kind,
            string spelling,
            SyntaxList<GreenNode> precedingTrivia,
            SyntaxList<GreenNode> followingTrivia)
        {
            return new PunctuationSyntax.Green(kind, spelling, precedingTrivia.Node, followingTrivia.Node);
        }

        internal static SyntaxToken.Green MissingToken(SyntaxKind kind)
        {
            return MissingToken(null, kind);
        }

        internal static SyntaxToken.Green MissingToken(SyntaxList<GreenNode> precedingTrivia, SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.LessThanQuestionToken:
                case SyntaxKind.XmlKeyword:
                case SyntaxKind.LessThanToken:
                case SyntaxKind.LessThanGreaterThanToken:
                case SyntaxKind.LessThanSlashToken:
                case SyntaxKind.BeginCDataToken:
                case SyntaxKind.LessThanExclamationMinusMinusToken:
                case SyntaxKind.LessThanPercentEqualsToken:
                case SyntaxKind.SlashToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.EqualsToken:
                case SyntaxKind.SingleQuoteToken:
                case SyntaxKind.DoubleQuoteToken:
                case SyntaxKind.QuestionGreaterThanToken:
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.ColonToken:
                case SyntaxKind.SlashGreaterThanToken:
                case SyntaxKind.EndCDataToken:
                case SyntaxKind.MinusMinusGreaterThanToken:
                case SyntaxKind.XmlTextLiteralToken:
                    return new PunctuationSyntax.Green(kind, string.Empty, precedingTrivia.Node, null);
                case SyntaxKind.XmlNameToken:
                    return new XmlNameTokenSyntax.Green(string.Empty, null, null);
                default:
                    break;
            }

            throw new InvalidOperationException();
        }

        internal static PunctuationSyntax.Green MissingPunctuation(SyntaxKind kind)
        {
            return new PunctuationSyntax.Green(kind, string.Empty, null, null);
        }

        internal static KeywordSyntax.Green Keyword(string name, GreenNode leadingTrivia, GreenNode trailingTrivia)
        {
            return new KeywordSyntax.Green(name, leadingTrivia, trailingTrivia);
        }

        internal static SkippedTokensTriviaSyntax.Green SkippedTokensTrivia(SyntaxList<GreenNode> tokens)
        {
            return new SkippedTokensTriviaSyntax.Green(tokens.Node);
        }
        #endregion
    }
}
