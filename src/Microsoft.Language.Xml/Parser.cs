using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.Language.Xml
{
    public class Parser
    {
        private readonly Scanner _scanner;
        private SyntaxToken currentToken;
        private SyntaxListPool _pool = new SyntaxListPool();
        private Buffer buffer;
        private CancellationToken cancellationToken;

        public Parser(Buffer buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.buffer = buffer;
            this._scanner = new Scanner(buffer);
            this.cancellationToken = cancellationToken;
        }

        public static XmlNodeSyntax ParseText(string xml)
        {
            var buffer = new StringBuffer(xml);
            var parser = new Parser(buffer);
            var root = parser.Parse();
            return root;
        }

        public XmlNodeSyntax Parse()
        {
            //Debug.Assert(
            //    CurrentToken.Kind == SyntaxKind.LessThanToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanGreaterThanToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanSlashToken ||
            //    CurrentToken.Kind == SyntaxKind.BeginCDataToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanExclamationMinusMinusToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanQuestionToken,
            //    "Invalid XML");

            XmlNodeSyntax Result = null;
            if (CurrentToken.Kind == SyntaxKind.LessThanQuestionToken)
            {
                Result = ParseXmlDocument();
            }
            else
            {
                Result = ParseXmlElements(ScannerState.Content);
                if (!(Result is XmlDocumentSyntax))
                {
                    Result = SyntaxFactory.XmlDocument(null, null, Result, null, CurrentToken);
                }
            }

            SetParentsAndStartPositions(Result);

            return Result;
        }

        private void SetParentsAndStartPositions(SyntaxNode node, SyntaxNode parent = null, int start = 0)
        {
            node.Parent = parent;
            node.Start = start;

            foreach (var child in node.ChildNodes)
            {
                SetParentsAndStartPositions(child, node, start);
                start += child.FullWidth;
            }
        }

        public XmlNodeSyntax ParseXmlElements(ScannerState state)
        {
            XmlNodeSyntax element = null;
            int totalWidth = 0;
            var parts = new List<SyntaxNode>();
            do
            {
                element = ParseXmlElement(state);
                if (element == null || element.FullWidth == 0)
                {
                    break;
                }

                totalWidth += element.FullWidth;
                parts.Add(element);
            }
            while (element != null);

            if (parts.Count > 1)
            {
                element = SyntaxFactory.XmlElement(null, new SyntaxList<SyntaxNode>(SyntaxList.List(parts.ToArray())), null);
            }
            else if (parts.Count == 1)
            {
                element = (XmlNodeSyntax)parts[0];
            }

            return element;
        }

        public SyntaxToken CurrentToken
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

        public SyntaxToken PrevToken
        {
            get
            {
                return _scanner.PrevToken;
            }
        }

        public XmlNodeSyntax ParseXmlDocument()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanQuestionToken);
            var whitespaceChecker = new XmlWhitespaceChecker();

            var prologue = ParseXmlDeclaration();
            prologue = whitespaceChecker.Visit(prologue) as XmlDeclarationSyntax;

            SyntaxNode node = prologue;
            var precedingMisc = ParseXmlMisc(true, whitespaceChecker, ref node);
            prologue = node as XmlDeclarationSyntax;
            XmlNodeSyntax body = null;
            SyntaxList<XmlNodeSyntax> followingMisc = null;

            body = ParseXmlElements(ScannerState.Content);

            node = body;
            followingMisc = ParseXmlMisc(false, whitespaceChecker, ref node);
            body = node as XmlNodeSyntax;

            //Debug.Assert(CurrentToken.Kind == SyntaxKind.EndOfFileToken);

            return SyntaxFactory.XmlDocument(prologue, precedingMisc, body, followingMisc, CurrentToken);
        }

        private XmlProcessingInstructionSyntax ParseXmlProcessingInstruction(ScannerState nextState, XmlWhitespaceChecker whitespaceChecker)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanQuestionToken, "ParseXmlPI called on the wrong token.");
            var beginProcessingInstruction = ((PunctuationSyntax)CurrentToken);
            GetNextToken(ScannerState.Element);
            XmlNameTokenSyntax name = null;

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

            XmlTextTokenSyntax textToken = null;
            var values = _pool.Allocate<XmlTextTokenSyntax>();
            if (CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken || CurrentToken.Kind == SyntaxKind.DocumentationCommentLineBreakToken)
            {
                textToken = ((XmlTextTokenSyntax)CurrentToken);
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

                    textToken = ((XmlTextTokenSyntax)CurrentToken);
                }
            }

            PunctuationSyntax endProcessingInstruction = null;
            VerifyExpectedToken(SyntaxKind.QuestionGreaterThanToken, ref endProcessingInstruction, nextState);
            var result = SyntaxFactory.XmlProcessingInstruction(
                beginProcessingInstruction,
                name,
                values.ToList(),
                endProcessingInstruction);
            result = ((XmlProcessingInstructionSyntax)whitespaceChecker.Visit(result));
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
        private bool VerifyExpectedToken<T>(SyntaxKind kind, ref T token, ScannerState state = ScannerState.Content) where T : SyntaxToken
        {
            SyntaxToken current = CurrentToken;
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

        public XmlNodeSyntax ParseXmlElement(ScannerState enclosingState)
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

            XmlNodeSyntax xml = null;
            var contexts = new List<XmlContext>(0);
            XmlElementEndTagSyntax endElement;
            var nextState = enclosingState;
            var whitespaceChecker = new XmlWhitespaceChecker();

            bool exitDo = false;
            do
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.LessThanToken:
                        bool nextTokenIsSlash = PeekNextToken(ScannerState.Element).Kind == SyntaxKind.SlashToken;
                        if (nextTokenIsSlash)
                        {
                            goto case SyntaxKind.LessThanSlashToken;
                        }

                        xml = ParseXmlElementStartTag(nextState);
                        xml = whitespaceChecker.Visit(xml) as XmlNodeSyntax;

                        if (xml.Kind == SyntaxKind.XmlElementStartTag)
                        {
                            var startElement = xml as XmlElementStartTagSyntax;
                            contexts.Add(new XmlContext(_pool, startElement));
                            nextState = ScannerState.Content;
                            continue;
                        }

                        break;
                    case SyntaxKind.LessThanSlashToken:
                        endElement = ParseXmlElementEndTag(nextState);
                        endElement = whitespaceChecker.Visit(endElement) as XmlElementEndTagSyntax;

                        if (contexts.Count > 0)
                        {
                            xml = CreateXmlElement(contexts, endElement);
                        }
                        else
                        {
                            var missingLessThan = SyntaxFactory.MissingPunctuation(SyntaxKind.LessThanToken);
                            var missingXmlNameToken = SyntaxFactory.MissingToken(SyntaxKind.XmlNameToken) as XmlNameTokenSyntax;
                            var missingName = SyntaxFactory.XmlName(null, missingXmlNameToken);
                            var missingGreaterThan = SyntaxFactory.MissingPunctuation(SyntaxKind.GreaterThanToken);
                            var startElement = SyntaxFactory.XmlElementStartTag(missingLessThan, missingName, null, missingGreaterThan);

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
                        xml = ParseXmlProcessingInstruction(nextState, whitespaceChecker);
                        xml = whitespaceChecker.Visit(xml) as XmlProcessingInstructionSyntax;
                        break;
                    case SyntaxKind.BeginCDataToken:
                        xml = ParseXmlCData(nextState);
                        break;
                    case SyntaxKind.XmlTextLiteralToken:
                    case SyntaxKind.XmlEntityLiteralToken:
                    case SyntaxKind.DocumentationCommentLineBreakToken:
                        SyntaxKind newKind = default(SyntaxKind);
                        var textTokens = _pool.Allocate<XmlTextTokenSyntax>();
                        do
                        {
                            textTokens.Add(CurrentToken as XmlTextTokenSyntax);
                            GetNextToken(nextState);
                            newKind = CurrentToken.Kind;
                        }
                        while (newKind == SyntaxKind.XmlTextLiteralToken ||
                               newKind == SyntaxKind.XmlEntityLiteralToken ||
                               newKind == SyntaxKind.DocumentationCommentLineBreakToken);

                        var textResult = textTokens.ToList();
                        _pool.Free(textTokens);
                        xml = SyntaxFactory.XmlText(new SyntaxList<SyntaxToken>(textResult.Node));
                        break;
                    case SyntaxKind.BadToken:
                        var badToken = CurrentToken as BadTokenSyntax;

                        if (badToken.SubKind == SyntaxSubKind.BeginDocTypeToken)
                        {
                            var docTypeTrivia = ParseXmlDocType(ScannerState.Element);
                            xml = SyntaxFactory.XmlText(SyntaxFactory.MissingToken(SyntaxKind.XmlTextLiteralToken));
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

        private SyntaxNode ParseXmlDocType(ScannerState enclosingState)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.BadToken && ((BadTokenSyntax)CurrentToken).SubKind == SyntaxSubKind.BeginDocTypeToken, "ParseDTD called on wrong token.");

            var builder = SyntaxListBuilder<SyntaxNode>.Create();
            var beginDocType = ((BadTokenSyntax)CurrentToken);
            builder.Add(beginDocType);

            XmlNameTokenSyntax name = null;
            GetNextToken(ScannerState.DocType);
            VerifyExpectedToken(SyntaxKind.XmlNameToken, ref name, ScannerState.DocType);

            builder.Add(name);
            ParseExternalID(builder);
            ParseInternalSubSet(builder);
            PunctuationSyntax greaterThan = null;
            VerifyExpectedToken(SyntaxKind.GreaterThanToken, ref greaterThan, enclosingState);
            builder.Add(greaterThan);
            return builder.ToList().Node;
        }

        private void ParseExternalID(SyntaxListBuilder<SyntaxNode> builder)
        {
            if (CurrentToken.Kind == SyntaxKind.XmlNameToken)
            {
                var name = ((XmlNameTokenSyntax)CurrentToken);
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

        private void ParseInternalSubSet(SyntaxListBuilder<SyntaxNode> builder)
        {
            SyntaxList<SyntaxToken> unexpected = null;
            if (CurrentToken.Kind != SyntaxKind.BadToken || ((BadTokenSyntax)CurrentToken).SubKind != SyntaxSubKind.OpenBracketToken)
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

            if (CurrentToken.Kind == SyntaxKind.BadToken && ((BadTokenSyntax)CurrentToken).SubKind == SyntaxSubKind.OpenBracketToken)
            {
                //Assume we're on the '['
                builder.Add(CurrentToken);
                GetNextToken(ScannerState.DocType);
                if (CurrentToken.Kind == SyntaxKind.BadToken && ((BadTokenSyntax)CurrentToken).SubKind == SyntaxSubKind.LessThanExclamationToken)
                {
                    builder.Add(CurrentToken);
                    GetNextToken(ScannerState.DocType);
                    ParseXmlMarkupDecl(builder);
                }

                if (CurrentToken.Kind != SyntaxKind.BadToken || ((BadTokenSyntax)CurrentToken).SubKind != SyntaxSubKind.CloseBracketToken)
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

        private void ParseXmlMarkupDecl(SyntaxListBuilder<SyntaxNode> builder)
        {
            while (true)
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.BadToken:
                        builder.Add(CurrentToken);
                        var badToken = ((BadTokenSyntax)CurrentToken);
                        GetNextToken(ScannerState.DocType);
                        if (badToken.SubKind == SyntaxSubKind.LessThanExclamationToken)
                        {
                            ParseXmlMarkupDecl(builder);
                        }

                        break;
                    case SyntaxKind.LessThanQuestionToken:
                        var xmlPI = ParseXmlProcessingInstruction(ScannerState.DocType, null);
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

        private XmlCDataSectionSyntax ParseXmlCData(ScannerState nextState)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.BeginCDataToken, "ParseXmlCData called on the wrong token.");
            var beginCData = ((PunctuationSyntax)CurrentToken);
            GetNextToken(ScannerState.CData);
            var values = _pool.Allocate<XmlTextTokenSyntax>();
            while (CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken || CurrentToken.Kind == SyntaxKind.DocumentationCommentLineBreakToken)
            {
                values.Add(((XmlTextTokenSyntax)CurrentToken));
                GetNextToken(ScannerState.CData);
            }

            PunctuationSyntax endCData = null;
            VerifyExpectedToken(SyntaxKind.EndCDataToken, ref endCData, nextState);
            var result = values.ToList();
            _pool.Free(values);
            return SyntaxFactory.XmlCDataSection(beginCData, result, endCData);
        }

        private XmlNodeSyntax ParseXmlComment(ScannerState nextState)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanExclamationMinusMinusToken, "ParseXmlComment called on wrong token.");
            PunctuationSyntax beginComment = ((PunctuationSyntax)CurrentToken);
            GetNextToken(ScannerState.Comment);
            var values = _pool.Allocate<XmlTextTokenSyntax>();
            while (CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken || CurrentToken.Kind == SyntaxKind.DocumentationCommentLineBreakToken)
            {
                var textToken = ((XmlTextTokenSyntax)CurrentToken);
                if (textToken.Text.Length == 2 && textToken.Text == "--")
                {
                    textToken = ReportSyntaxError(textToken, ERRID.ERR_IllegalXmlCommentChar);
                }

                values.Add(textToken);
                GetNextToken(ScannerState.Comment);
            }

            PunctuationSyntax endComment = null;
            VerifyExpectedToken(SyntaxKind.MinusMinusGreaterThanToken, ref endComment, nextState);
            var result = values.ToList();
            _pool.Free(values);
            return SyntaxFactory.XmlComment(beginComment, result, endComment);
        }

        private T ReportSyntaxError<T>(T xml, ERRID eRR_XmlEndElementNoMatchingStart, params object[] parameters) where T : SyntaxNode
        {
            return xml;
            // TODO: Implement.
        }

        private XmlNodeSyntax ParseXmlElementStartTag(ScannerState enclosingState)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanToken, "ParseXmlElement call on wrong token.");
            PunctuationSyntax lessThan = ((PunctuationSyntax)CurrentToken);
            GetNextToken(ScannerState.Element);
            var Name = ParseXmlQualifiedName(false, true, ScannerState.Element, ScannerState.Element);
            var nameIsFollowedByWhitespace = Name.HasTrailingTrivia;
            var Attributes = ParseXmlAttributes(!nameIsFollowedByWhitespace, Name);
            PunctuationSyntax greaterThan = null;
            PunctuationSyntax endEmptyElementToken = null;
            switch ((CurrentToken.Kind))
            {
                case SyntaxKind.GreaterThanToken:
                    // Element with content
                    greaterThan = ((PunctuationSyntax)CurrentToken);
                    GetNextToken(ScannerState.Content);
                    return SyntaxFactory.XmlElementStartTag(lessThan, Name, Attributes.Node, greaterThan);
                case SyntaxKind.SlashGreaterThanToken:
                    // Empty element
                    endEmptyElementToken = ((PunctuationSyntax)CurrentToken);
                    GetNextToken(enclosingState);
                    return SyntaxFactory.XmlEmptyElement(lessThan, Name, Attributes, endEmptyElementToken);
                case SyntaxKind.SlashToken:
                    // Looks like an empty element but  / followed by '>' is an error when there is whitespace between the tokens.
                    if (PeekNextToken(ScannerState.Element).Kind == SyntaxKind.GreaterThanToken)
                    {
                        SyntaxToken divideToken = CurrentToken;
                        GetNextToken(ScannerState.Element);
                        greaterThan = ((PunctuationSyntax)CurrentToken);
                        GetNextToken(enclosingState);
                        var unexpectedSyntax = new SyntaxList<SyntaxToken>(SyntaxList.List(divideToken, greaterThan));
                        endEmptyElementToken = new PunctuationSyntax(SyntaxKind.SlashGreaterThanToken, "", null, null)
                            .AddLeadingSyntax(unexpectedSyntax, ERRID.ERR_IllegalXmlWhiteSpace);
                        return SyntaxFactory.XmlEmptyElement(lessThan, Name, Attributes, endEmptyElementToken);
                    }
                    else
                    {
                        return ResyncXmlElement(enclosingState, lessThan, Name, Attributes);
                    }
                default:
                    return ResyncXmlElement(enclosingState, lessThan, Name, Attributes);
            }
        }

        private XmlNodeSyntax ResyncXmlElement(ScannerState state, PunctuationSyntax lessThan, XmlNameSyntax Name, SyntaxList<SyntaxNode> attributes)
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
            PunctuationSyntax greaterThan;
            //TODO - Don't add an error if the unexpectedSyntax already has errors.
            switch (CurrentToken.Kind)
            {
                case SyntaxKind.SlashGreaterThanToken:
                    var endEmptyElementToken = ((PunctuationSyntax)CurrentToken);
                    if (unexpectedSyntax.Node != null)
                    {
                        endEmptyElementToken = endEmptyElementToken.AddLeadingSyntax(unexpectedSyntax, ERRID.ERR_ExpectedGreater);
                    }

                    GetNextToken(state);
                    return SyntaxFactory.XmlEmptyElement(lessThan, Name, attributes, endEmptyElementToken);
                case SyntaxKind.GreaterThanToken:
                    greaterThan = ((PunctuationSyntax)CurrentToken);
                    GetNextToken(ScannerState.Content);
                    if (unexpectedSyntax.Node != null)
                    {
                        greaterThan = greaterThan.AddLeadingSyntax(unexpectedSyntax, ERRID.ERR_ExpectedGreater);
                    }

                    return SyntaxFactory.XmlElementStartTag(lessThan, Name, attributes.Node, greaterThan);
                default:
                    // Try to avoid spurios missing '>' error message. Only report error if no skipped text
                    // and attributes are error free.
                    greaterThan = SyntaxFactory.MissingPunctuation(SyntaxKind.GreaterThanToken);
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

                    return SyntaxFactory.XmlElementStartTag(lessThan, Name, attributes.Node, greaterThan);
            }
        }

        private SyntaxList<SyntaxToken> ResyncAt(ScannerState state, SyntaxKind[] resyncTokens)
        {
            var skippedTokens = this._pool.Allocate<SyntaxToken>();
            ResyncAt(skippedTokens, state, resyncTokens);
            var result = skippedTokens.ToList();
            this._pool.Free(skippedTokens);
            return result;
        }

        private void ResyncAt(SyntaxListBuilder<SyntaxToken> skippedTokens, ScannerState state, SyntaxKind[] resyncTokens)
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

        private static bool IsTokenOrKeyword(SyntaxToken token, SyntaxKind[] kinds)
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

        private SyntaxList<XmlNodeSyntax> ParseXmlAttributes(bool requireLeadingWhitespace, XmlNodeSyntax xmlElementName)
        {
            var Attributes = this._pool.Allocate<XmlNodeSyntax>();
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

        public XmlNodeSyntax ParseXmlAttribute(bool requireLeadingWhitespace, bool AllowNameAsExpression, XmlNodeSyntax xmlElementName)
        {
            Debug.Assert(IsToken(
                CurrentToken,
                SyntaxKind.XmlNameToken,
                SyntaxKind.LessThanPercentEqualsToken,
                SyntaxKind.EqualsToken,
                SyntaxKind.SingleQuoteToken,
                SyntaxKind.DoubleQuoteToken), "ParseXmlAttribute called on wrong token.");
            XmlNodeSyntax Result = null;
            if (CurrentToken.Kind == SyntaxKind.XmlNameToken ||
                (AllowNameAsExpression && CurrentToken.Kind == SyntaxKind.LessThanPercentEqualsToken) ||
                CurrentToken.Kind == SyntaxKind.EqualsToken ||
                CurrentToken.Kind == SyntaxKind.SingleQuoteToken ||
                CurrentToken.Kind == SyntaxKind.DoubleQuoteToken)
            {
                var Name = ParseXmlQualifiedName(requireLeadingWhitespace, true, ScannerState.Element, ScannerState.Element);
                if (CurrentToken.Kind == SyntaxKind.EqualsToken)
                {
                    var equals = ((PunctuationSyntax)CurrentToken);
                    GetNextToken(ScannerState.Element);
                    XmlNodeSyntax value = null;

                    // Try parsing as a string (quoted or unquoted)
                    value = ParseXmlString(ScannerState.Element);
                    Result = SyntaxFactory.XmlAttribute(Name, equals, value);
                }
                else
                {
                    XmlNodeSyntax value;
                    if (CurrentToken.Kind != SyntaxKind.SingleQuoteToken && CurrentToken.Kind != SyntaxKind.DoubleQuoteToken)
                    {
                        var missingQuote = ((PunctuationSyntax)SyntaxFactory.MissingToken(SyntaxKind.SingleQuoteToken));
                        value = SyntaxFactory.XmlString(missingQuote, null, missingQuote);
                    }
                    else
                    {
                        // Case of quoted string without attribute name
                        // Try parsing as a string (quoted or unquoted)
                        value = ParseXmlString(ScannerState.Element);
                    }

                    Result = SyntaxFactory.XmlAttribute(Name, ((PunctuationSyntax)HandleUnexpectedToken(SyntaxKind.EqualsToken)), value);
                }
            }

            return Result;
        }

        private object HandleUnexpectedToken(SyntaxKind kind)
        {
            var t = SyntaxFactory.MissingToken(kind);
            return ReportSyntaxError(t, ERRID.ERR_MissingXmlEndTag);
        }

        public XmlStringSyntax ParseXmlString(ScannerState nextState)
        {
            ScannerState state;
            PunctuationSyntax startQuote = null;
            if (CurrentToken.Kind == SyntaxKind.SingleQuoteToken)
            {
                state = CurrentToken.Text == "'" ? ScannerState.SingleQuotedString : ScannerState.SmartSingleQuotedString;
                startQuote = ((PunctuationSyntax)CurrentToken);
                GetNextToken(state);
            }
            else if (CurrentToken.Kind == SyntaxKind.DoubleQuoteToken)
            {
                state = CurrentToken.Text == "\"" ? ScannerState.QuotedString : ScannerState.SmartQuotedString;
                startQuote = ((PunctuationSyntax)CurrentToken);
                GetNextToken(state);
            }
            else
            {
                // this is not a quote.
                // Let's parse the stuff as if it is quoted, but complain that quote is missing
                state = ScannerState.UnQuotedString;
                startQuote = ((PunctuationSyntax)SyntaxFactory.MissingToken(SyntaxKind.SingleQuoteToken));
                startQuote = ReportSyntaxError(startQuote, ERRID.ERR_StartAttributeValue);
                ResetCurrentToken(state);
            }

            var list = _pool.Allocate<XmlTextTokenSyntax>();
            while (true)
            {
                var kind = CurrentToken.Kind;
                switch (kind)
                {
                    case SyntaxKind.SingleQuoteToken:
                    case SyntaxKind.DoubleQuoteToken:
                        {
                            var endQuote = ((PunctuationSyntax)CurrentToken);
                            GetNextToken(nextState);
                            var result = SyntaxFactory.XmlString(startQuote, list.ToList(), endQuote);
                            _pool.Free(list);
                            return result;
                        }
                    case SyntaxKind.XmlTextLiteralToken:
                    case SyntaxKind.XmlEntityLiteralToken:
                    case SyntaxKind.DocumentationCommentLineBreakToken:
                        list.Add(((XmlTextTokenSyntax)CurrentToken));
                        break;
                    default:
                        {
                            var endQuote = HandleUnexpectedToken(startQuote.Kind);
                            var result = SyntaxFactory.XmlString(startQuote, list.ToList(), ((PunctuationSyntax)endQuote));
                            _pool.Free(list);
                            return result;
                        }
                }

                GetNextToken(state);
            }
        }

        private XmlNameSyntax ParseXmlQualifiedName(bool requireLeadingWhitespace, bool allowExpr, ScannerState stateForName, ScannerState nextState)
        {
            switch ((CurrentToken.Kind))
            {
                case SyntaxKind.XmlNameToken:
                    return ParseXmlQualifiedName(requireLeadingWhitespace, stateForName, nextState);
            }

            ResetCurrentToken(nextState);
            return ReportExpectedXmlName();
        }

        private XmlNameSyntax ReportExpectedXmlName()
        {
            return ReportSyntaxError(
                SyntaxFactory.XmlName(
                    null,
                    SyntaxFactory.XmlNameToken(
                        "",
                        null,
                        null)),
                ERRID.ERR_ExpectedXmlName);
        }

        private XmlNameSyntax ParseXmlQualifiedName(
            bool requireLeadingWhitespace,
            ScannerState stateForName,
            ScannerState nextState)
        {
            var hasPrecedingWhitespace = requireLeadingWhitespace &&
                (PrevToken.GetTrailingTrivia() != null && PrevToken.GetTrailingTrivia().ContainsWhitespaceTrivia() ||
                CurrentToken.GetLeadingTrivia() != null && CurrentToken.GetLeadingTrivia().ContainsWhitespaceTrivia());
            var localName = ((XmlNameTokenSyntax)CurrentToken);
            GetNextToken(stateForName);
            if (requireLeadingWhitespace && !hasPrecedingWhitespace)
            {
                localName = ReportSyntaxError(localName, ERRID.ERR_ExpectedXmlWhiteSpace);
            }

            XmlPrefixSyntax prefix = null;
            if (CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                PunctuationSyntax colon = ((PunctuationSyntax)CurrentToken);
                GetNextToken(stateForName);
                prefix = SyntaxFactory.XmlPrefix(localName, colon);
                if (CurrentToken.Kind == SyntaxKind.XmlNameToken)
                {
                    localName = ((XmlNameTokenSyntax)CurrentToken);
                    GetNextToken(stateForName);
                    if (colon.HasTrailingTrivia || localName.HasLeadingTrivia)
                    {
                        localName = ReportSyntaxError(localName, ERRID.ERR_ExpectedXmlName);
                    }
                }
                else
                {
                    localName = ReportSyntaxError(SyntaxFactory.XmlNameToken("", null, null), ERRID.ERR_ExpectedXmlName);
                }
            }

            var name = SyntaxFactory.XmlName(prefix, localName);
            ResetCurrentToken(nextState);
            return name;
        }

        private void ResetCurrentToken(ScannerState enclosingState)
        {
            _scanner.ResetCurrentToken(enclosingState);
            currentToken = null;
        }

        private XmlNodeSyntax CreateXmlElement(List<XmlContext> contexts, XmlElementEndTagSyntax endElement)
        {
            var i = contexts.MatchEndElement(endElement.NameNode);
            XmlNodeSyntax element;
            if (i >= 0)
            {
                var last = contexts.Count - 1;
                while (last > i)
                {
                    var missingEndElement = SyntaxFactory.XmlElementEndTag(
                        ((PunctuationSyntax)HandleUnexpectedToken(SyntaxKind.LessThanSlashToken)),
                        ReportSyntaxError(SyntaxFactory.XmlName(null, SyntaxFactory.XmlNameToken("", null, null)), ERRID.ERR_ExpectedXmlName),
                        ((PunctuationSyntax)HandleUnexpectedToken(SyntaxKind.GreaterThanToken)));
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
                var nameExpr = contexts.Peek().StartElement.NameNode;
                if (nameExpr.Kind == SyntaxKind.XmlName)
                {
                    var name = ((XmlNameSyntax)nameExpr);
                    if (name.Prefix != null)
                    {
                        prefix = name.Prefix.Name.Text;
                        colon = ":";
                    }

                    localName = name.LocalName.Text;
                }

                endElement = ReportSyntaxError(endElement, ERRID.ERR_MismatchedXmlEndTag, prefix, colon, localName);
                element = contexts.Peek().CreateElement(endElement, ErrorFactory.ErrorInfo(ERRID.ERR_MissingXmlEndTag));
            }

            contexts.Pop();
            return element;
        }

        private XmlElementEndTagSyntax ParseXmlElementEndTag(ScannerState nextState)
        {
            PunctuationSyntax beginEndElement = null;
            XmlNameSyntax name = null;
            PunctuationSyntax greaterToken = null;
            SyntaxList<SyntaxToken> unexpected = null;
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
                    var lessThan = ((PunctuationSyntax)CurrentToken);
                    SyntaxToken slashToken = PeekNextToken(ScannerState.EndElement);
                    if (slashToken.Kind == SyntaxKind.SlashToken)
                    {
                        if (lessThan.HasTrailingTrivia || slashToken.HasLeadingTrivia)
                        {
                            beginEndElement = beginEndElement.AddLeadingSyntax(
                                SyntaxList.List(lessThan, slashToken),
                                ERRID.ERR_IllegalXmlWhiteSpace);
                        }
                        else
                        {
                            beginEndElement = (
                                (PunctuationSyntax)SyntaxFactory.Token(
                                    lessThan.GetLeadingTrivia(),
                                    SyntaxKind.LessThanSlashToken,
                                    slashToken.GetTrailingTrivia(),
                                    lessThan.Text + slashToken.Text));
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
                name = ((XmlNameSyntax)ParseXmlQualifiedName(false, false, ScannerState.EndElement, ScannerState.EndElement));
            }

            VerifyExpectedToken(SyntaxKind.GreaterThanToken, ref greaterToken, nextState);
            return SyntaxFactory.XmlElementEndTag(beginEndElement, name, greaterToken);
        }

        private bool IsToken(SyntaxToken currentToken, params SyntaxKind[] kinds)
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

        private SyntaxList<XmlNodeSyntax> ParseXmlMisc(bool IsProlog, XmlWhitespaceChecker whitespaceChecker, ref SyntaxNode outerNode)
        {
            var content = this._pool.Allocate<XmlNodeSyntax>();
            bool exitWhile = false;
            while (!exitWhile)
            {
                XmlNodeSyntax result = null;
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.BadToken:
                        var badToken = ((BadTokenSyntax)CurrentToken);
                        SyntaxNode skipped;
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
                        result = ParseXmlProcessingInstruction(ScannerState.Misc, whitespaceChecker);
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

        private XmlDeclarationSyntax ParseXmlDeclaration()
        {
            //Debug.Assert(CurrentToken.Kind == SyntaxKind.LessThanQuestionToken && PeekNextToken(ScannerState.Element).Kind == SyntaxKind.XmlNameToken && ((XmlNameTokenSyntax)PeekNextToken(ScannerState.Element)).PossibleKeywordKind == SyntaxKind.XmlKeyword, "ParseXmlDecl called on the wrong token.");
            var beginPrologue = ((PunctuationSyntax)CurrentToken);
            GetNextToken(ScannerState.Element);
            XmlNameTokenSyntax nameToken = null;
            VerifyExpectedToken(SyntaxKind.XmlNameToken, ref nameToken, ScannerState.Element);
            var encodingIndex = 0;
            var standaloneIndex = 0;
            var foundVersion = false;
            var foundEncoding = false;
            var foundStandalone = false;
            SyntaxNode[] nodes = new SyntaxNode[4];
            int i = 0;
            nodes[i] = _scanner.MakeKeyword(nameToken);
            i += 1;

            bool exitWhile = false;
            while (!exitWhile)
            {
                XmlDeclarationOptionSyntax nextOption;
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.XmlNameToken:
                        var optionName = ((XmlNameTokenSyntax)CurrentToken);
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

            SyntaxList<SyntaxToken> unexpected = null;
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

            PunctuationSyntax endPrologue = null;
            VerifyExpectedToken(SyntaxKind.QuestionGreaterThanToken, ref endPrologue, ScannerState.Content);
            if (unexpected.Node != null)
            {
                endPrologue = endPrologue.AddLeadingSyntax(unexpected, ERRID.ERR_ExpectedXmlName);
            }

            Debug.Assert(foundVersion == (nodes[1] != null));
            if (nodes[1] == null)
            {
                var version = SyntaxFactory.XmlDeclarationOption(
                    ((XmlNameTokenSyntax)SyntaxFactory.MissingToken(SyntaxKind.XmlNameToken)),
                    SyntaxFactory.MissingPunctuation(SyntaxKind.EqualsToken),
                    CreateMissingXmlString());
                nodes[1] = ReportSyntaxError(version, ERRID.ERR_MissingVersionInXmlDecl);
            }

            return SyntaxFactory.XmlDeclaration(
                beginPrologue,
                (nodes[0] as SyntaxToken),
                (nodes[1] as XmlDeclarationOptionSyntax),
                encodingIndex == 0 ? null : (nodes[encodingIndex] as XmlDeclarationOptionSyntax),
                standaloneIndex == 0 ? null : (nodes[standaloneIndex] as XmlDeclarationOptionSyntax),
                endPrologue);
        }

        private XmlStringSyntax CreateMissingXmlString()
        {
            var missingDoubleQuote = SyntaxFactory.MissingPunctuation(SyntaxKind.DoubleQuoteToken);
            return SyntaxFactory.XmlString(missingDoubleQuote, null, missingDoubleQuote);
        }

        private XmlDeclarationOptionSyntax ParseXmlDeclarationOption()
        {
            Debug.Assert(IsToken(
                CurrentToken,
                SyntaxKind.XmlNameToken,
                SyntaxKind.LessThanPercentEqualsToken,
                SyntaxKind.EqualsToken,
                SyntaxKind.SingleQuoteToken,
                SyntaxKind.DoubleQuoteToken), "ParseXmlPrologueOption called on wrong token.");
            XmlDeclarationOptionSyntax result = null;
            XmlNameTokenSyntax name = null;
            PunctuationSyntax equals = null;
            XmlStringSyntax value = null;
            var hasPrecedingWhitespace = PrevToken.GetTrailingTrivia().ContainsWhitespaceTrivia() || CurrentToken.GetLeadingTrivia().ContainsWhitespaceTrivia();
            VerifyExpectedToken(SyntaxKind.XmlNameToken, ref name, ScannerState.Element);
            if (!hasPrecedingWhitespace)
            {
                name = ReportSyntaxError(name, ERRID.ERR_ExpectedXmlWhiteSpace);
            }

            SyntaxList<SyntaxToken> skipped = null;
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

            result = SyntaxFactory.XmlDeclarationOption(name, equals, value);
            return result;
        }

        private SyntaxToken PeekNextToken(ScannerState scannerState)
        {
            return _scanner.PeekNextToken(scannerState);
        }
    }
}
