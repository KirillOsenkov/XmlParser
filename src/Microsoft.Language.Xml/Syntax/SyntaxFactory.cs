using System;
using System.Collections.Generic;
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
            return XmlDocument(prologue, precedingMisc.Node, body, followingMisc.Node, eof);
        }

        public static XmlDocumentSyntax XmlDocument(
            XmlDeclarationSyntax prologue,
            SyntaxNode precedingMisc,
            XmlNodeSyntax body,
            SyntaxNode followingMisc,
            SyntaxToken eof)
        {
            return (XmlDocumentSyntax)new XmlDocumentSyntax.Green(prologue.GreenNode,
                                                                   precedingMisc.GreenNode,
                                                                   body.GreenNode,
                                                                   followingMisc.GreenNode,
                                                                   eof.GreenNode).CreateRed();
        }

        public static XmlNameSyntax XmlName(XmlPrefixSyntax prefix, XmlNameTokenSyntax localName)
        {
            return (XmlNameSyntax)new XmlNameSyntax.Green(prefix.GreenNode, localName.GreenNode).CreateRed();
        }

        public static PunctuationSyntax Punctuation(SyntaxKind kind, string spelling, SyntaxList<SyntaxNode> precedingTrivia, SyntaxList<SyntaxNode> followingTrivia)
        {
            return Punctuation(kind, spelling, precedingTrivia.Node, followingTrivia.Node);
        }

        public static PunctuationSyntax Punctuation(SyntaxKind kind, string spelling, SyntaxNode precedingTrivia, SyntaxNode followingTrivia)
        {
            return (PunctuationSyntax)new PunctuationSyntax.Green(kind, spelling, precedingTrivia?.GreenNode, followingTrivia?.GreenNode).CreateRed();
        }

        public static PunctuationSyntax MissingPunctuation(SyntaxKind kind, SyntaxNode leadingTrivia = null)
        {
            return (PunctuationSyntax)new PunctuationSyntax.Green(kind, string.Empty, leadingTrivia?.GreenNode, null).CreateRed();
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
                    return XmlNameToken("", null, null);
                default:
                    break;
            }

            throw new InvalidOperationException();
        }

        public static SyntaxNode SkippedTokensTrivia(SyntaxList<SyntaxToken> syntaxList)
        {
            return SkippedTokensTrivia(syntaxList.Node);
        }

        public static SyntaxNode SkippedTokensTrivia(SyntaxNode syntaxList)
        {
            return (SkippedTokensTriviaSyntax)new SkippedTokensTriviaSyntax.Green(syntaxList.GreenNode).CreateRed();
        }

        public static XmlNodeSyntax XmlElement(XmlElementStartTagSyntax startElement, SyntaxList<SyntaxNode> contentList, XmlElementEndTagSyntax endElement)
        {
            return XmlElement(startElement, contentList.Node, endElement);
        }

        public static XmlNodeSyntax XmlElement(XmlElementStartTagSyntax startElement, SyntaxNode content, XmlElementEndTagSyntax endElement)
        {
            return (XmlElementSyntax)new XmlElementSyntax.Green(startElement.GreenNode, content.GreenNode, endElement.GreenNode).CreateRed();
        }

        /*  <summary>
        ''' Represents the start tag of an XML element of the form &lt;element&gt;.
        ''' </summary> */
        public static XmlElementStartTagSyntax XmlElementStartTag(
            PunctuationSyntax lessThanToken,
            XmlNameSyntax name,
            SyntaxList<XmlAttributeSyntax> attributes,
            PunctuationSyntax greaterThanToken)
        {
            return XmlElementStartTag(lessThanToken, name, attributes.Node, greaterThanToken);
        }

        public static XmlElementStartTagSyntax XmlElementStartTag(
            PunctuationSyntax lessThanToken,
            XmlNameSyntax name,
            SyntaxNode attributes,
            PunctuationSyntax greaterThanToken)
        {
            Debug.Assert(lessThanToken != null && lessThanToken.Kind == SyntaxKind.LessThanToken);
            Debug.Assert(name != null);
            Debug.Assert(greaterThanToken != null && greaterThanToken.Kind == SyntaxKind.GreaterThanToken);
            return (XmlElementStartTagSyntax)new XmlElementStartTagSyntax.Green(lessThanToken.GreenNode, name.GreenNode, attributes.GreenNode, greaterThanToken.GreenNode).CreateRed();
        }

        public static XmlElementEndTagSyntax XmlElementEndTag(PunctuationSyntax lessThanSlashToken, XmlNameSyntax name, PunctuationSyntax greaterThanToken)
        {
            Debug.Assert(lessThanSlashToken != null && lessThanSlashToken.Kind == SyntaxKind.LessThanSlashToken);
            Debug.Assert(greaterThanToken != null && greaterThanToken.Kind == SyntaxKind.GreaterThanToken);
            return (XmlElementEndTagSyntax)new XmlElementEndTagSyntax.Green(lessThanSlashToken.GreenNode, name.GreenNode, greaterThanToken.GreenNode).CreateRed();
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
            return XmlText(textTokens.Node);
        }

        public static XmlTextSyntax XmlText(SyntaxNode textTokens)
        {
            return (XmlTextSyntax)new XmlTextSyntax.Green(textTokens.GreenNode).CreateRed();
        }

        public static SyntaxToken Token(SyntaxNode leadingTrivia, SyntaxKind kind, SyntaxNode trailingTrivia, string text)
        {
            return (PunctuationSyntax)new PunctuationSyntax.Green(kind, text, leadingTrivia?.GreenNode, trailingTrivia?.GreenNode).CreateRed();
        }

        public static BadTokenSyntax BadToken(SyntaxSubKind subkind, string spelling, SyntaxList<SyntaxNode> precedingTrivia, SyntaxList<SyntaxNode> followingTrivia)
        {
            return BadToken(subkind, spelling, precedingTrivia.Node, followingTrivia.Node);
        }

        public static BadTokenSyntax BadToken(SyntaxSubKind subkind, string spelling, SyntaxNode precedingTrivia, SyntaxNode followingTrivia)
        {
            return (BadTokenSyntax)new BadTokenSyntax.Green(subkind, spelling, precedingTrivia?.GreenNode, followingTrivia?.GreenNode).CreateRed();
        }

        public static XmlNameTokenSyntax XmlNameToken(string text, SyntaxNode precedingTrivia, SyntaxNode followingTrivia)
        {
            return (XmlNameTokenSyntax)new XmlNameTokenSyntax.Green(text, precedingTrivia?.GreenNode, followingTrivia?.GreenNode).CreateRed();
        }

        /*  <summary>
        ''' Represents an empty XML element of the form &lt;element /&gt;
        ''' </summary>
        */
        public static XmlEmptyElementSyntax XmlEmptyElement(PunctuationSyntax lessThanToken,
                                                             XmlNameSyntax name,
                                                             SyntaxList<SyntaxNode> attributes,
                                                             PunctuationSyntax slashGreaterThanToken)
        {
            return XmlEmptyElement(lessThanToken, name, attributes.Node, slashGreaterThanToken);
        }

        public static XmlEmptyElementSyntax XmlEmptyElement(
            PunctuationSyntax lessThanToken,
            XmlNameSyntax name,
            SyntaxNode attributes,
            PunctuationSyntax slashGreaterThanToken)
        {
            Debug.Assert(lessThanToken != null && lessThanToken.Kind == SyntaxKind.LessThanToken);
            Debug.Assert(name != null);
            Debug.Assert(slashGreaterThanToken != null && slashGreaterThanToken.Kind == SyntaxKind.SlashGreaterThanToken);
            return (XmlEmptyElementSyntax)new XmlEmptyElementSyntax.Green(lessThanToken.GreenNode, name.GreenNode, attributes.GreenNode, slashGreaterThanToken.GreenNode).CreateRed();
        }

        /*  <summary>
        ''' Represents a string of XML characters embedded as the content of an XML
        ''' element.
        ''' </summary>
        */

        public static XmlStringSyntax XmlString(PunctuationSyntax startQuoteToken, SyntaxList<XmlTextTokenSyntax> textTokens, PunctuationSyntax endQuoteToken)
        {
            return XmlString(startQuoteToken, textTokens.Node, endQuoteToken);
        }

        public static XmlStringSyntax XmlString(PunctuationSyntax startQuoteToken, SyntaxNode textTokens, PunctuationSyntax endQuoteToken)
        {
            //Debug.Assert(startQuoteToken != null && SyntaxFacts.IsXmlStringStartQuoteToken(startQuoteToken.Kind));
            //Debug.Assert(endQuoteToken != null && SyntaxFacts.IsXmlStringEndQuoteToken(endQuoteToken.Kind));

            return (XmlStringSyntax)new XmlStringSyntax.Green(startQuoteToken.GreenNode, textTokens.GreenNode, endQuoteToken.GreenNode).CreateRed();
        }

        /*  <summary>
        ''' Represents an XML document prologue option - version, encoding, standalone or
        ''' whitespace in an XML literal expression.
        ''' </summary>
        */
        public static XmlDeclarationOptionSyntax XmlDeclarationOption(XmlNameTokenSyntax name, PunctuationSyntax equals, XmlStringSyntax value)
        {
            Debug.Assert(name != null && name.Kind == SyntaxKind.XmlNameToken);
            Debug.Assert(equals != null && equals.Kind == SyntaxKind.EqualsToken);
            Debug.Assert(value != null);

            return (XmlDeclarationOptionSyntax)new XmlDeclarationOptionSyntax.Green(name.GreenNode, equals.GreenNode, value.GreenNode).CreateRed();
        }

        /*  <summary>
        ''' Represents the XML declaration prologue in an XML literal expression.
        ''' </summary>
        */
        public static XmlDeclarationSyntax XmlDeclaration(PunctuationSyntax lessThanQuestionToken, SyntaxToken xmlKeyword, XmlDeclarationOptionSyntax version, XmlDeclarationOptionSyntax encoding, XmlDeclarationOptionSyntax standalone, PunctuationSyntax questionGreaterThanToken)
        {
            Debug.Assert(lessThanQuestionToken != null && lessThanQuestionToken.Kind == SyntaxKind.LessThanQuestionToken);
            //Debug.Assert(xmlKeyword != null && xmlKeyword.Kind == SyntaxKind.XmlKeyword);
            Debug.Assert(version != null);
            Debug.Assert(questionGreaterThanToken != null && questionGreaterThanToken.Kind == SyntaxKind.QuestionGreaterThanToken);
            return (XmlDeclarationSyntax)new XmlDeclarationSyntax.Green(lessThanQuestionToken.GreenNode, xmlKeyword.GreenNode, version.GreenNode, encoding.GreenNode, standalone.GreenNode, questionGreaterThanToken.GreenNode).CreateRed();
        }

        public static XmlAttributeSyntax XmlAttribute(XmlNameSyntax name, PunctuationSyntax equals, XmlNodeSyntax value)
        {
            return (XmlAttributeSyntax)new XmlAttributeSyntax.Green(name.GreenNode, equals.GreenNode, value.GreenNode).CreateRed();
        }

        public static XmlPrefixSyntax XmlPrefix(XmlNameTokenSyntax localName, PunctuationSyntax colon)
        {
            return (XmlPrefixSyntax)new XmlPrefixSyntax.Green(localName.GreenNode, colon.GreenNode).CreateRed();
        }

        public static XmlProcessingInstructionSyntax XmlProcessingInstruction(
            PunctuationSyntax beginProcessingInstruction,
            XmlNameTokenSyntax name,
            SyntaxList<SyntaxNode> toList,
            PunctuationSyntax endProcessingInstruction)
        {
            return XmlProcessingInstruction(beginProcessingInstruction, name, toList.Node, endProcessingInstruction);
        }

        public static XmlProcessingInstructionSyntax XmlProcessingInstruction(
            PunctuationSyntax beginProcessingInstruction,
            XmlNameTokenSyntax name,
            SyntaxNode toList,
            PunctuationSyntax endProcessingInstruction)
        {
            return (XmlProcessingInstructionSyntax)new XmlProcessingInstructionSyntax.Green(beginProcessingInstruction.GreenNode, name.GreenNode, toList.GreenNode, endProcessingInstruction.GreenNode).CreateRed();
        }

        public static XmlTextTokenSyntax XmlTextLiteralToken(string text, SyntaxList<SyntaxNode> leadingTrivia, SyntaxList<SyntaxNode> trailingTrivia)
        {
            return XmlTextLiteralToken(text, leadingTrivia.Node, trailingTrivia.Node);
        }

        public static XmlTextTokenSyntax XmlTextLiteralToken(string text, SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
        {
            return (XmlTextTokenSyntax)new XmlTextTokenSyntax.Green(text, leadingTrivia.GreenNode, trailingTrivia.GreenNode).CreateRed();
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
        public static XmlEntityTokenSyntax XmlEntityLiteralToken(string text, string value, SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
        {
            Debug.Assert(value != null);
            return (XmlEntityTokenSyntax)new XmlEntityTokenSyntax.Green(text, value, leadingTrivia.GreenNode, trailingTrivia.GreenNode).CreateRed();
        }

        public static SyntaxTrivia WhitespaceTrivia(string text)
        {
            Debug.Assert(text != null);
            return (SyntaxTrivia)new SyntaxTrivia.Green(SyntaxKind.WhitespaceTrivia, text).CreateRed();
        }

        public static SyntaxTrivia EndOfLineTrivia(string text)
        {
            return (SyntaxTrivia)new SyntaxTrivia.Green(SyntaxKind.EndOfLineTrivia, text).CreateRed();
        }

        public static XmlCDataSectionSyntax XmlCDataSection(
            PunctuationSyntax beginCData,
            SyntaxList<XmlTextTokenSyntax> result,
            PunctuationSyntax endCData)
        {
            return XmlCDataSection(beginCData, result.Node, endCData);
        }

        public static XmlCDataSectionSyntax XmlCDataSection(
            PunctuationSyntax beginCData,
            SyntaxNode result,
            PunctuationSyntax endCData)
        {
            return (XmlCDataSectionSyntax)new XmlCDataSectionSyntax.Green(beginCData.GreenNode, result.GreenNode, endCData.GreenNode).CreateRed();
        }

        public static XmlNodeSyntax XmlComment(
            PunctuationSyntax beginComment,
            SyntaxList<XmlTextTokenSyntax> result,
            PunctuationSyntax endComment)
        {
            return XmlComment(beginComment, result.Node, endComment);
        }

        public static XmlNodeSyntax XmlComment(
            PunctuationSyntax beginComment,
            SyntaxNode result,
            PunctuationSyntax endComment)
        {
            return (XmlCommentSyntax)new XmlCommentSyntax.Green(beginComment.GreenNode, result.GreenNode, endComment.GreenNode).CreateRed();
        }

        public static KeywordSyntax Keyword(string name, SyntaxList<SyntaxNode> leadingTrivia, SyntaxList<SyntaxNode> trailingTrivia)
        {
            return Keyword(name, leadingTrivia.Node, trailingTrivia.Node);
        }

        public static KeywordSyntax Keyword(string name, SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
        {
            return (KeywordSyntax)new KeywordSyntax.Green(name, leadingTrivia.GreenNode, trailingTrivia.GreenNode).CreateRed();
        }

        /// <summary>
        /// Creates an empty list of syntax nodes.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        public static SyntaxList<TNode> List<TNode>() where TNode : SyntaxNode
        {
            return default(SyntaxList<TNode>);
        }

        /// <summary>
        /// Creates a singleton list of syntax nodes.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        /// <param name="node">The single element node.</param>
        /// <returns></returns>
        public static SyntaxList<TNode> SingletonList<TNode>(TNode node) where TNode : SyntaxNode
        {
            return new SyntaxList<TNode>(node);
        }

        /// <summary>
        /// Creates a list of syntax nodes.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        /// <param name="nodes">A sequence of element nodes.</param>
        public static SyntaxList<TNode> List<TNode>(IEnumerable<TNode> nodes) where TNode : SyntaxNode
        {
            return new SyntaxList<TNode>(nodes);
        }
    }
}
