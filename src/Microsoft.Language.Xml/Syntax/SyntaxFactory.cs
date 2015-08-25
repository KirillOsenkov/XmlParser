using System;
using System.Diagnostics;

namespace Microsoft.Language.Xml
{
    public class SyntaxFactory
    {
        public static readonly SyntaxToken EofToken = Token(null, SyntaxKind.EndOfFileToken, null, "");

        public static XmlDocumentSyntax XmlDocument(
            XmlDeclarationSyntax prologue,
            SyntaxList<SyntaxNode> precedingMisc,
            XmlNodeSyntax body,
            SyntaxList<XmlNodeSyntax> followingMisc,
            SyntaxToken eof)
        {
            return new XmlDocumentSyntax(SyntaxKind.XmlDocument, prologue, precedingMisc.Node, body, followingMisc.Node, eof);
        }

        public static XmlNameSyntax XmlName(XmlPrefixSyntax prefix, XmlNameTokenSyntax localName)
        {
            return new XmlNameSyntax(prefix, localName);
        }

        public static PunctuationSyntax MissingPunctuation(SyntaxKind kind, SyntaxNode leadingTrivia = null)
        {
            return new PunctuationSyntax(kind, "", leadingTrivia, null);
        }

        public static SyntaxToken MissingToken(SyntaxKind kind, SyntaxList<SyntaxNode> precedingTrivia = default(SyntaxList<SyntaxNode>))
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
                    return MissingPunctuation(kind, precedingTrivia.Node);
                case SyntaxKind.XmlNameToken:
                    return SyntaxFactory.XmlNameToken("", null, null);
                default:
                    break;
            }

