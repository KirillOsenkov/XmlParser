using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;
    using static InternalSyntax.SyntaxFactory;

    public class Parser
    {
        private readonly Scanner _scanner;
        private SyntaxToken.Green currentToken;
        private SyntaxListPool _pool = new SyntaxListPool();
        private Buffer buffer;
        private CancellationToken cancellationToken;

        private Parser(Buffer buffer, Scanner scanner = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.buffer = buffer;
            this._scanner = scanner ?? new Scanner(buffer);
            this.cancellationToken = cancellationToken;
        }

        public static XmlDocumentSyntax ParseText(string xml)
        {
            var buffer = new StringBuffer(xml);
            return Parse(buffer);
        }

        public static XmlDocumentSyntax Parse(Buffer buffer)
        {
            var parser = new Parser(buffer);
            return ParseDocument(parser);
        }

        public static XmlDocumentSyntax ParseIncremental(string newXml, TextChangeRange[] changes, XmlDocumentSyntax previousDocument)
        {
            return ParseIncremental(new StringBuffer(newXml), changes, previousDocument);
        }

        public static XmlDocumentSyntax ParseIncremental(Buffer newBuffer, TextChangeRange[] changes, XmlDocumentSyntax previousDocument)
        {
            if (!CanParseIncrementally(previousDocument, newBuffer, changes))
                return Parse(newBuffer);

            var parser = new Parser(newBuffer, new Blender(newBuffer, changes, previousDocument));
            return ParseDocument(parser);
        }

        static XmlDocumentSyntax ParseDocument(Parser parser)
        {
            var root = parser.Parse();
            return (XmlDocumentSyntax)root.CreateRed();
        }

        static bool CanParseIncrementally(SyntaxNode root, Buffer newBuffer, TextChangeRange[] changes)
        {
            foreach (var change in changes)
            {
                // If the whole buffer changed, no need to do incremental parsing
                if (change.Span == root.Span)
                    return false;
                // If a special XML character has been entered, whole structure could have evolved
                for (int position = change.NewSpan.Start; position < change.NewSpan.End; position++)
                {
                    switch (newBuffer[position]) {
                        case '<':
                        case '>':
                        case '"':
                        case '\'':
                            return false;
                        default: continue;
                    }
                }
                for (int position = change.Span.Start; position < change.Span.End; position++)
                {
                    var nonTerminal = root.FindNode(position, includeTrivia: false, excludeTerminal: true);
                    if (nonTerminal == null)
                        continue;
                    // If one of the change touches a node name then all bets are off and we need to reparse the whole context
                    if (nonTerminal.Kind == SyntaxKind.XmlName && ((XmlNameSyntax)nonTerminal).IsXmlNodeName())
                        return false;
                    // Advance position to the end of the node we found
                    position += nonTerminal.FullWidth - position + nonTerminal.Start - 1;
                }
            }

            return true;
        }

        private XmlDocumentSyntax.Green Parse()
        {
            //Debug.Assert(
            //    CurrentToken.Kind == SyntaxKind.LessThanToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanGreaterThanToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanSlashToken ||
            //    CurrentToken.Kind == SyntaxKind.BeginCDataToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanExclamationMinusMinusToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanQuestionToken,
            //    "Invalid XML");

            XmlDocumentSyntax.Green result = null;
            if (CurrentToken.Kind == SyntaxKind.LessThanQuestionToken)
            {
                result = ParseXmlDocument();
            }
            else
            {
                var elements = ParseXmlElements(ScannerState.Content);
                if (!(elements is XmlDocumentSyntax.Green))
                {
                    result = XmlDocument(null, null, elements, null, CurrentToken);
                }
                else
                {
                    result = elements as XmlDocumentSyntax.Green;
                }
            }

            return result;
        }

        private XmlNodeSyntax.Green ParseXmlElements(ScannerState state)
        {
            XmlNodeSyntax.Green element = null;
            var parts = new List<GreenNode>();
            do
            {
                element = ParseXmlElement(state);
                if (element == null)
                    break;

                parts.Add(element);
            }
            while (element != null);

            if (parts.Count > 1)
            {
                element = XmlElement(null, InternalSyntax.SyntaxList.List(parts.ToArray()), null);
            }
            else if (parts.Count == 1)
            {
                element = (XmlNodeSyntax.Green)parts[0];
            }

            return element;
        }

        internal SyntaxToken.Green CurrentToken
        {
            get
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (currentToken == null)
                {
                    currentToken = _scanner.GetCurrentToken();
                }

                return currentToken;
            }
        }

        internal SyntaxToken.Green PrevToken
        {
            get
            {
                return _scanner.PrevToken;
            }
        }

        private XmlDocumentSyntax.Green ParseXmlDocument()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanQuestionToken);

            var prologue = ParseXmlDeclaration();

            GreenNode node = prologue;
            var precedingMisc = ParseXmlMisc(true, ref node);
            prologue = node as XmlDeclarationSyntax.Green;
            XmlNodeSyntax.Green body = null;
            InternalSyntax.SyntaxList<XmlNodeSyntax.Green> followingMisc = null;

            body = ParseXmlElement(ScannerState.Misc);

            node = body;
            followingMisc = ParseXmlMisc(false, ref node);
            body = node as XmlNodeSyntax.Green;

            //Debug.Assert(CurrentToken.Kind == SyntaxKind.EndOfFileToken);

            return XmlDocument(prologue, precedingMisc, body, followingMisc, CurrentToken);
        }

        private XmlProcessingInstructionSyntax.Green ParseXmlProcessingInstruction(ScannerState nextState)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanQuestionToken, "ParseXmlPI called on the wrong token.");
            var beginProcessingInstruction = ((PunctuationSyntax.Green)CurrentToken);
            GetNextToken(ScannerState.Element);
            XmlNameTokenSyntax.Green name = null;

            //TODO - name has to allow :. Dev10 puts a fully qualified name here.
            if (!VerifyExpectedToken(SyntaxKind.XmlNameToken, ref name, ScannerState.StartProcessingInstruction))
            {
                // In case there wasn't a name in the PI and the scanner returned another token from the element state,
                // the current token must be reset to a processing instruction token.
                ResetCurrentToken(ScannerState.StartProcessingInstruction);
            }

            if (name.Text.Length == 3 && name.Text.Equals("xml", StringComparison.OrdinalIgnoreCase))
            {
                name = ReportSyntaxError(name, ERRID.ERR_IllegalProcessingInstructionName, name.Text);
            }

            XmlTextTokenSyntax.Green textToken = null;
            var values = _pool.Allocate<XmlTextTokenSyntax.Green>();
            if (CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken || CurrentToken.Kind == SyntaxKind.DocumentationCommentLineBreakToken)
            {
                textToken = ((XmlTextTokenSyntax.Green)CurrentToken);
                if (!name.IsMissing && !name.GetTrailingTrivia().ContainsWhitespaceTrivia() && !textToken.GetLeadingTrivia().ContainsWhitespaceTrivia())
                {
                    textToken = ReportSyntaxError(textToken, ERRID.ERR_ExpectedXmlWhiteSpace);
                }

                while (true)
                {
                    values.Add(textToken);
                    GetNextToken(ScannerState.ProcessingInstruction);
                    if (CurrentToken.Kind != SyntaxKind.XmlTextLiteralToken && CurrentToken.Kind != SyntaxKind.DocumentationCommentLineBreakToken)
                    {
                        break;
                    }

                    textToken = ((XmlTextTokenSyntax.Green)CurrentToken);
                }
            }

            PunctuationSyntax.Green endProcessingInstruction = null;
            VerifyExpectedToken(SyntaxKind.QuestionGreaterThanToken, ref endProcessingInstruction, nextState);
            var result = XmlProcessingInstruction(
                beginProcessingInstruction,
                name,
                values.ToListNode(),
                endProcessingInstruction);
            _pool.Free(values);
            return result;
        }

        // Produce an error message if the current token is not the expected TokenType.
        // File: C:\dd\vs_langs01_1\src\vb\Language\Prototype\Dev11\Native\VB\Language\Compiler\Parser\Parser.cpp
        // Lines: 1021 - 1021
        // inline bool .Parser::VerifyExpectedToken( [ tokens TokenType ] [ _Inout_ bool& ErrorInConstruct ] )
        /*  <summary>
        ''' Check that the current token is the expected kind, the current node is consumed and optionally a new line
        ''' after the token.
        ''' </summary>
        ''' <param name="kind">The expected node kind.</param>
        ''' <returns>A token of the expected kind.  This node may be an empty token with an error attached to it</returns>
        ''' <remarks>Since nodes are immutable, the only way to create nodes with errors attached is to create a node without an error,
        ''' then add an error with this method to create another node.</remarks>
        */
        private bool VerifyExpectedToken<T>(SyntaxKind kind, ref T token, ScannerState state = ScannerState.Content) where T : SyntaxToken.Green
        {
            SyntaxToken.Green current = CurrentToken;
            if (current.Kind == kind)
            {
                token = ((T)current);
                GetNextToken(state);
                return true;
            }
            else
            {
                token = ((T)HandleUnexpectedToken(kind));
                return false;
            }
        }

        private XmlNodeSyntax.Green ParseXmlElement(ScannerState enclosingState)
        {
            //Debug.Assert(
            //    IsToken(CurrentToken,
            //        SyntaxKind.LessThanToken,
            //        SyntaxKind.LessThanGreaterThanToken,
            //        SyntaxKind.LessThanSlashToken,
            //        SyntaxKind.BeginCDataToken,
            //        SyntaxKind.LessThanExclamationMinusMinusToken,
            //        SyntaxKind.LessThanQuestionToken,
            //        SyntaxKind.LessThanPercentEqualsToken,
            //        SyntaxKind.XmlTextLiteralToken,
            //        SyntaxKind.BadToken),
            //    "ParseXmlElement call on wrong token.");

            XmlNodeSyntax.Green xml = null;
            var contexts = new List<XmlContext>(0);
            XmlElementEndTagSyntax.Green endElement;
            var nextState = enclosingState;

            bool exitDo = false;
            do
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.LessThanToken:
                        var reused = _scanner.GetCurrentSyntaxNode() as XmlNodeSyntax.Green;
                        if (reused != null && (reused.Kind == SyntaxKind.XmlElement || reused.Kind == SyntaxKind.XmlEmptyElement))
                        {
                            xml = reused;
                            GetNextSyntaxNode();
                            break;
                        }

                        bool nextTokenIsSlash = PeekNextToken(ScannerState.Element).Kind == SyntaxKind.SlashToken;
                        if (nextTokenIsSlash)
                        {
                            goto case SyntaxKind.LessThanSlashToken;
                        }

                        xml = ParseXmlElementStartTag(nextState);

                        if (xml.Kind == SyntaxKind.XmlElementStartTag)
                        {
                            var startElement = xml as XmlElementStartTagSyntax.Green;
                            contexts.Add(new XmlContext(_pool, startElement));
                            nextState = ScannerState.Content;
                            continue;
                        }

                        break;
                    case SyntaxKind.LessThanSlashToken:
                        endElement = ParseXmlElementEndTag(nextState);

                        if (contexts.Count > 0)
                        {
                            xml = CreateXmlElement(contexts, endElement);
                        }
                        else
                        {
                            var missingLessThan = MissingPunctuation(SyntaxKind.LessThanToken);
                            var missingXmlNameToken = MissingToken(SyntaxKind.XmlNameToken) as XmlNameTokenSyntax.Green;
                            var missingName = XmlName(null, missingXmlNameToken);
                            var missingGreaterThan = MissingPunctuation(SyntaxKind.GreaterThanToken);
                            var startElement = XmlElementStartTag(missingLessThan, missingName, null, missingGreaterThan);

                            contexts.Add(new XmlContext(_pool, startElement));
                            xml = contexts[contexts.Count - 1].CreateElement(endElement);
                            xml = ReportSyntaxError(xml, ERRID.ERR_XmlEndElementNoMatchingStart);
                            contexts.RemoveAt(contexts.Count - 1);
                        }

                        break;
                    case SyntaxKind.LessThanExclamationMinusMinusToken:
                        xml = ParseXmlComment(nextState);
                        break;
                    case SyntaxKind.LessThanQuestionToken:
                        xml = ParseXmlProcessingInstruction(nextState);
                        break;
                    case SyntaxKind.BeginCDataToken:
                        xml = ParseXmlCData(nextState);
                        break;
                    case SyntaxKind.XmlTextLiteralToken:
                    case SyntaxKind.XmlEntityLiteralToken:
                    case SyntaxKind.DocumentationCommentLineBreakToken:
                        SyntaxKind newKind = default(SyntaxKind);
                        var textTokens = _pool.Allocate<XmlTextTokenSyntax.Green>();
                        do
                        {
                            textTokens.Add(CurrentToken as XmlTextTokenSyntax.Green);
                            GetNextToken(nextState);
                            newKind = CurrentToken.Kind;
                        }
                        while (newKind == SyntaxKind.XmlTextLiteralToken ||
                               newKind == SyntaxKind.XmlEntityLiteralToken ||
                               newKind == SyntaxKind.DocumentationCommentLineBreakToken);

                        var textResult = textTokens.ToList();
                        _pool.Free(textTokens);
                        xml = XmlText(new InternalSyntax.SyntaxList<SyntaxToken.Green>(textResult.Node));
                        break;
                    case SyntaxKind.BadToken:
                        var badToken = CurrentToken as BadTokenSyntax.Green;

                        if (badToken.SubKind == SyntaxSubKind.BeginDocTypeToken)
                        {
                            var docTypeTrivia = ParseXmlDocType(ScannerState.Element);
                            xml = XmlText(MissingToken(SyntaxKind.XmlTextLiteralToken));
                            xml = xml.AddLeadingSyntax(docTypeTrivia, ERRID.ERR_DTDNotSupported);
                        }
                        else
                        {
                            // Let ParseXmlEndElement do the resync
                            exitDo = true;
                            continue;
                        }

                        break;
                    default:
                        exitDo = true;
                        continue;
                }

                if (contexts.Count > 0)
                {
                    contexts[contexts.Count - 1].Add(xml);
                }
                else
                {
                    exitDo = true;
                }
            }
            while (!exitDo);

            // Recover from improperly terminated element.
            // Close all contexts and return
            if (contexts.Count > 0)
            {
                while (true)
                {
                    endElement = ParseXmlElementEndTag(nextState);
                    xml = CreateXmlElement(contexts, endElement);
                    if (contexts.Count > 0)
                    {
                        contexts[contexts.Count - 1].Add(xml);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            ResetCurrentToken(enclosingState);

            return xml;
        }

        private GreenNode ParseXmlDocType(ScannerState enclosingState)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.BadToken && ((BadTokenSyntax.Green)CurrentToken).SubKind == SyntaxSubKind.BeginDocTypeToken, "ParseDTD called on wrong token.");

            var builder = InternalSyntax.SyntaxListBuilder<GreenNode>.Create();
            var beginDocType = ((BadTokenSyntax.Green)CurrentToken);
            builder.Add(beginDocType);

            XmlNameTokenSyntax.Green name = null;
            GetNextToken(ScannerState.DocType);
            VerifyExpectedToken(SyntaxKind.XmlNameToken, ref name, ScannerState.DocType);

            builder.Add(name);
            ParseExternalID(builder);
            ParseInternalSubSet(builder);
            PunctuationSyntax.Green greaterThan = null;
            VerifyExpectedToken(SyntaxKind.GreaterThanToken, ref greaterThan, enclosingState);
            builder.Add(greaterThan);
            return builder.ToList().Node;
        }

        private void ParseExternalID(InternalSyntax.SyntaxListBuilder<GreenNode> builder)
        {
            if (CurrentToken.Kind == SyntaxKind.XmlNameToken)
            {
                var name = ((XmlNameTokenSyntax.Green)CurrentToken);
                switch (name.ToString())
                {
                    case "SYSTEM":
                        {
                            builder.Add(name);
                            GetNextToken(ScannerState.DocType);
                            var systemLiteral = ParseXmlString(ScannerState.DocType);
                            builder.Add(systemLiteral);
                            break;
                        }
                    case "PUBLIC":
                        {
                            builder.Add(name);
                            GetNextToken(ScannerState.DocType);
                            var publicLiteral = ParseXmlString(ScannerState.DocType);
                            builder.Add(publicLiteral);
                            var systemLiteral = ParseXmlString(ScannerState.DocType);
                            builder.Add(systemLiteral);
                            break;
                        }
                }
            }
        }

        private void ParseInternalSubSet(InternalSyntax.SyntaxListBuilder<GreenNode> builder)
        {
            InternalSyntax.SyntaxList<GreenNode> unexpected = null;
            if (CurrentToken.Kind != SyntaxKind.BadToken || ((BadTokenSyntax.Green)CurrentToken).SubKind != SyntaxSubKind.OpenBracketToken)
            {
                unexpected = ResyncAt(ScannerState.DocType, new[]
                {
                    SyntaxKind.BadToken,
                    SyntaxKind.GreaterThanToken,
                    SyntaxKind.LessThanToken,
                    SyntaxKind.LessThanExclamationMinusMinusToken,
                    SyntaxKind.BeginCDataToken,
                    SyntaxKind.LessThanPercentEqualsToken,
                    SyntaxKind.EndOfXmlToken
                });
                if (unexpected.Node != null)
                {
                    builder.Add(unexpected.Node);
                }
            }

            if (CurrentToken.Kind == SyntaxKind.BadToken && ((BadTokenSyntax.Green)CurrentToken).SubKind == SyntaxSubKind.OpenBracketToken)
            {
                //Assume we're on the '['
                builder.Add(CurrentToken);
                GetNextToken(ScannerState.DocType);
                if (CurrentToken.Kind == SyntaxKind.BadToken && ((BadTokenSyntax.Green)CurrentToken).SubKind == SyntaxSubKind.LessThanExclamationToken)
                {
                    builder.Add(CurrentToken);
                    GetNextToken(ScannerState.DocType);
                    ParseXmlMarkupDecl(builder);
                }

                if (CurrentToken.Kind != SyntaxKind.BadToken || ((BadTokenSyntax.Green)CurrentToken).SubKind != SyntaxSubKind.CloseBracketToken)
                {
                    unexpected = ResyncAt(ScannerState.DocType, new[]
                    {
                        SyntaxKind.BadToken,
                        SyntaxKind.GreaterThanToken,
                        SyntaxKind.LessThanToken,
                        SyntaxKind.LessThanExclamationMinusMinusToken,
                        SyntaxKind.BeginCDataToken,
                        SyntaxKind.LessThanPercentEqualsToken,
                        SyntaxKind.EndOfXmlToken
                    });
                    if (unexpected.Node != null)
                    {
                        builder.Add(unexpected.Node);
                    }
                }

                // Assume we're on the ']'
                builder.Add(CurrentToken);
                GetNextToken(ScannerState.DocType);
            }
        }

        private void ParseXmlMarkupDecl(InternalSyntax.SyntaxListBuilder<GreenNode> builder)
        {
            while (true)
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.BadToken:
                        builder.Add(CurrentToken);
                        var badToken = ((BadTokenSyntax.Green)CurrentToken);
                        GetNextToken(ScannerState.DocType);
                        if (badToken.SubKind == SyntaxSubKind.LessThanExclamationToken)
                        {
                            ParseXmlMarkupDecl(builder);
                        }

                        break;
                    case SyntaxKind.LessThanQuestionToken:
                        var xmlPI = ParseXmlProcessingInstruction(ScannerState.DocType);
                        builder.Add(xmlPI);
                        break;
                    case SyntaxKind.LessThanExclamationMinusMinusToken:
                        var xmlComment = ParseXmlComment(ScannerState.DocType);
                        builder.Add(xmlComment);
                        break;
                    case SyntaxKind.GreaterThanToken:
                        builder.Add(CurrentToken);
                        GetNextToken(ScannerState.DocType);
                        return;
                    case SyntaxKind.EndOfFileToken:
                    case SyntaxKind.EndOfXmlToken:
                        return;
                    default:
                        builder.Add(CurrentToken);
                        GetNextToken(ScannerState.DocType);
                        break;
                }
            }
        }

        public void GetNextToken(ScannerState nextState)
        {
            _scanner.GetNextTokenInState(nextState);
            currentToken = null;
        }

        internal void GetNextSyntaxNode(ScannerState withState = ScannerState.Content)
        {
            _scanner.MoveToNextSyntaxNode(withState);
            currentToken = null;
        }

        private XmlCDataSectionSyntax.Green ParseXmlCData(ScannerState nextState)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.BeginCDataToken, "ParseXmlCData called on the wrong token.");
            var beginCData = ((PunctuationSyntax.Green)CurrentToken);
            GetNextToken(ScannerState.CData);
            var values = _pool.Allocate<XmlTextTokenSyntax.Green>();
            while (CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken || CurrentToken.Kind == SyntaxKind.DocumentationCommentLineBreakToken)
            {
                values.Add(((XmlTextTokenSyntax.Green)CurrentToken));
                GetNextToken(ScannerState.CData);
            }

            PunctuationSyntax.Green endCData = null;
            VerifyExpectedToken(SyntaxKind.EndCDataToken, ref endCData, nextState);
            var result = values.ToListNode();
            _pool.Free(values);
            return XmlCDataSection(beginCData, result, endCData);
        }

        private XmlNodeSyntax.Green ParseXmlComment(ScannerState nextState)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanExclamationMinusMinusToken, "ParseXmlComment called on wrong token.");
            PunctuationSyntax.Green beginComment = ((PunctuationSyntax.Green)CurrentToken);
            GetNextToken(ScannerState.Comment);
            var values = _pool.Allocate<XmlTextTokenSyntax.Green>();
            while (CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken || CurrentToken.Kind == SyntaxKind.DocumentationCommentLineBreakToken)
            {
                var textToken = ((XmlTextTokenSyntax.Green)CurrentToken);
                if (textToken.Text.Length == 2 && textToken.Text == "--")
                {
                    textToken = ReportSyntaxError(textToken, ERRID.ERR_IllegalXmlCommentChar);
                }

                values.Add(textToken);
                GetNextToken(ScannerState.Comment);
            }

            PunctuationSyntax.Green endComment = null;
            VerifyExpectedToken(SyntaxKind.MinusMinusGreaterThanToken, ref endComment, nextState);
            var result = values.ToListNode();
            _pool.Free(values);
            return XmlComment(beginComment, result, endComment);
        }

        private T ReportSyntaxError<T>(T xml, ERRID eRR_XmlEndElementNoMatchingStart, params object[] parameters) where T : GreenNode
        {
            return xml;
            // TODO: Implement.
        }

        private XmlNodeSyntax.Green ParseXmlElementStartTag(ScannerState enclosingState)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanToken, "ParseXmlElement call on wrong token.");
            PunctuationSyntax.Green lessThan = ((PunctuationSyntax.Green)CurrentToken);
            GetNextToken(ScannerState.Element);
            var Name = ParseXmlQualifiedName(false, true, ScannerState.Element, ScannerState.Element);
            var nameIsFollowedByWhitespace = Name.HasTrailingTrivia;
            var Attributes = ParseXmlAttributes(!nameIsFollowedByWhitespace, Name);
            PunctuationSyntax.Green greaterThan = null;
            PunctuationSyntax.Green endEmptyElementToken = null;
            switch ((CurrentToken.Kind))
            {
                case SyntaxKind.GreaterThanToken:
                    // Element with content
                    greaterThan = ((PunctuationSyntax.Green)CurrentToken);
                    GetNextToken(ScannerState.Content);
                    return XmlElementStartTag(lessThan, Name, Attributes.Node, greaterThan);
                case SyntaxKind.SlashGreaterThanToken:
                    // Empty element
                    endEmptyElementToken = ((PunctuationSyntax.Green)CurrentToken);
                    GetNextToken(enclosingState);
                    return XmlEmptyElement(lessThan, Name, Attributes.Node, endEmptyElementToken);
                case SyntaxKind.SlashToken:
                    // Looks like an empty element but  / followed by '>' is an error when there is whitespace between the tokens.
                    if (PeekNextToken(ScannerState.Element).Kind == SyntaxKind.GreaterThanToken)
                    {
                        SyntaxToken.Green divideToken = CurrentToken;
                        GetNextToken(ScannerState.Element);
                        greaterThan = ((PunctuationSyntax.Green)CurrentToken);
                        GetNextToken(enclosingState);
                        var unexpectedSyntax = InternalSyntax.SyntaxList.List(divideToken, greaterThan);
                        endEmptyElementToken = Punctuation(SyntaxKind.SlashGreaterThanToken, "", null, null)
                            .AddLeadingSyntax(unexpectedSyntax, ERRID.ERR_IllegalXmlWhiteSpace);
                        return XmlEmptyElement(lessThan, Name, Attributes.Node, endEmptyElementToken);
                    }
                    else
                    {
                        return ResyncXmlElement(enclosingState, lessThan, Name, Attributes);
                    }
                default:
                    return ResyncXmlElement(enclosingState, lessThan, Name, Attributes);
            }
        }

        private XmlNodeSyntax.Green ResyncXmlElement(ScannerState state, PunctuationSyntax.Green lessThan, XmlNameSyntax.Green Name, InternalSyntax.SyntaxList<GreenNode> attributes)
        {
            var unexpectedSyntax = ResyncAt(ScannerState.Element, new[]
            {
                SyntaxKind.SlashGreaterThanToken,
                SyntaxKind.GreaterThanToken,
                SyntaxKind.LessThanToken,
                SyntaxKind.LessThanSlashToken,
                SyntaxKind.LessThanPercentEqualsToken,
                SyntaxKind.BeginCDataToken,
                SyntaxKind.LessThanExclamationMinusMinusToken,
                SyntaxKind.LessThanQuestionToken,
                SyntaxKind.EndOfXmlToken
            }

            );
            PunctuationSyntax.Green greaterThan;
            //TODO - Don't add an error if the unexpectedSyntax already has errors.
            switch (CurrentToken.Kind)
            {
                case SyntaxKind.SlashGreaterThanToken:
                    var endEmptyElementToken = ((PunctuationSyntax.Green)CurrentToken);
                    if (unexpectedSyntax.Node != null)
                    {
                        endEmptyElementToken = endEmptyElementToken.AddLeadingSyntax(unexpectedSyntax, ERRID.ERR_ExpectedGreater);
                    }

                    GetNextToken(state);
                    return XmlEmptyElement(lessThan, Name, attributes.Node, endEmptyElementToken);
                case SyntaxKind.GreaterThanToken:
                    greaterThan = ((PunctuationSyntax.Green)CurrentToken);
                    GetNextToken(ScannerState.Content);
                    if (unexpectedSyntax.Node != null)
                    {
                        greaterThan = greaterThan.AddLeadingSyntax(unexpectedSyntax, ERRID.ERR_ExpectedGreater);
                    }

                    return XmlElementStartTag(lessThan, Name, attributes.Node, greaterThan);
                default:
                    // Try to avoid spurios missing '>' error message. Only report error if no skipped text
                    // and attributes are error free.
                    greaterThan = MissingPunctuation(SyntaxKind.GreaterThanToken);
                    if (unexpectedSyntax.Node == null)
                    {
                        ////if (!(attributes.Node != null && attributes.Node.ContainsDiagnostics))
                        ////{
                        ////    greaterThan = Parser.ReportSyntaxError(greaterThan, ERRID.ERR_ExpectedGreater);
                        ////}
                    }
                    else
                    {
                        greaterThan = greaterThan.AddLeadingSyntax(unexpectedSyntax, ERRID.ERR_Syntax);
                    }

                    return XmlElementStartTag(lessThan, Name, attributes.Node, greaterThan);
            }
        }

        private InternalSyntax.SyntaxList<SyntaxToken.Green> ResyncAt(ScannerState state, SyntaxKind[] resyncTokens)
        {
            var skippedTokens = this._pool.Allocate<SyntaxToken.Green>();
            ResyncAt(skippedTokens, state, resyncTokens);
            var result = skippedTokens.ToList();
            this._pool.Free(skippedTokens);
            return result;
        }

        private void ResyncAt(InternalSyntax.SyntaxListBuilder<SyntaxToken.Green> skippedTokens, ScannerState state, SyntaxKind[] resyncTokens)
        {
            Debug.Assert(resyncTokens != null);
            while (CurrentToken.Kind != SyntaxKind.EndOfFileToken)
            {
                if (CurrentToken.Kind == SyntaxKind.EndOfXmlToken)
                {
                    break;
                }

                if (IsTokenOrKeyword(CurrentToken, resyncTokens))
                {
                    break;
                }

                skippedTokens.Add(CurrentToken);
                GetNextToken(state);
            }
        }

        private static bool IsTokenOrKeyword(SyntaxToken.Green token, SyntaxKind[] kinds)
        {
            Debug.Assert(!kinds.Contains(SyntaxKind.IdentifierToken));
            if (token.Kind == SyntaxKind.IdentifierToken)
            {
                return false;
            }
            else
            {
                return Array.IndexOf(kinds, token.Kind) >= 0;
            }
        }

        private InternalSyntax.SyntaxList<XmlNodeSyntax.Green> ParseXmlAttributes(bool requireLeadingWhitespace, XmlNodeSyntax.Green xmlElementName)
        {
            var Attributes = this._pool.Allocate<XmlNodeSyntax.Green>();
            bool exitDo = false;
            do
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.XmlNameToken:
                    case SyntaxKind.LessThanPercentEqualsToken:
                    case SyntaxKind.EqualsToken:
                    case SyntaxKind.SingleQuoteToken:
                    case SyntaxKind.DoubleQuoteToken:
                        var attribute = ParseXmlAttribute(
                            requireLeadingWhitespace,
                            AllowNameAsExpression: true,
                            xmlElementName: xmlElementName);
                        Debug.Assert(attribute != null);
                        requireLeadingWhitespace = !attribute.HasTrailingTrivia;
                        Attributes.Add(attribute);
                        break;
                    default:
                        exitDo = true;
                        break;
                }
            }
            while (!exitDo);

            var result = Attributes.ToList();
            this._pool.Free(Attributes);
            return result;
        }

        private XmlNodeSyntax.Green ParseXmlAttribute(bool requireLeadingWhitespace, bool AllowNameAsExpression, XmlNodeSyntax.Green xmlElementName)
        {
            Debug.Assert(IsToken(
                CurrentToken,
                SyntaxKind.XmlNameToken,
                SyntaxKind.LessThanPercentEqualsToken,
                SyntaxKind.EqualsToken,
                SyntaxKind.SingleQuoteToken,
                SyntaxKind.DoubleQuoteToken), "ParseXmlAttribute called on wrong token.");
            XmlNodeSyntax.Green Result = null;
            if (CurrentToken.Kind == SyntaxKind.XmlNameToken ||
                (AllowNameAsExpression && CurrentToken.Kind == SyntaxKind.LessThanPercentEqualsToken) ||
                CurrentToken.Kind == SyntaxKind.EqualsToken ||
                CurrentToken.Kind == SyntaxKind.SingleQuoteToken ||
                CurrentToken.Kind == SyntaxKind.DoubleQuoteToken)
            {
                var reused = _scanner.GetCurrentSyntaxNode() as XmlNodeSyntax.Green;
                if (reused != null && reused.Kind == SyntaxKind.XmlAttribute)
                {
                    Result = reused;
                    GetNextSyntaxNode(ScannerState.Element);
                    return Result;
                }

                var Name = ParseXmlQualifiedName(requireLeadingWhitespace, true, ScannerState.Element, ScannerState.Element);
                if (CurrentToken.Kind == SyntaxKind.EqualsToken)
                {
                    var equals = ((PunctuationSyntax.Green)CurrentToken);
                    GetNextToken(ScannerState.Element);
                    XmlStringSyntax.Green value = null;

                    // Try parsing as a string (quoted or unquoted)
                    value = ParseXmlString(ScannerState.Element);
                    Result = XmlAttribute(Name, equals, value);
                }
                else
                {
                    XmlStringSyntax.Green value;
                    if (CurrentToken.Kind != SyntaxKind.SingleQuoteToken && CurrentToken.Kind != SyntaxKind.DoubleQuoteToken)
                    {
                        var missingQuote = ((PunctuationSyntax.Green)MissingToken(SyntaxKind.SingleQuoteToken));
                        value = XmlString(missingQuote, null, missingQuote);
                    }
                    else
                    {
                        // Case of quoted string without attribute name
                        // Try parsing as a string (quoted or unquoted)
                        value = ParseXmlString(ScannerState.Element);
                    }

                    Result = XmlAttribute(Name, ((PunctuationSyntax.Green)HandleUnexpectedToken(SyntaxKind.EqualsToken)), value);
                }
            }

            return Result;
        }

        private object HandleUnexpectedToken(SyntaxKind kind)
        {
            var t = MissingToken(kind);
            return ReportSyntaxError(t, ERRID.ERR_MissingXmlEndTag);
        }

        private XmlStringSyntax.Green ParseXmlString(ScannerState nextState)
        {
            ScannerState state;
            PunctuationSyntax.Green startQuote = null;
            if (CurrentToken.Kind == SyntaxKind.SingleQuoteToken)
            {
                state = CurrentToken.Text == "'" ? ScannerState.SingleQuotedString : ScannerState.SmartSingleQuotedString;
                startQuote = ((PunctuationSyntax.Green)CurrentToken);
                GetNextToken(state);
            }
            else if (CurrentToken.Kind == SyntaxKind.DoubleQuoteToken)
            {
                state = CurrentToken.Text == "\"" ? ScannerState.QuotedString : ScannerState.SmartQuotedString;
                startQuote = ((PunctuationSyntax.Green)CurrentToken);
                GetNextToken(state);
            }
            else
            {
                // this is not a quote.
                // Let's parse the stuff as if it is quoted, but complain that quote is missing
                state = ScannerState.UnQuotedString;
                startQuote = ((PunctuationSyntax.Green)MissingToken(SyntaxKind.SingleQuoteToken));
                startQuote = ReportSyntaxError(startQuote, ERRID.ERR_StartAttributeValue);
                ResetCurrentToken(state);
            }

            var list = _pool.Allocate<XmlTextTokenSyntax.Green>();
            while (true)
            {
                var kind = CurrentToken.Kind;
                switch (kind)
                {
                    case SyntaxKind.SingleQuoteToken:
                    case SyntaxKind.DoubleQuoteToken:
                        {
                            var endQuote = ((PunctuationSyntax.Green)CurrentToken);
                            GetNextToken(nextState);
                            var result = XmlString(startQuote, list.ToListNode(), endQuote);
                            _pool.Free(list);
                            return result;
                        }
                    case SyntaxKind.XmlTextLiteralToken:
                    case SyntaxKind.XmlEntityLiteralToken:
                    case SyntaxKind.DocumentationCommentLineBreakToken:
                        list.Add(((XmlTextTokenSyntax.Green)CurrentToken));
                        break;
                    default:
                        {
                            var endQuote = HandleUnexpectedToken(startQuote.Kind);
                            var result = XmlString(startQuote, list.ToListNode(), ((PunctuationSyntax.Green)endQuote));
                            _pool.Free(list);
                            return result;
                        }
                }

                GetNextToken(state);
            }
        }

        private XmlNameSyntax.Green ParseXmlQualifiedName(bool requireLeadingWhitespace, bool allowExpr, ScannerState stateForName, ScannerState nextState)
        {
            switch ((CurrentToken.Kind))
            {
                case SyntaxKind.XmlNameToken:
                    return ParseXmlQualifiedName(requireLeadingWhitespace, stateForName, nextState);
            }

            ResetCurrentToken(nextState);
            return ReportExpectedXmlName();
        }

        private XmlNameSyntax.Green ReportExpectedXmlName()
        {
            return ReportSyntaxError(
                XmlName(
                    null,
                    XmlNameToken(
                        "",
                        null,
                        null)),
                ERRID.ERR_ExpectedXmlName);
        }

        private XmlNameSyntax.Green ParseXmlQualifiedName(
            bool requireLeadingWhitespace,
            ScannerState stateForName,
            ScannerState nextState)
        {
            var hasPrecedingWhitespace = requireLeadingWhitespace &&
                (PrevToken != null && PrevToken.GetTrailingTrivia() != null && PrevToken.GetTrailingTrivia().ContainsWhitespaceTrivia() ||
                CurrentToken.GetLeadingTrivia() != null && CurrentToken.GetLeadingTrivia().ContainsWhitespaceTrivia());
            var localName = ((XmlNameTokenSyntax.Green)CurrentToken);
            GetNextToken(stateForName);
            if (requireLeadingWhitespace && !hasPrecedingWhitespace)
            {
                localName = ReportSyntaxError(localName, ERRID.ERR_ExpectedXmlWhiteSpace);
            }

            XmlPrefixSyntax.Green prefix = null;
            if (CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                PunctuationSyntax.Green colon = ((PunctuationSyntax.Green)CurrentToken);
                GetNextToken(stateForName);
                prefix = XmlPrefix(localName, colon);
                if (CurrentToken.Kind == SyntaxKind.XmlNameToken)
                {
                    localName = ((XmlNameTokenSyntax.Green)CurrentToken);
                    GetNextToken(stateForName);
                    if (colon.HasTrailingTrivia || localName.HasLeadingTrivia)
                    {
                        localName = ReportSyntaxError(localName, ERRID.ERR_ExpectedXmlName);
                    }
                }
                else
                {
                    localName = ReportSyntaxError(XmlNameToken("", null, null), ERRID.ERR_ExpectedXmlName);
                }
            }

            var name = XmlName(prefix, localName);
            ResetCurrentToken(nextState);
            return name;
        }

        private void ResetCurrentToken(ScannerState enclosingState)
        {
            _scanner.ResetCurrentToken(enclosingState);
            currentToken = null;
        }

        private XmlNodeSyntax.Green CreateXmlElement(List<XmlContext> contexts, XmlElementEndTagSyntax.Green endElement)
        {
            var i = contexts.MatchEndElement(endElement.NameNode);
            XmlNodeSyntax.Green element;
            if (i >= 0)
            {
                var last = contexts.Count - 1;
                while (last > i)
                {
                    var missingEndElement = XmlElementEndTag(
                        ((PunctuationSyntax.Green)HandleUnexpectedToken(SyntaxKind.LessThanSlashToken)),
                        ReportSyntaxError(XmlName(null, XmlNameToken("", null, null)), ERRID.ERR_ExpectedXmlName),
                        ((PunctuationSyntax.Green)HandleUnexpectedToken(SyntaxKind.GreaterThanToken)));
                    var xml = contexts.Peek().CreateElement(missingEndElement, ErrorFactory.ErrorInfo(ERRID.ERR_MissingXmlEndTag));
                    contexts.Pop();
                    if (contexts.Count > 0)
                    {
                        contexts.Peek().Add(xml);
                    }
                    else
                    {
                        break;
                    }

                    last -= 1;
                }

                if (endElement.IsMissing)
                {
                    element = contexts.Peek().CreateElement(endElement, ErrorFactory.ErrorInfo(ERRID.ERR_MissingXmlEndTag));
                }
                else
                {
                    element = contexts.Peek().CreateElement(endElement);
                }
            }
            else
            {
                var prefix = "";
                var colon = "";
                var localName = "";
                var nameExpr = ((XmlElementStartTagSyntax)contexts.Peek().StartElement.CreateRed()).NameNode;
                if (nameExpr.Kind == SyntaxKind.XmlName)
                {
                    var name = ((XmlNameSyntax)nameExpr);
                    if (name.PrefixNode != null)
                    {
                        prefix = name.PrefixNode.Name.Text;
                        colon = ":";
                    }

                    localName = name.LocalNameNode.Text;
                }

                endElement = ReportSyntaxError(endElement, ERRID.ERR_MismatchedXmlEndTag, prefix, colon, localName);
                element = contexts.Peek().CreateElement(endElement, ErrorFactory.ErrorInfo(ERRID.ERR_MissingXmlEndTag));
            }

            contexts.Pop();
            return element;
        }

        private XmlElementEndTagSyntax.Green ParseXmlElementEndTag(ScannerState nextState)
        {
            PunctuationSyntax.Green beginEndElement = null;
            XmlNameSyntax.Green name = null;
            PunctuationSyntax.Green greaterToken = null;
            InternalSyntax.SyntaxList<SyntaxToken.Green> unexpected = null;
            if (CurrentToken.Kind != SyntaxKind.LessThanSlashToken)
            {
                unexpected = ResyncAt(ScannerState.Content, new[]
                {
                    SyntaxKind.LessThanToken, SyntaxKind.LessThanSlashToken, SyntaxKind.EndOfXmlToken
                });
            }

            if (!VerifyExpectedToken(SyntaxKind.LessThanSlashToken, ref beginEndElement, ScannerState.EndElement))
            {
                // Check for '<' followed by '/'.  This is an error because whitespace is not allowed between the tokens.
                if (CurrentToken.Kind == SyntaxKind.LessThanToken)
                {
                    var lessThan = ((PunctuationSyntax.Green)CurrentToken);
                    SyntaxToken.Green slashToken = PeekNextToken(ScannerState.EndElement);
                    if (slashToken.Kind == SyntaxKind.SlashToken)
                    {
                        if (lessThan.HasTrailingTrivia || slashToken.HasLeadingTrivia)
                        {
                            beginEndElement = beginEndElement.AddLeadingSyntax(
                                InternalSyntax.SyntaxList.List(lessThan, slashToken),
                                ERRID.ERR_IllegalXmlWhiteSpace);
                        }
                        else
                        {
                            beginEndElement = Punctuation(
                                SyntaxKind.LessThanSlashToken,
                                lessThan.Text + slashToken.Text,
                                lessThan.GetLeadingTrivia(),
                                slashToken.GetTrailingTrivia());
                        }

                        GetNextToken(ScannerState.EndElement);
                        GetNextToken(ScannerState.EndElement);
                    }
                }
            }

            if (unexpected.Node != null)
            {
                ////if (unexpected.Node.ContainsDiagnostics())
                ////{
                ////    beginEndElement = beginEndElement.AddLeadingSyntax(unexpected);
                ////}
                ////else
                {
                    beginEndElement = beginEndElement.AddLeadingSyntax(unexpected, ERRID.ERR_ExpectedLT);
                }
            }

            if (CurrentToken.Kind == SyntaxKind.XmlNameToken)
            {
                // /* AllowExpr */' /* IsBracketed */'
                name = ((XmlNameSyntax.Green)ParseXmlQualifiedName(false, false, ScannerState.EndElement, ScannerState.EndElement));
            }

            VerifyExpectedToken(SyntaxKind.GreaterThanToken, ref greaterToken, nextState);
            return XmlElementEndTag(beginEndElement, name, greaterToken);
        }

        private bool IsToken(SyntaxToken.Green currentToken, params SyntaxKind[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
            {
                if (currentToken.Kind == kinds[i])
                {
                    return true;
                }
            }

            return false;
        }

        private InternalSyntax.SyntaxList<XmlNodeSyntax.Green> ParseXmlMisc(bool IsProlog, ref GreenNode outerNode)
        {
            var content = this._pool.Allocate<XmlNodeSyntax.Green>();
            bool exitWhile = false;
            while (!exitWhile)
            {
                XmlNodeSyntax.Green result = null;
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.BadToken:
                        var badToken = ((BadTokenSyntax.Green)CurrentToken);
                        GreenNode skipped;
                        if (badToken.SubKind == SyntaxSubKind.BeginDocTypeToken)
                        {
                            skipped = ParseXmlDocType(ScannerState.Misc);
                        }
                        else
                        {
                            skipped = badToken;
                            GetNextToken(ScannerState.Misc);
                        }

                        var count = content.Count;
                        if (count > 0)
                        {
                            content[count - 1] = content[count - 1].AddTrailingSyntax(skipped, ERRID.ERR_DTDNotSupported);
                        }
                        else
                        {
                            outerNode = outerNode.AddTrailingSyntax(skipped, ERRID.ERR_DTDNotSupported);
                        }

                        break;
                    case SyntaxKind.LessThanExclamationMinusMinusToken:
                        result = ParseXmlComment(ScannerState.Misc);
                        break;
                    case SyntaxKind.LessThanQuestionToken:
                        result = ParseXmlProcessingInstruction(ScannerState.Misc);
                        break;
                    default:
                        exitWhile = true;
                        break;
                }

                if (result != null)
                {
                    content.Add(result);
                }
            }

            var contentList = content.ToList();
            this._pool.Free(content);
            return contentList;
        }

        private XmlDeclarationSyntax.Green ParseXmlDeclaration()
        {
            //Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanQuestionToken && PeekNextToken(ScannerState.Element).Kind == SyntaxKind.XmlNameToken && ((XmlNameTokenSyntax)PeekNextToken(ScannerState.Element)).PossibleKeywordKind == SyntaxKind.XmlKeyword, "ParseXmlDecl called on the wrong token.");
            var beginPrologue = ((PunctuationSyntax.Green)CurrentToken);
            GetNextToken(ScannerState.Element);
            XmlNameTokenSyntax.Green nameToken = null;
            VerifyExpectedToken(SyntaxKind.XmlNameToken, ref nameToken, ScannerState.Element);
            var encodingIndex = 0;
            var standaloneIndex = 0;
            var foundVersion = false;
            var foundEncoding = false;
            var foundStandalone = false;
            GreenNode[] nodes = new GreenNode[4];
            int i = 0;
            nodes[i] = MakeKeyword(nameToken);
            i += 1;

            bool exitWhile = false;
            while (!exitWhile)
            {
                XmlDeclarationOptionSyntax.Green nextOption;
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.XmlNameToken:
                        var optionName = ((XmlNameTokenSyntax.Green)CurrentToken);
                        switch (optionName.ToString())
                        {
                            case "version":
                                nextOption = ParseXmlDeclarationOption();
                                if (foundVersion)
                                {
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_DuplicateXmlAttribute, optionName.ToString());
                                    nodes[i - 1] = nodes[i - 1].AddTrailingSyntax(nextOption);
                                    break;
                                }

                                foundVersion = true;
                                Debug.Assert(i == 1);
                                if (foundEncoding || foundStandalone)
                                {
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_VersionMustBeFirstInXmlDecl, "", "", optionName.ToString());
                                }

                                if (nextOption.Value.TextTokens.Node == null || nextOption.Value.TextTokens.Node.ToFullString() != "1.0")
                                {
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_InvalidAttributeValue1, "1.0");
                                }

                                nodes[i] = nextOption;
                                i += 1;
                                break;
                            case "encoding":
                                nextOption = ParseXmlDeclarationOption();
                                if (foundEncoding)
                                {
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_DuplicateXmlAttribute, optionName.ToString());
                                    nodes[i - 1] = nodes[i - 1].AddTrailingSyntax(nextOption);
                                    break;
                                }

                                foundEncoding = true;
                                if (foundStandalone)
                                {
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_AttributeOrder, "encoding", "standalone");
                                    nodes[i - 1] = nodes[i - 1].AddTrailingSyntax(nextOption);
                                    break;
                                }
                                else if (!foundVersion)
                                {
                                    nodes[i - 1] = nodes[i - 1].AddTrailingSyntax(nextOption);
                                    break;
                                }

                                Debug.Assert(i == 2);
                                encodingIndex = i;
                                nodes[i] = nextOption;
                                i += 1;
                                break;
                            case "standalone":
                                nextOption = ParseXmlDeclarationOption();
                                if (foundStandalone)
                                {
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_DuplicateXmlAttribute, optionName.ToString());
                                    nodes[i - 1] = nodes[i - 1].AddTrailingSyntax(nextOption);
                                    break;
                                }

                                foundStandalone = true;
                                if (!foundVersion)
                                {
                                    nodes[i - 1] = nodes[i - 1].AddTrailingSyntax(nextOption);
                                    break;
                                }

                                var stringValue = nextOption.Value.TextTokens.Node != null ? nextOption.Value.TextTokens.Node.ToFullString() : "";
                                if (stringValue != "yes" && stringValue != "no")
                                {
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_InvalidAttributeValue2, "yes", "no");
                                }

                                Debug.Assert(i == 2 || i == 3);
                                standaloneIndex = i;
                                nodes[i] = nextOption;
                                i += 1;
                                break;
                            default:
                                nextOption = ParseXmlDeclarationOption();
                                nextOption = ReportSyntaxError(nextOption, ERRID.ERR_IllegalAttributeInXmlDecl, "", "", nextOption.Name.ToString());
                                nodes[i - 1] = nodes[i - 1].AddTrailingSyntax(nextOption);
                                break;
                        }

                        break;
                    case SyntaxKind.LessThanPercentEqualsToken:
                        nextOption = ParseXmlDeclarationOption();
                        nodes[i - 1] = nodes[i - 1].AddTrailingSyntax(nextOption);
                        break;
                    default:
                        exitWhile = true;
                        break;
                }
            }

            InternalSyntax.SyntaxList<SyntaxToken.Green> unexpected = null;
            if (CurrentToken.Kind != SyntaxKind.QuestionGreaterThanToken)
            {
                unexpected = ResyncAt(ScannerState.Element, new[]
                    {
                        SyntaxKind.EndOfXmlToken,
                        SyntaxKind.QuestionGreaterThanToken,
                        SyntaxKind.LessThanToken,
                        SyntaxKind.LessThanPercentEqualsToken,
                        SyntaxKind.LessThanExclamationMinusMinusToken
                    });
            }

            PunctuationSyntax.Green endPrologue = null;
            VerifyExpectedToken(SyntaxKind.QuestionGreaterThanToken, ref endPrologue, ScannerState.Content);
            if (unexpected.Node != null)
            {
                endPrologue = endPrologue.AddLeadingSyntax(unexpected, ERRID.ERR_ExpectedXmlName);
            }

            Debug.Assert(foundVersion == (nodes[1] != null));
            if (nodes[1] == null)
            {
                var version = XmlDeclarationOption(
                    ((XmlNameTokenSyntax.Green)MissingToken(SyntaxKind.XmlNameToken)),
                    MissingPunctuation(SyntaxKind.EqualsToken),
                    CreateMissingXmlString());
                nodes[1] = ReportSyntaxError(version, ERRID.ERR_MissingVersionInXmlDecl);
            }

            return XmlDeclaration(
                beginPrologue,
                (nodes[0] as SyntaxToken.Green),
                (nodes[1] as XmlDeclarationOptionSyntax.Green),
                encodingIndex == 0 ? null : (nodes[encodingIndex] as XmlDeclarationOptionSyntax.Green),
                standaloneIndex == 0 ? null : (nodes[standaloneIndex] as XmlDeclarationOptionSyntax.Green),
                endPrologue);
        }

        internal GreenNode MakeKeyword(XmlNameTokenSyntax.Green xmlName)
        {
            //Debug.Assert(xmlName.PossibleKeywordKind != SyntaxKind.XmlNameToken);
            return Keyword(/*SyntaxKind.XmlNameToken, */xmlName.Text, xmlName.GetLeadingTrivia(), xmlName.GetTrailingTrivia());
        }

        private XmlStringSyntax.Green CreateMissingXmlString()
        {
            var missingDoubleQuote = MissingPunctuation(SyntaxKind.DoubleQuoteToken);
            return XmlString(missingDoubleQuote, null, missingDoubleQuote);
        }

        private XmlDeclarationOptionSyntax.Green ParseXmlDeclarationOption()
        {
            Debug.Assert(IsToken(
                CurrentToken,
                SyntaxKind.XmlNameToken,
                SyntaxKind.LessThanPercentEqualsToken,
                SyntaxKind.EqualsToken,
                SyntaxKind.SingleQuoteToken,
                SyntaxKind.DoubleQuoteToken), "ParseXmlPrologueOption called on wrong token.");
            XmlDeclarationOptionSyntax.Green result = null;
            XmlNameTokenSyntax.Green name = null;
            PunctuationSyntax.Green equals = null;
            XmlStringSyntax.Green value = null;
            var hasPrecedingWhitespace = PrevToken.GetTrailingTrivia().ContainsWhitespaceTrivia() || CurrentToken.GetLeadingTrivia().ContainsWhitespaceTrivia();
            VerifyExpectedToken(SyntaxKind.XmlNameToken, ref name, ScannerState.Element);
            if (!hasPrecedingWhitespace)
            {
                name = ReportSyntaxError(name, ERRID.ERR_ExpectedXmlWhiteSpace);
            }

            InternalSyntax.SyntaxList<SyntaxToken.Green> skipped = null;
            if (!VerifyExpectedToken(SyntaxKind.EqualsToken, ref equals, ScannerState.Element))
            {
                skipped = ResyncAt(ScannerState.Element, new[]
                {
                    SyntaxKind.SingleQuoteToken,
                    SyntaxKind.DoubleQuoteToken,
                    SyntaxKind.LessThanPercentEqualsToken,
                    SyntaxKind.QuestionGreaterThanToken,
                    SyntaxKind.EndOfXmlToken
                });
                equals = equals.AddTrailingSyntax(skipped);
            }

            switch (CurrentToken.Kind)
            {
                case SyntaxKind.SingleQuoteToken:
                case SyntaxKind.DoubleQuoteToken:
                    value = ParseXmlString(ScannerState.Element);
                    break;
                default:
                    value = CreateMissingXmlString();
                    break;
            }

            result = XmlDeclarationOption(name, equals, value);
            return result;
        }

        private SyntaxToken.Green PeekNextToken(ScannerState scannerState)
        {
            return _scanner.PeekNextToken(scannerState);
        }
    }
}