            throw new InvalidOperationException();
        }

        public static SyntaxNode SkippedTokensTrivia(SyntaxList<SyntaxToken> syntaxList)
        {
            return new SkippedTokensTriviaSyntax(SyntaxKind.SkippedTokensTrivia, syntaxList.Node);
        }

        public static XmlNodeSyntax XmlElement(XmlElementStartTagSyntax startElement, SyntaxList<SyntaxNode> contentList, XmlElementEndTagSyntax endElement)
        {
            return new XmlElementSyntax(startElement, contentList.Node, endElement);
        }

        /*  <summary>
        ''' Represents the start tag of an XML element of the form &lt;element&gt;.
        ''' </summary> */
        public static XmlElementStartTagSyntax XmlElementStartTag(
            PunctuationSyntax lessThanToken,
            XmlNameSyntax name,
            SyntaxNode attributes,
            PunctuationSyntax greaterThanToken)
        {
            Debug.Assert(lessThanToken != null && lessThanToken.Kind == SyntaxKind.LessThanToken);
            Debug.Assert(name != null);
            Debug.Assert(greaterThanToken != null && greaterThanToken.Kind == SyntaxKind.GreaterThanToken);
            return new XmlElementStartTagSyntax(SyntaxKind.XmlElementStartTag, lessThanToken, name, attributes, greaterThanToken);
        }

        internal static XmlElementEndTagSyntax XmlElementEndTag(PunctuationSyntax lessThanSlashToken, XmlNameSyntax name, PunctuationSyntax greaterThanToken)
        {
            Debug.Assert(lessThanSlashToken != null && lessThanSlashToken.Kind == SyntaxKind.LessThanSlashToken);
            Debug.Assert(greaterThanToken != null && greaterThanToken.Kind == SyntaxKind.GreaterThanToken);
            var result = new XmlElementEndTagSyntax(SyntaxKind.XmlElementEndTag, lessThanSlashToken, name, greaterThanToken);

            return result;
        }

        /*  <summary>
        ''' Represents Xml text.
        ''' </summary>
        ''' <param name="textTokens">
        ''' A list of all the text tokens in the Xml text. This list always contains at
        ''' least one token.
        ''' </param>
        */
        public static XmlTextSyntax XmlText(SyntaxList<SyntaxToken> textTokens)
        {
            return new XmlTextSyntax(SyntaxKind.XmlText, ((SyntaxNode)textTokens.Node));
        }

        public static SyntaxToken Token(SyntaxNode leadingTrivia, SyntaxKind kind, SyntaxNode trailingTrivia, string text)
        {
            return new PunctuationSyntax(kind, text, leadingTrivia, trailingTrivia);
        }

        public static BadTokenSyntax BadToken(SyntaxSubKind subkind, string spelling, SyntaxList<SyntaxNode> precedingTrivia, SyntaxList<SyntaxNode> followingTrivia)
        {
            return new BadTokenSyntax(SyntaxKind.BadToken, spelling, precedingTrivia.Node, followingTrivia.Node);
        }

        public static XmlNameTokenSyntax XmlNameToken(string text, SyntaxNode precedingTrivia, SyntaxNode followingTrivia)
        {
            return new XmlNameTokenSyntax(text, precedingTrivia, followingTrivia);
        }

        /*  <summary>
        ''' Represents an empty XML element of the form &lt;element /&gt;
        ''' </summary>
        */
        internal static XmlEmptyElementSyntax XmlEmptyElement(
            PunctuationSyntax lessThanToken,
            XmlNameSyntax name,
            SyntaxList<SyntaxNode> attributes,
            PunctuationSyntax slashGreaterThanToken)
        {
            Debug.Assert(lessThanToken != null && lessThanToken.Kind == SyntaxKind.LessThanToken);
            Debug.Assert(name != null);
            Debug.Assert(slashGreaterThanToken != null && slashGreaterThanToken.Kind == SyntaxKind.SlashGreaterThanToken);
            return new XmlEmptyElementSyntax(lessThanToken, name, attributes.Node, slashGreaterThanToken);
        }

        /*  <summary>
        ''' Represents a string of XML characters embedded as the content of an XML
        ''' element.
        ''' </summary>
        */
        internal static XmlStringSyntax XmlString(PunctuationSyntax startQuoteToken, SyntaxList<XmlTextTokenSyntax> textTokens, PunctuationSyntax endQuoteToken)
        {
            //Debug.Assert(startQuoteToken != null && SyntaxFacts.IsXmlStringStartQuoteToken(startQuoteToken.Kind));
            //Debug.Assert(endQuoteToken != null && SyntaxFacts.IsXmlStringEndQuoteToken(endQuoteToken.Kind));

            var result = new XmlStringSyntax(SyntaxKind.XmlString, startQuoteToken, textTokens, endQuoteToken);

            return result;
        }

        /*  <summary>
        ''' Represents an XML document prologue option - version, encoding, standalone or
        ''' whitespace in an XML literal expression.
        ''' </summary>
        */
        internal static XmlDeclarationOptionSyntax XmlDeclarationOption(XmlNameTokenSyntax name, PunctuationSyntax equals, XmlStringSyntax value)
        {
            Debug.Assert(name != null && name.Kind == SyntaxKind.XmlNameToken);
            Debug.Assert(equals != null && equals.Kind == SyntaxKind.EqualsToken);
            Debug.Assert(value != null);
            //int hash;
            //var cached = SyntaxNodeCache.TryGetNode(SyntaxKind.XmlDeclarationOption, _factoryContext, name, equals, value, hash);
            //if (cached != null)
            //{
            //    return ((XmlDeclarationOptionSyntax)cached);
            //}

            var result = new XmlDeclarationOptionSyntax(SyntaxKind.XmlDeclarationOption, name, equals, value);
            //if (hash >= 0)
            //{
            //    SyntaxNodeCache.AddNode(result, hash);
            //}

            return result;
        }

        /*  <summary>
        ''' Represents the XML declaration prologue in an XML literal expression.
        ''' </summary>
        */
        internal static XmlDeclarationSyntax XmlDeclaration(PunctuationSyntax lessThanQuestionToken, SyntaxToken xmlKeyword, XmlDeclarationOptionSyntax version, XmlDeclarationOptionSyntax encoding, XmlDeclarationOptionSyntax standalone, PunctuationSyntax questionGreaterThanToken)
        {
            Debug.Assert(lessThanQuestionToken != null && lessThanQuestionToken.Kind == SyntaxKind.LessThanQuestionToken);
            //Debug.Assert(xmlKeyword != null && xmlKeyword.Kind == SyntaxKind.XmlKeyword);
            Debug.Assert(version != null);
            Debug.Assert(questionGreaterThanToken != null && questionGreaterThanToken.Kind == SyntaxKind.QuestionGreaterThanToken);
            return new XmlDeclarationSyntax(SyntaxKind.XmlDeclaration, lessThanQuestionToken, xmlKeyword, version, encoding, standalone, questionGreaterThanToken);
        }

        public static XmlNodeSyntax XmlAttribute(XmlNameSyntax name, PunctuationSyntax equals, XmlNodeSyntax value)
        {
            return new XmlAttributeSyntax(name, equals, value);
        }

        public static XmlPrefixSyntax XmlPrefix(XmlNameTokenSyntax localName, PunctuationSyntax colon)
        {
            return new XmlPrefixSyntax(localName, colon);
        }

        internal static XmlProcessingInstructionSyntax XmlProcessingInstruction(
            PunctuationSyntax beginProcessingInstruction,
            XmlNameTokenSyntax name,
            SyntaxList<SyntaxNode> toList,
            PunctuationSyntax endProcessingInstruction)
        {
            return new XmlProcessingInstructionSyntax(beginProcessingInstruction, name, toList, endProcessingInstruction);
        }

        internal static XmlTextTokenSyntax XmlTextLiteralToken(string text, string value, SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
        {
            return new XmlTextTokenSyntax(SyntaxKind.XmlTextLiteralToken, text, leadingTrivia, trailingTrivia, value);
        }

        /*  <summary>
          ''' Represents character data in Xml content also known as PCData or in an Xml
          ''' attribute value. All text is here for now even text that does not need
          ''' normalization such as comment, pi and cdata text.
          ''' </summary>
          ''' <param name="text">
          ''' The actual text of this token.
          ''' </param>
        */
        internal static XmlTextTokenSyntax XmlEntityLiteralToken(string text, string value, SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
        {
            Debug.Assert(text != null);
            return new XmlTextTokenSyntax(SyntaxKind.XmlEntityLiteralToken, text, leadingTrivia, trailingTrivia, value);
        }

        internal static SyntaxNode WhitespaceTrivia(string text)
        {
            Debug.Assert(text != null);
            return new SyntaxTrivia(SyntaxKind.WhitespaceTrivia, text);
        }

        internal static SyntaxTrivia EndOfLineTrivia(string text)
        {
            return new SyntaxTrivia(SyntaxKind.EndOfLineTrivia, text);
        }

        internal static XmlCDataSectionSyntax XmlCDataSection(
            PunctuationSyntax beginCData,
            SyntaxList<XmlTextTokenSyntax> result,
            PunctuationSyntax endCData)
        {
            return new XmlCDataSectionSyntax(SyntaxKind.XmlCDataSection, beginCData, result.Node, endCData);
        }

        internal static XmlNodeSyntax XmlComment(
            PunctuationSyntax beginComment,
            SyntaxList<XmlTextTokenSyntax> result,
            PunctuationSyntax endComment)
        {
            return new XmlCommentSyntax(SyntaxKind.XmlComment, beginComment, result.Node, endComment);
        }
    }
}
