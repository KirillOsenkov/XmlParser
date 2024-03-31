using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;
    using static InternalSyntax.SyntaxFactory;

    public class Scanner
    {
        public const int MaxTokensLookAheadBeyondEOL = 4;
        public const int MaxCharsLookBehind = 1;

        private ScannerToken _prevToken;
        protected ScannerToken _currentToken;
        protected int _lineBufferOffset; // marks the next character to read from _LineBuffer
        private int _bufferLen;
        private readonly List<ScannerToken> _tokens = new List<ScannerToken>();
        private Buffer buffer;
        private int _endOfTerminatorTrivia;
        private StringTable _stringTable = StringTable.GetInstance();
        private SyntaxListPool triviaListPool = new SyntaxListPool();
        private readonly PooledStringBuilder _sbPooled;
        private readonly StringBuilder _sb;
        private readonly char[] _internBuffer = new char[256];
        private TextKeyedCache<SyntaxTrivia.Green> _triviaCache = TextKeyedCache<SyntaxTrivia.Green>.GetInstance ();
        private TextKeyedCache<XmlNameTokenSyntax.Green> _nameTokenCache = TextKeyedCache<XmlNameTokenSyntax.Green>.GetInstance();

        public Scanner(Buffer buffer)
        {
            this.buffer = buffer;
            this._bufferLen = buffer.Length;
            _sbPooled = PooledStringBuilder.GetInstance();
            _sb = _sbPooled.Builder;
        }

        internal virtual bool TryCrumbleOnce()
        {
            Debug.Assert(false, "regular scanner has nothing to crumble");
            return false;
        }

        internal virtual GreenNode GetCurrentSyntaxNode()
        {
            return null;
        }

        internal virtual void MoveToNextSyntaxNode(ScannerState withState)
        {
            _prevToken = default(ScannerToken);
            ResetTokens(withState);
        }

        internal SyntaxToken.Green GetCurrentToken()
        {
            var tk = _currentToken.InnerTokenObject;
            if (tk == null)
            {
                Debug.Assert(_currentToken.Position == _lineBufferOffset);
                var state = _currentToken.State;
                tk = GetScannerToken(state);
                _currentToken = _currentToken.With(state, tk);
            }

            return tk;
        }

        internal SyntaxToken.Green PrevToken
        {
            get
            {
                return _prevToken.InnerTokenObject;
            }
        }

        internal void GetNextTokenInState(ScannerState state)
        {
            _prevToken = _currentToken;
            if (_tokens.Count == 0)
            {
                _currentToken = new ScannerToken(_lineBufferOffset, _endOfTerminatorTrivia, null, state);
            }
            else
            {
                _currentToken = _tokens[0];
                _tokens.RemoveAt(0);
                ResetCurrentToken(state);
            }
        }

        internal void ResetCurrentToken(ScannerState state)
        {
            if (state != _currentToken.State)
            {
                AbandonAllTokens();
                Debug.Assert(_currentToken.Position == _lineBufferOffset);
                Debug.Assert(_currentToken.EndOfTerminatorTrivia == _endOfTerminatorTrivia);
                _currentToken = _currentToken.With(state, null);
            }
        }

        private void AbandonAllTokens()
        {
            RevertState(_currentToken);
            _tokens.Clear();
            _currentToken = _currentToken.With(ScannerState.Content, null);
        }

        private void ResetTokens(ScannerState state)
        {
            Debug.Assert(_lineBufferOffset >= _currentToken.Position);
            _tokens.Clear();
            _currentToken = new ScannerToken(_lineBufferOffset, _endOfTerminatorTrivia, null, state);
        }

        internal SyntaxToken.Green PeekNextToken(ScannerState state)
        {
            if (_tokens.Count > 0)
            {
                var tk = _tokens[0];
                if (tk.State == state)
                {
                    return tk.InnerTokenObject;
                }
                else
                {
                    AbandonPeekedTokens();
                }
            }

            // ensure that current token has been read
            GetCurrentToken();
            return GetTokenAndAddToQueue(state);
        }

        private SyntaxToken.Green GetTokenAndAddToQueue(ScannerState state)
        {
            var lineBufferOffset = _lineBufferOffset;
            var endOfTerminatorTrivia = _endOfTerminatorTrivia;
            var tk = GetScannerToken(state);
            _tokens.Add(new ScannerToken(lineBufferOffset, endOfTerminatorTrivia, tk, state));
            return tk;
        }

        private void AbandonPeekedTokens()
        {
            if (_tokens.Count == 0)
            {
                return;
            }

            RevertState(_tokens[0]);
            _tokens.Clear();
        }

        private void RevertState(ScannerToken revertTo)
        {
            _lineBufferOffset = revertTo.Position;
            _endOfTerminatorTrivia = revertTo.EndOfTerminatorTrivia;
        }

        private SyntaxToken.Green GetScannerToken(ScannerState state)
        {
            SyntaxToken.Green token = null;

            switch (state)
            {
                case ScannerState.Misc:
                    token = ScanXmlMisc();
                    break;
                case ScannerState.Element:
                case ScannerState.EndElement:
                case ScannerState.DocType:
                    token = ScanXmlElement(state);
                    break;
                case ScannerState.Content:
                    token = ScanXmlContent();
                    break;
                case ScannerState.CData:
                    token = ScanXmlCData();
                    break;
                case ScannerState.StartProcessingInstruction:
                case ScannerState.ProcessingInstruction:
                    token = ScanXmlPIData(state);
                    break;
                case ScannerState.Comment:
                    token = ScanXmlComment();
                    break;
                case ScannerState.SingleQuotedString:
                    token = ScanXmlStringSingle();
                    break;
                case ScannerState.SmartSingleQuotedString:
                    token = ScanXmlStringSmartSingle();
                    break;
                case ScannerState.QuotedString:
                    token = ScanXmlStringDouble();
                    break;
                case ScannerState.SmartQuotedString:
                    token = ScanXmlStringSmartDouble();
                    break;
                case ScannerState.UnQuotedString:
                    token = ScanXmlStringUnQuoted();
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return token;
        }

        internal SyntaxToken.Green ScanXmlStringUnQuoted()
        {
            if (!CanGetChar())
            {
                return Eof;
            }

            var Here = 0;
            var scratch = GetScratch();
            while (CanGetCharAtOffset(Here))
            {
                char c = PeekAheadChar(Here);
                // this is for the case where opening quote is omitted, but closing is present
                if (c == '\'' || c == '"')
                {
                    if (Here > 0)
                    {
                        return XmlMakeAttributeDataToken(null, Here, scratch);
                    }
                    else
                    {
                        if (c == '\'')
                        {
                            return XmlMakeSingleQuoteToken(null, c, isOpening: false);
                        }
                        else
                        {
                            return XmlMakeDoubleQuoteToken(null, c, isOpening: false);
                        }
                    }
                }

                switch (c)
                {
                    case UCH_CR:
                    case UCH_LF:
                    case ' ':
                    case UCH_TAB:
                        if (Here > 0)
                        {
                            return XmlMakeAttributeDataToken(null, Here, scratch);
                        }
                        else
                        {
                            return MissingToken(null, SyntaxKind.SingleQuoteToken);
                        }
                    case '<':
                    case '>':
                    case '?':
                        // This cannot be in a string. terminate the string.
                        if (Here != 0)
                        {
                            return XmlMakeAttributeDataToken(null, Here, scratch);
                        }
                        else
                        {
                            return MissingToken(null, SyntaxKind.SingleQuoteToken);
                        }
                    case '&':
                        if (Here > 0)
                        {
                            return XmlMakeAttributeDataToken(null, Here, scratch);
                        }
                        else
                        {
                            return ScanXmlReference(null);
                        }
                    case '/':
                        if (CanGetCharAtOffset(Here + 1) && PeekAheadChar(Here + 1) == '>')
                        {
                            if (Here != 0)
                            {
                                return XmlMakeAttributeDataToken(null, Here, scratch);
                            }
                            else
                            {
                                return MissingToken(null, SyntaxKind.SingleQuoteToken);
                            }
                        }

                        goto default;
                    default:
                        var xmlCh = ScanXmlChar(Here);
                        if (xmlCh.Length == 0)
                        {
                            // bad char
                            if (Here > 0)
                            {
                                return XmlMakeAttributeDataToken(null, Here, scratch);
                            }
                            else
                            {
                                return XmlMakeBadToken(null, 1, ERRID.ERR_IllegalChar);
                            }
                        }

                        xmlCh.AppendTo(scratch);
                        Here += xmlCh.Length;
                        break;
                }
            }

            return XmlMakeAttributeDataToken(null, Here, scratch);
        }

        private SyntaxToken.Green ScanXmlStringSmartDouble()
        {
            return ScanXmlString(DWCH_RSMART_DQ, DWCH_LSMART_DQ, false);
        }

        internal SyntaxToken.Green ScanXmlString(char terminatingChar, char altTerminatingChar, bool isSingle)
        {
            var precedingTrivia = triviaListPool.Allocate<GreenNode>();
            SyntaxToken.Green result;
            var Here = 0;
            var scratch = GetScratch();
            while (CanGetCharAtOffset(Here))
            {
                char c = PeekAheadChar(Here);
                if (c == terminatingChar || c == altTerminatingChar)
                {
                    if (Here > 0)
                    {
                        result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch);
                        goto CleanUp;
                    }
                    else
                    {
                        if (isSingle)
                        {
                            result = XmlMakeSingleQuoteToken(precedingTrivia, c, isOpening: false);
                        }
                        else
                        {
                            result = XmlMakeDoubleQuoteToken(precedingTrivia, c, isOpening: false);
                        }

                        goto CleanUp;
                    }
                }

                switch (c)
                {
                    case UCH_CR:
                    case UCH_LF:
                        Here = SkipLineBreak(c, Here);
                        scratch.Append(UCH_SPACE);
                        result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch);
                        goto CleanUp;
                    case UCH_TAB:
                        scratch.Append(UCH_SPACE);
                        Here += 1;
                        break;
                    case '<':
                        // This cannot be in a string. terminate the string.
                        if (Here > 0)
                        {
                            result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch);
                            goto CleanUp;
                        }
                        else
                        {
                            var data = MissingToken(null, SyntaxKind.SingleQuoteToken);
                            if (precedingTrivia.Count > 0)
                            {
                                data = (SyntaxToken.Green)data.WithLeadingTrivia(precedingTrivia.ToListNode());
                            }

                            var errInfo = ErrorFactory.ErrorInfo(isSingle ? ERRID.ERR_ExpectedSQuote : ERRID.ERR_ExpectedQuote);
                            result = data;
                            result = ((SyntaxToken.Green)data.SetDiagnostics(new[]
                            {
                                errInfo
                            }));
                            goto CleanUp;
                        }
                    case '&':
                        if (Here > 0)
                        {
                            result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch);
                            goto CleanUp;
                        }
                        else
                        {
                            result = ScanXmlReference(precedingTrivia.ToList());
                            goto CleanUp;
                        }

                    default:
                        var xmlCh = ScanXmlChar(Here);
                        if (xmlCh.Length == 0)
                        {
                            // bad char
                            if (Here > 0)
                            {
                                result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch);
                                goto CleanUp;
                            }
                            else
                            {
                                result = XmlMakeBadToken(precedingTrivia, 1, ERRID.ERR_IllegalChar);
                                goto CleanUp;
                            }
                        }

                        xmlCh.AppendTo(scratch);
                        Here += xmlCh.Length;
                        break;
                }
            }

            // no more chars
            if (Here > 0)
            {
                result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch);
                goto CleanUp;
            }
            else
            {
                result = EofToken(precedingTrivia);
                goto CleanUp;
            }

        CleanUp:
            triviaListPool.Free(precedingTrivia);
            return result;
        }

        private StringBuilder GetScratch()
        {
            return _sb;
        }

        private XmlTextTokenSyntax.Green XmlMakeAttributeDataToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia, int tokenWidth, StringBuilder scratch)
        {
            return XmlMakeTextLiteralToken(precedingTrivia, tokenWidth, scratch);
        }

        private SyntaxToken.Green ScanXmlStringDouble()
        {
            return ScanXmlString('"', '"', isSingle: false);
        }

        private SyntaxToken.Green ScanXmlStringSmartSingle()
        {
            return ScanXmlString(DWCH_RSMART_Q, DWCH_LSMART_Q, isSingle: true);
        }

        private SyntaxToken.Green ScanXmlStringSingle()
        {
            return ScanXmlString('\'', '\'', isSingle: true);
        }

        internal SyntaxToken.Green ScanXmlComment()
        {
            InternalSyntax.SyntaxList<GreenNode> precedingTrivia = null;
            var Here = 0;
            while (CanGetCharAtOffset(Here))
            {
                char c = PeekAheadChar(Here);
                switch (c)
                {
                    case UCH_CR:
                    case UCH_LF:
                        return XmlMakeCommentToken(precedingTrivia, Here + LengthOfLineBreak(c, Here));
                    case '-':
                        if (CanGetCharAtOffset(Here + 1) && PeekAheadChar(Here + 1) == '-')
                        {
                            // // --> terminates an Xml comment but otherwise -- is an illegal character sequence.
                            // // The scanner will always returns "--" as a separate comment data string and the
                            // // the semantics will error if '--' is ever found.
                            // // Return valid characters up to the --
                            if (Here > 0)
                            {
                                return XmlMakeCommentToken(precedingTrivia, Here);
                            }

                            if (CanGetCharAtOffset(Here + 2))
                            {
                                c = PeekAheadChar(Here + 2);
                                Here += 2;
                                // // if > is not found then this is an error.  Return the -- string
                                if (c != '>')
                                {
                                    return XmlMakeCommentToken(precedingTrivia, 2);
                                }
                                else
                                {
                                    return XmlMakeEndCommentToken(precedingTrivia);
                                }
                            }
                        }

                        goto ScanChars;
                    default:
                    ScanChars:
                        var xmlCh = ScanXmlChar(Here);
                        if (xmlCh.Length != 0)
                        {
                            Here += xmlCh.Length;
                            continue;
                        }

                        // bad char
                        if (Here > 0)
                        {
                            return XmlMakeCommentToken(precedingTrivia, Here);
                        }
                        else
                        {
                            return XmlMakeBadToken(precedingTrivia, 1, ERRID.ERR_IllegalChar);
                        }
                }
            }

            // no more chars
            if (Here > 0)
            {
                return XmlMakeCommentToken(precedingTrivia, Here);
            }
            else
            {
                return EofToken(precedingTrivia);
            }
        }

        private PunctuationSyntax.Green XmlMakeEndCommentToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            Debug.Assert(PeekChar() == '-');
            Debug.Assert(PeekAheadChar(1) == '-');
            Debug.Assert(PeekAheadChar(2) == '>');
            AdvanceChar(3);
            return Punctuation(SyntaxKind.MinusMinusGreaterThanToken, "-->", precedingTrivia, null);
        }

        private XmlTextTokenSyntax.Green XmlMakeCommentToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia, int TokenWidth)
        {
            Debug.Assert(TokenWidth > 0);
            var text = GetText(TokenWidth); // GetTextNotInterned() in the original
            return XmlTextToken(text, precedingTrivia.Node, null);
        }

        internal SyntaxToken.Green ScanXmlPIData(ScannerState state)
        {
            // // Scan the PI data after the white space
            // // [16]    PI    ::=    '<?' PITarget (S (Char* - (Char* '?>' Char*)))? '?>'
            // // [17]    PITarget    ::=    Name - (('X' | 'x') ('M' | 'm') ('L' | 'l'))
            Debug.Assert(state == ScannerState.StartProcessingInstruction || state == ScannerState.ProcessingInstruction);

            var precedingTrivia = triviaListPool.Allocate<GreenNode>();
            SyntaxToken.Green result;

            if (state == ScannerState.StartProcessingInstruction && CanGetChar())
            {
                var c = PeekChar();
                switch (c)
                {
                    case UCH_CR:
                    case UCH_LF:
                    case ' ':
                    case UCH_TAB:
                        var wsTrivia = ScanXmlTrivia(c);
                        precedingTrivia.AddRange(wsTrivia);
                        break;
                }
            }

            var Here = 0;
            while (CanGetCharAtOffset(Here))
            {
                char c = PeekAheadChar(Here);
                switch (c)
                {
                    case UCH_CR:
                    case UCH_LF:
                        result = XmlMakeProcessingInstructionToken(precedingTrivia.ToList(), Here + LengthOfLineBreak(c, Here));
                        goto CleanUp;
                    case '?':
                        if (CanGetCharAtOffset(Here + 1) && PeekAheadChar(Here + 1) == '>')
                        {
                            //// If valid characters found then return them.
                            if (Here != 0)
                            {
                                result = XmlMakeProcessingInstructionToken(precedingTrivia.ToList(), Here);
                                goto CleanUp;
                            }

                            // // Create token for the '?>' termination sequence
                            result = XmlMakeEndProcessingInstructionToken(precedingTrivia.ToList());
                            goto CleanUp;
                        }

                        goto default;
                    default:
                        var xmlCh = ScanXmlChar(Here);
                        if (xmlCh.Length > 0)
                        {
                            Here += xmlCh.Length;
                            continue;
                        }

                        // bad char
                        if (Here != 0)
                        {
                            result = XmlMakeProcessingInstructionToken(precedingTrivia.ToList(), Here);
                            goto CleanUp;
                        }
                        else
                        {
                            result = XmlMakeBadToken(precedingTrivia.ToList(), 1, ERRID.ERR_IllegalChar);
                            goto CleanUp;
                        }
                }
            }

            // no more chars
            if (Here > 0)
            {
                result = XmlMakeProcessingInstructionToken(precedingTrivia.ToList(), Here);
            }
            else
            {
                result = EofToken(precedingTrivia.ToList());
            }

        CleanUp:
            triviaListPool.Free(precedingTrivia);
            return result;
        }

        private XmlTextTokenSyntax.Green XmlMakeProcessingInstructionToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia, int TokenWidth)
        {
            Debug.Assert(TokenWidth > 0);
            var text = GetText(TokenWidth); // was GetTextNotInterned
            return XmlTextToken(text, precedingTrivia.Node, null);
        }

        internal SyntaxToken.Green ScanXmlCData()
        {
            InternalSyntax.SyntaxList<GreenNode> precedingTrivia = null;

            var scratch = GetScratch();
            var Here = 0;
            while (CanGetCharAtOffset(Here))
            {
                char c = PeekAheadChar(Here);
                switch (c)
                {
                    case UCH_CR:
                    case UCH_LF:
                        Here = SkipLineBreak(c, Here);
                        scratch.Append(UCH_LF);
                        return XmlMakeCDataToken(precedingTrivia, Here, scratch);
                    case ']':
                        if (CanGetCharAtOffset(Here + 2) && PeekAheadChar(Here + 1) == ']' && PeekAheadChar(Here + 2) == '>')
                        {
                            //// If valid characters found then return them.
                            if (Here != 0)
                            {
                                return XmlMakeCDataToken(precedingTrivia, Here, scratch);
                            }

                            return XmlMakeEndCDataToken(precedingTrivia);
                        }

                        goto ScanChars;

                    default:
                    ScanChars:
                        var xmlCh = ScanXmlChar(Here);
                        if (xmlCh.Length == 0)
                        {
                            // bad char
                            if (Here > 0)
                            {
                                return XmlMakeCDataToken(precedingTrivia, Here, scratch);
                            }
                            else
                            {
                                return XmlMakeBadToken(precedingTrivia, 1, ERRID.ERR_IllegalChar);
                            }
                        }

                        xmlCh.AppendTo(scratch);
                        Here += xmlCh.Length;
                        break;
                }
            }

            // no more chars
            if (Here > 0)
            {
                return XmlMakeCDataToken(precedingTrivia, Here, scratch);
            }
            else
            {
                return EofToken(precedingTrivia);
            }
        }

        private PunctuationSyntax.Green XmlMakeEndCDataToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            Debug.Assert(PeekChar() == ']');
            Debug.Assert(PeekAheadChar(1) == ']');
            Debug.Assert(PeekAheadChar(2) == '>');
            AdvanceChar(3);
            return Punctuation(SyntaxKind.EndCDataToken, "]]>", precedingTrivia, null);
        }

        private XmlTextTokenSyntax.Green XmlMakeCDataToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia, int TokenWidth, StringBuilder scratch)
        {
            return XmlMakeTextLiteralToken(precedingTrivia, TokenWidth, scratch);
        }

        private SyntaxToken.Green ScanXmlElement(ScannerState state)
        {
            Debug.Assert(state == ScannerState.Element || state == ScannerState.EndElement || state == ScannerState.DocType);

            InternalSyntax.SyntaxList<GreenNode> leadingTrivia = null;
            while (CanGetChar())
            {
                char c = PeekChar();
                switch (c)
                {
                    // // Whitespace
                    // //  S    ::=    (#x20 | #x9 | #xD | #xA)+
                    case UCH_CR:
                    case UCH_LF:
                        leadingTrivia = ScanXmlTrivia(c);
                        break;
                    case ' ':
                    case UCH_TAB:
                        leadingTrivia = ScanXmlTrivia(c);
                        break;
                    case '/':
                        if (CanGetCharAtOffset(1) && PeekAheadChar(1) == '>')
                        {
                            return XmlMakeEndEmptyElementToken(leadingTrivia);
                        }

                        return XmlMakeDivToken(leadingTrivia);
                    case '>':
                        return XmlMakeGreaterToken(leadingTrivia);
                    case '=':
                        return XmlMakeEqualsToken(leadingTrivia);
                    case '\'':
                    case DWCH_LSMART_Q:
                    case DWCH_RSMART_Q:
                        return XmlMakeSingleQuoteToken(leadingTrivia, c, isOpening: true);
                    case '"':
                    case DWCH_LSMART_DQ:
                    case DWCH_RSMART_DQ:
                        return XmlMakeDoubleQuoteToken(leadingTrivia, c, isOpening: true);
                    case '<':
                        if (CanGetCharAtOffset(1))
                        {
                            char ch = PeekAheadChar(1);
                            switch (ch)
                            {
                                case '!':
                                    if (CanGetCharAtOffset(2))
                                    {
                                        switch ((PeekAheadChar(2)))
                                        {
                                            case '-':
                                                if (CanGetCharAtOffset(3) && PeekAheadChar(3) == '-')
                                                {
                                                    return XmlMakeBeginCommentToken(leadingTrivia, _scanNoTriviaFunc);
                                                }

                                                break;
                                            case '[':
                                                if (CanGetCharAtOffset(8) &&
                                                    PeekAheadChar(3) == 'C' &&
                                                    PeekAheadChar(4) == 'D' &&
                                                    PeekAheadChar(5) == 'A' &&
                                                    PeekAheadChar(6) == 'T' &&
                                                    PeekAheadChar(7) == 'A' &&
                                                    PeekAheadChar(8) == '[')
                                                {
                                                    return XmlMakeBeginCDataToken(leadingTrivia, _scanNoTriviaFunc);
                                                }

                                                break;
                                            case 'D':
                                                if (CanGetCharAtOffset(8) &&
                                                    PeekAheadChar(3) == 'O' &&
                                                    PeekAheadChar(4) == 'C' &&
                                                    PeekAheadChar(5) == 'T' &&
                                                    PeekAheadChar(6) == 'Y' &&
                                                    PeekAheadChar(7) == 'P' &&
                                                    PeekAheadChar(8) == 'E')
                                                {
                                                    return XmlMakeBeginDTDToken(leadingTrivia);
                                                }

                                                break;
                                        }
                                    }

                                    return XmlLessThanExclamationToken(state, leadingTrivia);
                                case '?':
                                    return XmlMakeBeginProcessingInstructionToken(leadingTrivia, _scanNoTriviaFunc);
                                case '/':
                                    return XmlMakeBeginEndElementToken(leadingTrivia, _scanNoTriviaFunc);
                            }
                        }

                        return XmlMakeLessToken(leadingTrivia);
                    case '?':
                        if (CanGetCharAtOffset(1) && PeekAheadChar(1) == '>')
                        {
                            return XmlMakeEndProcessingInstructionToken(leadingTrivia);
                        }

                        return XmlMakeBadToken(leadingTrivia, 1, ERRID.ERR_IllegalXmlNameChar);
                    case '(':
                        return XmlMakeLeftParenToken(leadingTrivia);
                    case ')':
                        return XmlMakeRightParenToken(leadingTrivia);
                    case '!':
                    case ';':
                    case '#':
                    case ',':
                    case '}':
                        return XmlMakeBadToken(leadingTrivia, 1, ERRID.ERR_IllegalXmlNameChar);
                    case ':':
                        return XmlMakeColonToken(leadingTrivia);
                    case '[':
                        return XmlMakeOpenBracketToken(state, leadingTrivia);
                    case ']':
                        return XmlMakeCloseBracketToken(state, leadingTrivia);
                    default:
                        return ScanXmlNcName(leadingTrivia);
                }
            }

            return EofToken(leadingTrivia);
        }

        private SyntaxToken.Green ScanXmlNcName(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            int Here = 0;
            bool IsIllegalChar = false;
            bool isFirst = true;
            ERRID err = ERRID.ERR_None;
            int errUnicode = 0;
            string errChar = null;

            // TODO - Fix ScanXmlNCName to conform to XML spec instead of old loose scanning.
            while (CanGetCharAtOffset(Here))
            {
                char c = PeekAheadChar(Here);
                switch (c)
                {
                    case ':':
                    case ' ':
                    case UCH_TAB:
                    case UCH_LF:
                    case UCH_CR:
                    case '=':
                    case '\'':
                    case '"':
                    case '/':
                    case '>':
                    case '<':
                    case '(':
                    case ')':
                    case '?':
                    case ';':
                    case ',':
                    case '}':
                        goto CreateNCNameToken;
                    default:
                        var xmlCh = ScanXmlChar(Here);
                        if (xmlCh.Length == 0)
                        {
                            IsIllegalChar = true;
                            goto CreateNCNameToken;
                        }
                        else
                        {
                            if (err == ERRID.ERR_None)
                            {
                                if (xmlCh.Length == 1)
                                {
                                    // Non surrogate check
                                    if (isFirst)
                                    {
                                        err = !IsStartNameChar(xmlCh.Char1) ? ERRID.ERR_IllegalXmlStartNameChar : ERRID.ERR_None;
                                        isFirst = false;
                                    }
                                    else
                                    {
                                        err = !IsNameChar(xmlCh.Char1) ? ERRID.ERR_IllegalXmlNameChar : ERRID.ERR_None;
                                    }

                                    if (err != ERRID.ERR_None)
                                    {
                                        errChar = Convert.ToString(xmlCh.Char1);
                                        errUnicode = Convert.ToInt32(xmlCh.Char1);
                                    }
                                }
                                else
                                {
                                    var unicode = UTF16ToUnicode(xmlCh);
                                    if (!(unicode >= 0x10000 && unicode <= 0xEFFFF))
                                    {
                                        err = ERRID.ERR_IllegalXmlNameChar;
                                        errChar = new string(new[]
                                        {
                                            xmlCh.Char1,
                                            xmlCh.Char2
                                        });
                                        errUnicode = unicode;
                                    }
                                }
                            }

                            Here += xmlCh.Length;
                        }

                        break;
                }
            }

        CreateNCNameToken:
            if (Here != 0)
            {
                var name = XmlMakeXmlNCNameToken(precedingTrivia.Node, Here);
                if (err != ERRID.ERR_None)
                {
                    name = name.WithDiagnostics(ErrorFactory.ErrorInfo(
                        err, errChar, string.Format("&H{0:X}", errUnicode)));
                }

                return name;
            }
            else if (IsIllegalChar)
            {
                return XmlMakeBadToken(precedingTrivia, 1, ERRID.ERR_IllegalChar);
            }

            return MissingToken(precedingTrivia, SyntaxKind.XmlNameToken);
        }

        private XmlNameTokenSyntax.Green XmlMakeXmlNCNameToken(GreenNode precedingTrivia, int tokenWidth)
        {
            Debug.Assert(tokenWidth > 0);
            var text = GetText(tokenWidth);
            var followingTrivia = ScanXmlWhitespace();
            // TODO: do something more efficient than create an intermediary string
            // for instance augment TextKeyedCache to work directly on the Buffer
            // instance instead of string or char[]
            var key = (precedingTrivia == null ? string.Empty : precedingTrivia.ToFullString())
                + text
                + (followingTrivia == null ? string.Empty : followingTrivia.ToFullString());
            var hashCode = Hash.GetFNVHashCode(key);
            var nameToken = _nameTokenCache.FindItem(key, 0, key.Length, hashCode);
            if (nameToken == null) {
                nameToken = new XmlNameTokenSyntax.Green(text, precedingTrivia, followingTrivia);
                _nameTokenCache.AddItem(key, 0, key.Length, hashCode, nameToken);
            }

            return nameToken;
        }

        public int UTF16ToUnicode(Scanner.XmlCharResult ch)
        {
            switch (ch.Length)
            {
                case 1:
                    return Convert.ToInt32(ch.Char1);
                case 2:
                    Debug.Assert(
                        Convert.ToInt32(ch.Char1) >= 0xD800 &&
                        Convert.ToInt32(ch.Char1) <= 0xDBFF &&
                        Convert.ToInt32(ch.Char2) >= 0xDC00 &&
                        Convert.ToInt32(ch.Char2) <= 0xDFFF);
                    return (
                        Convert.ToInt32(ch.Char1) - 0xD800) << 10 +
                        (Convert.ToInt32(ch.Char2) - 0xDC00) + 0x10000;
            }

            return 0;
        }

        public readonly struct XmlCharResult
        {
            public readonly int Length;
            public readonly char Char1;
            public readonly char Char2;

            public XmlCharResult(char ch)
            {
                Length = 1;
                Char1 = ch;
                Char2 = (char)0;
            }

            public XmlCharResult(char ch1, char ch2)
            {
                Length = 2;
                Char1 = ch1;
                Char2 = ch2;
            }

            public void AppendTo(StringBuilder list)
            {
                Debug.Assert(list != null);
                Debug.Assert(Length != 0);
                list.Append(Char1);
                if (Length == 2)
                {
                    list.Append(Char2);
                }
            }
        }

        public bool IsNameChar(char ch)
        {
            return XmlCharType.IsNameCharXml4e(ch);
        }

        public bool IsStartNameChar(char ch)
        {
            return XmlCharType.IsStartNameCharXml4e(ch);
        }

        private XmlCharResult ScanXmlChar(int Here)
        {
            Debug.Assert(Here >= 0);
            Debug.Assert(CanGetCharAtOffset(Here));
            var c = PeekAheadChar(Here);
            if (!XmlCharType.IsValidUtf16(c))
            {
                return default(XmlCharResult);
            }

            if (!char.IsSurrogate(c))
            {
                return new XmlCharResult(c);
            }

            return ScanSurrogatePair(c, Here);
        }

        /*  <summary>
        ''' 0 - not a surrogate, 2 - is valid surrogate
        ''' 1 is an error
        ''' </summary>
        */
        private XmlCharResult ScanSurrogatePair(char c1, int Here)
        {
            Debug.Assert(Here >= 0);
            Debug.Assert(CanGetCharAtOffset(Here));
            Debug.Assert(PeekAheadChar(Here) == c1);
            if (char.IsHighSurrogate(c1) && CanGetCharAtOffset(Here + 1))
            {
                var c2 = PeekAheadChar(Here + 1);
                if (char.IsLowSurrogate(c2))
                {
                    return new XmlCharResult(c1, c2);
                }
            }

            return default(XmlCharResult);
        }

        private static readonly PunctuationSyntax.Green _xmlColonToken = Punctuation(SyntaxKind.ColonToken, ":", null, null);
        private PunctuationSyntax.Green XmlMakeColonToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar();
            var followingTrivia = ScanXmlWhitespace();
            if (precedingTrivia.Node == null && followingTrivia == null)
                return _xmlColonToken;
            return Punctuation(SyntaxKind.ColonToken, ":", precedingTrivia, followingTrivia);
        }

        private BadTokenSyntax.Green XmlMakeOpenBracketToken(ScannerState state, InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            Debug.Assert(PeekChar() == '[');
            return XmlMakeBadToken(
                SyntaxSubKind.OpenBracketToken,
                precedingTrivia,
                1,
                state == ScannerState.DocType ?
                    ERRID.ERR_DTDNotSupported :
                    ERRID.ERR_IllegalXmlNameChar);
        }

        private BadTokenSyntax.Green XmlMakeCloseBracketToken(ScannerState state, InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            Debug.Assert(PeekChar() == ']');
            return XmlMakeBadToken(
                SyntaxSubKind.CloseBracketToken,
                precedingTrivia,
                1,
                state == ScannerState.DocType ?
                    ERRID.ERR_DTDNotSupported :
                    ERRID.ERR_IllegalXmlNameChar);
        }

        private PunctuationSyntax.Green XmlMakeLeftParenToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar();
            var followingTrivia = ScanXmlWhitespace();
            return Punctuation(SyntaxKind.OpenParenToken, "(", precedingTrivia, followingTrivia);
        }

        private PunctuationSyntax.Green XmlMakeRightParenToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar();
            var followingTrivia = ScanXmlWhitespace();
            return Punctuation(SyntaxKind.CloseParenToken, ")", precedingTrivia, followingTrivia);
        }

        private BadTokenSyntax.Green XmlMakeBadToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia, int length, ERRID id)
        {
            return XmlMakeBadToken(SyntaxSubKind.None, precedingTrivia, length, id);
        }

        private PunctuationSyntax.Green XmlMakeEndProcessingInstructionToken(
            InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            Debug.Assert(PeekChar() == '?');
            Debug.Assert(PeekAheadChar(1) == '>');
            AdvanceChar(2);
            return Punctuation(
                SyntaxKind.QuestionGreaterThanToken,
                "?>",
                precedingTrivia,
                null);
        }

        private PunctuationSyntax.Green XmlMakeBeginEndElementToken(
            InternalSyntax.SyntaxList<GreenNode> precedingTrivia,
            Func<InternalSyntax.SyntaxList<GreenNode>> scanTrailingTrivia)
        {
            Debug.Assert(PeekChar() == '<');
            Debug.Assert(PeekAheadChar(1) == '/');
            AdvanceChar(2);
            var followingTrivia = scanTrailingTrivia();
            return Punctuation(
                SyntaxKind.LessThanSlashToken,
                "</",
                precedingTrivia,
                followingTrivia);
        }

        private SyntaxToken.Green XmlLessThanExclamationToken(
            ScannerState state,
            InternalSyntax.SyntaxList<GreenNode> leadingTrivia)
        {
            Debug.Assert(PeekChar() == '<');
            Debug.Assert(PeekAheadChar(1) == '!');
            return XmlMakeBadToken(
                SyntaxSubKind.LessThanExclamationToken,
                leadingTrivia,
                2,
                state == ScannerState.DocType ?
                    ERRID.ERR_DTDNotSupported :
                    ERRID.ERR_Syntax);
        }

        private BadTokenSyntax.Green XmlMakeBadToken(SyntaxSubKind subkind, InternalSyntax.SyntaxList<GreenNode> precedingTrivia, int length, ERRID id)
        {
            var spelling = GetText(length);
            var followingTrivia = ScanXmlWhitespace();
            var result1 = BadToken(subkind, spelling, precedingTrivia.Node, followingTrivia);
            DiagnosticInfo diagnostic;
            switch (id)
            {
                case ERRID.ERR_IllegalXmlStartNameChar:
                case ERRID.ERR_IllegalXmlNameChar:
                    Debug.Assert(length == 1);
                    if (id == ERRID.ERR_IllegalXmlNameChar && (precedingTrivia.Any() || PrevToken == null || PrevToken.HasTrailingTrivia || PrevToken.Kind == SyntaxKind.LessThanToken || PrevToken.Kind == SyntaxKind.LessThanSlashToken || PrevToken.Kind == SyntaxKind.LessThanQuestionToken))
                    {
                        id = ERRID.ERR_IllegalXmlStartNameChar;
                    }

                    var xmlCh = spelling[0];
                    var xmlChAsUnicode = UTF16ToUnicode(new XmlCharResult(xmlCh));
                    diagnostic = ErrorFactory.ErrorInfo(id, xmlCh, string.Format("&H{0:X}", xmlChAsUnicode));
                    break;
                default:
                    diagnostic = ErrorFactory.ErrorInfo(id);
                    break;
            }

            var errResult1 = ((BadTokenSyntax.Green)result1.SetDiagnostics(new[]
            {
                diagnostic
            }));

            Debug.Assert(errResult1 != null);
            return errResult1;
        }

        private SyntaxToken.Green XmlMakeBeginCDataToken(
            InternalSyntax.SyntaxList<GreenNode> leadingTrivia,
            Func<InternalSyntax.SyntaxList<GreenNode>> scanTrailingTrivia)
        {
            Debug.Assert(PeekChar() == '<');
            Debug.Assert(PeekAheadChar(1) == '!');
            Debug.Assert(PeekAheadChar(2) == '[');
            Debug.Assert(PeekAheadChar(3) == 'C');
            Debug.Assert(PeekAheadChar(4) == 'D');
            Debug.Assert(PeekAheadChar(5) == 'A');
            Debug.Assert(PeekAheadChar(6) == 'T');
            Debug.Assert(PeekAheadChar(7) == 'A');
            Debug.Assert(PeekAheadChar(8) == '[');
            AdvanceChar(9);
            var followingTrivia = scanTrailingTrivia();
            return Punctuation(
                SyntaxKind.BeginCDataToken,
                "<![CDATA[",
                leadingTrivia,
                followingTrivia);
        }

        private static readonly PunctuationSyntax.Green _xmlDoubleQuoteToken = Punctuation(SyntaxKind.DoubleQuoteToken, "\"", null, null);
        private SyntaxToken.Green XmlMakeDoubleQuoteToken(
            InternalSyntax.SyntaxList<GreenNode> leadingTrivia,
            char spelling,
            bool isOpening)
        {
            Debug.Assert(PeekChar() == spelling);

            AdvanceChar();

            GreenNode followingTrivia = null;
            if (!isOpening)
            {
                var ws = ScanXmlWhitespace();
                followingTrivia = ws;
            }

            if (leadingTrivia.Node == null && followingTrivia == null)
                return _xmlDoubleQuoteToken;

            return Punctuation(
                SyntaxKind.DoubleQuoteToken,
                Intern(spelling),
                leadingTrivia,
                followingTrivia);
        }

        private SyntaxToken.Green XmlMakeSingleQuoteToken(
            InternalSyntax.SyntaxList<GreenNode> leadingTrivia,
            char spelling,
            bool isOpening)
        {
            Debug.Assert(PeekChar() == spelling);

            AdvanceChar();

            GreenNode followingTrivia = null;
            if (!isOpening)
            {
                var ws = ScanXmlWhitespace();
                followingTrivia = ws;
            }

            return Punctuation(
                SyntaxKind.SingleQuoteToken,
                Intern(spelling),
                leadingTrivia,
                new InternalSyntax.SyntaxList<GreenNode>(followingTrivia));
        }

        private string Intern(char spelling)
        {
            return _stringTable.Add(spelling);
        }

        private string Intern(char[] spelling)
        {
            return _stringTable.Add(spelling, 0, spelling.Length);
        }

        private static readonly PunctuationSyntax.Green _xmlEqualsToken = Punctuation(SyntaxKind.EqualsToken, "=", null, null);
        private SyntaxToken.Green XmlMakeEqualsToken(InternalSyntax.SyntaxList<GreenNode> leadingTrivia)
        {
            AdvanceChar();
            var followingTrivia = ScanXmlWhitespace();
            if (leadingTrivia.Node == null && followingTrivia == null)
                return _xmlEqualsToken;
            return Punctuation(SyntaxKind.EqualsToken, Intern('='), leadingTrivia, followingTrivia);
        }

        private SyntaxToken.Green XmlMakeGreaterToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar();

            // Note: > does not consume following trivia
            return Punctuation(SyntaxKind.GreaterThanToken, ">", precedingTrivia, null);
        }

        private SyntaxToken.Green XmlMakeEndEmptyElementToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar(2);

            return Punctuation(SyntaxKind.SlashGreaterThanToken, "/>", precedingTrivia, null);
        }

        private PunctuationSyntax.Green XmlMakeDivToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar();
            var followingTrivia = ScanXmlWhitespace();
            return Punctuation(SyntaxKind.SlashToken, "/", precedingTrivia, followingTrivia);
        }

        private XmlTextTokenSyntax.Green XmlMakeTextLiteralToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia, int TokenWidth, StringBuilder Scratch)
        {
            Debug.Assert(TokenWidth > 0);
            var text = GetText(TokenWidth);
            var value = _stringTable.Add(Scratch);
            Scratch.Clear();
            return XmlTextToken(text, precedingTrivia.Node, null);
        }

        internal SyntaxToken.Green ScanXmlContent()
        {
            int Here = 0;
            bool IsAllWhitespace = true;
            // lets do a funky peek-behind to make sure we are not restarting after a non-Ws char.
            if (_lineBufferOffset > 0)
            {
                var prevChar = PeekAheadChar(-1);
                if (prevChar != '>' && !XmlCharType.IsWhiteSpace(prevChar))
                {
                    IsAllWhitespace = false;
                }
            }

            var scratch = GetScratch();
            while (CanGetCharAtOffset(Here))
            {
                char c = PeekAheadChar(Here);
                switch (c)
                {
                    case UCH_CR:
                    case UCH_LF:
                        Here = SkipLineBreak(c, Here);
                        scratch.Append(UCH_LF);
                        break;
                    case ' ':
                    case UCH_TAB:
                        scratch.Append(c);
                        Here += 1;
                        break;
                    case '&':
                        if (Here != 0)
                        {
                            return XmlMakeTextLiteralToken(null, Here, scratch);
                        }

                        return ScanXmlReference(null);
                    case '<':
                        InternalSyntax.SyntaxList<GreenNode> precedingTrivia = null;
                        if (Here != 0)
                        {
                            if (!IsAllWhitespace)
                            {
                                return XmlMakeTextLiteralToken(null, Here, scratch);
                            }
                            else
                            {
                                scratch.Clear(); // will not use this
                                Here = 0; // consumed chars.
                                precedingTrivia = ScanXmlTrivia(PeekChar());
                            }
                        }

                        Debug.Assert(Here == 0);
                        if (CanGetCharAtOffset(1))
                        {
                            char ch = PeekAheadChar(1);
                            switch (ch)
                            {
                                case '!':
                                    if (CanGetCharAtOffset(2))
                                    {
                                        switch ((PeekAheadChar(2)))
                                        {
                                            case '-':
                                                if (CanGetCharAtOffset(3) && PeekAheadChar(3) == '-')
                                                {
                                                    return XmlMakeBeginCommentToken(precedingTrivia, _scanNoTriviaFunc);
                                                }

                                                break;
                                            case '[':
                                                if (CanGetCharAtOffset(8) &&
                                                    PeekAheadChar(3) == 'C' &&
                                                    PeekAheadChar(4) == 'D' &&
                                                    PeekAheadChar(5) == 'A' &&
                                                    PeekAheadChar(6) == 'T' &&
                                                    PeekAheadChar(7) == 'A' &&
                                                    PeekAheadChar(8) == '[')
                                                {
                                                    return XmlMakeBeginCDataToken(precedingTrivia, _scanNoTriviaFunc);
                                                }

                                                break;
                                            case 'D':
                                                if (CanGetCharAtOffset(8) &&
                                                    PeekAheadChar(3) == 'O' &&
                                                    PeekAheadChar(4) == 'C' &&
                                                    PeekAheadChar(5) == 'T' &&
                                                    PeekAheadChar(6) == 'Y' &&
                                                    PeekAheadChar(7) == 'P' &&
                                                    PeekAheadChar(8) == 'E')
                                                {
                                                    return XmlMakeBeginDTDToken(precedingTrivia);
                                                }

                                                break;
                                        }
                                    }

                                    break;
                                case '?':
                                    return XmlMakeBeginProcessingInstructionToken(precedingTrivia, _scanNoTriviaFunc);
                                case '/':
                                    return XmlMakeBeginEndElementToken(precedingTrivia, _scanNoTriviaFunc);
                            }
                        }

                        return XmlMakeLessToken(precedingTrivia);
                    case ']':
                        if (CanGetCharAtOffset(Here + 2) && PeekAheadChar(Here + 1) == ']' && PeekAheadChar(Here + 2) == '>')
                        {
                            // // If valid characters found then return them.
                            if (Here != 0)
                            {
                                return XmlMakeTextLiteralToken(null, Here, scratch);
                            }

                            return XmlMakeTextLiteralToken(null, 3, ERRID.ERR_XmlEndCDataNotAllowedInContent);
                        }

                        goto ScanChars;
                    case '#':
                        // // Even though # is valid in content, abort xml scanning if the m_State shows and error
                        // // and the line begins with NL WS* # WS* KW
                        //TODO: error recovery - how can we do ths?
                        //If m_State.m_IsXmlError Then
                        //    MakeXmlCharToken(tokens.tkXmlCharData, Here - m_InputStreamPosition, IsAllWhitespace)
                        //    m_InputStreamPosition = Here
                        //    Dim sharp As Token = MakeToken(tokens.tkSharp, 1)
                        //    m_InputStreamPosition += 1
                        //    While (m_InputStream(m_InputStreamPosition) = " "c OrElse m_InputStream(m_InputStreamPosition) = UCH_TAB)
                        //        m_InputStreamPosition += 1
                        //    End While
                        //    ScanXmlQName()
                        //    Dim restart As Token = CheckXmlForStatement()
                        //    If restart IsNot Nothing Then
                        //        ' // Abort Xml - Found Keyword space at the beginning of the line
                        //        AbandonTokens(restart)
                        //        m_State.Init(LexicalState.VB)
                        //        MakeToken(tokens.tkXmlAbort, 0)
                        //        Return
                        //    End If
                        //    AbandonTokens(sharp)
                        //    Here = m_InputStreamPosition
                        //End If
                        goto ScanChars;
                    case '%':
                        //TODO: error recovery. We cannot do this.
                        //If there is all whitespace after ">", it will be scanned as insignificant,
                        //but in this case it is significant.
                        //Also as far as I can see Dev10 does not resync on "%>" text anyways.
                        //' // Even though %> is valid in pcdata.  When inside of an embedded expression
                        //' // return this sequence separately so that the xml literal completion code can
                        //' // easily detect the end of an embedded expression that may be temporarily hidden
                        //' // by a new element.  i.e. <%= <a> %>
                        //If CanGetCharAtOffset(Here + 1) AndAlso _
                        //   PeekAheadChar(Here + 1) = ">"c Then
                        //    ' // If valid characters found then return them.
                        //    If Here <> 0 Then
                        //        Return XmlMakeCharDataToken(Nothing, Here, New String(value.ToArray))
                        //    End If
                        //    ' // Create a special pcdata token for the possible tkEndXmlEmbedded
                        //    Return XmlMakeCharDataToken(Nothing, 2, "%>")
                        //Else
                        //    IsAllWhitespace = False
                        //    value.Add("%"c)
                        //    Here += 1
                        //End If
                        //Continue While
                        goto ScanChars;
                    default:
                    ScanChars:
                        ;
                        // // Check characters are valid
                        IsAllWhitespace = false;
                        var xmlCh = ScanXmlChar(Here);
                        if (xmlCh.Length == 0)
                        {
                            // bad char
                            if (Here > 0)
                            {
                                return XmlMakeTextLiteralToken(null, Here, scratch);
                            }
                            else
                            {
                                return XmlMakeBadToken(null, 1, ERRID.ERR_IllegalChar);
                            }
                        }

                        xmlCh.AppendTo(scratch);
                        Here += xmlCh.Length;
                        break;
                }
            }

            // no more chars
            if (Here > 0)
            {
                return XmlMakeTextLiteralToken(null, Here, scratch);
            }
            else
            {
                return Eof;
            }
        }

        private SyntaxToken.Green XmlMakeTextLiteralToken(
            InternalSyntax.SyntaxList<GreenNode> leadingTrivia,
            int tokenWidth,
            ERRID eRR_XmlEndCDataNotAllowedInContent)
        {
            var text = GetText(tokenWidth);
            return (SyntaxToken.Green)new XmlTextTokenSyntax.Green(text, leadingTrivia.Node, null)
                                                            .SetDiagnostic(ErrorFactory.ErrorInfo(eRR_XmlEndCDataNotAllowedInContent));
        }

        private XmlTextTokenSyntax.Green ScanXmlReference(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            Debug.Assert(CanGetChar());
            Debug.Assert(PeekChar() == '&');
            // skip 1 char for "&"
            if (CanGetCharAtOffset(1))
            {
                char c = PeekAheadChar(1);
                switch (c)
                {
                    case '#':
                        var Here = 2;
                        var result = ScanXmlCharRef(ref Here);
                        if (result.Length != 0)
                        {
                            string value = null;
                            if (result.Length == 1)
                            {
                                value = Intern(result.Char1);
                            }
                            else if (result.Length == 2)
                            {
                                value = Intern(new[]
                                {
                                    result.Char1, result.Char2
                                });
                            }

                            if (CanGetCharAtOffset(Here) && PeekAheadChar(Here) == ';')
                            {
                                return XmlMakeEntityLiteralToken(precedingTrivia, Here + 1, value);
                            }
                            else
                            {
                                var noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, Here, value);
                                var noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon);
                                return ((XmlTextTokenSyntax.Green)noSemicolon.SetDiagnostics(new[]
                                {
                                    noSemicolonError
                                }));
                            }
                        }

                        break;
                    case 'a':
                        // // &amp;
                        // // &apos;
                        if (CanGetCharAtOffset(4) && PeekAheadChar(2) == 'm' && PeekAheadChar(3) == 'p')
                        {
                            if (PeekAheadChar(4) == ';')
                            {
                                return XmlMakeAmpLiteralToken(precedingTrivia);
                            }
                            else
                            {
                                var noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 4, "&");
                                var noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon);
                                return ((XmlTextTokenSyntax.Green)noSemicolon.SetDiagnostics(new[]
                                {
                                noSemicolonError
                                }));
                            }
                        }
                        else if (CanGetCharAtOffset(5) && PeekAheadChar(2) == 'p' && PeekAheadChar(3) == 'o' && PeekAheadChar(4) == 's')
                        {
                            if (PeekAheadChar(5) == ';')
                            {
                                return XmlMakeAposLiteralToken(precedingTrivia);
                            }
                            else
                            {
                                var noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 5, "'");
                                var noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon);
                                return ((XmlTextTokenSyntax.Green)noSemicolon.SetDiagnostics(new[]
                                {
                                    noSemicolonError
                                }));
                            }
                        }

                        break;
                    case 'l':
                        // // &lt;
                        if (CanGetCharAtOffset(3) && PeekAheadChar(2) == 't')
                        {
                            if (PeekAheadChar(3) == ';')
                            {
                                return XmlMakeLtLiteralToken(precedingTrivia);
                            }
                            else
                            {
                                var noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 3, "<");
                                var noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon);
                                return ((XmlTextTokenSyntax.Green)noSemicolon.SetDiagnostics(new[]
                                {
                                    noSemicolonError
                                }));
                            }
                        }

                        break;
                    case 'g':
                        // // &gt;
                        if (CanGetCharAtOffset(3) && PeekAheadChar(2) == 't')
                        {
                            if (PeekAheadChar(3) == ';')
                            {
                                return XmlMakeGtLiteralToken(precedingTrivia);
                            }
                            else
                            {
                                var noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 3, ">");
                                var noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon);
                                return ((XmlTextTokenSyntax.Green)noSemicolon.SetDiagnostics(new[]
                                {
                                    noSemicolonError
                                }));
                            }
                        }

                        break;
                    case 'q':
                        // // &quot;
                        if (CanGetCharAtOffset(5) &&
                            PeekAheadChar(2) == 'u' &&
                            PeekAheadChar(3) == 'o' &&
                            PeekAheadChar(4) == 't')
                        {
                            if (PeekAheadChar(5) == ';')
                            {
                                return XmlMakeQuotLiteralToken(precedingTrivia);
                            }
                            else
                            {
                                var noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 5, "\"");
                                var noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon);
                                return ((XmlTextTokenSyntax.Green)noSemicolon.SetDiagnostics(new[]
                                {
                                    noSemicolonError
                                }));
                            }
                        }

                        break;
                }
            }

            var badEntity = XmlMakeEntityLiteralToken(precedingTrivia, 1, "");
            var errInfo = ErrorFactory.ErrorInfo(ERRID.ERR_XmlEntityReference);
            return ((XmlTextTokenSyntax.Green)badEntity.SetDiagnostics(new[]
            {
                errInfo
            }));
        }

        private static readonly XmlEntityTokenSyntax.Green _xmlAmpToken = XmlEntityToken("&amp;", "&", null, null);
        private XmlEntityTokenSyntax.Green XmlMakeAmpLiteralToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar(5); // "&amp;".Length
            return precedingTrivia.Node == null ? _xmlAmpToken : XmlEntityToken("&amp;", "&", precedingTrivia.Node, null);
        }

        private static readonly XmlEntityTokenSyntax.Green _xmlAposToken = XmlEntityToken("&apos;", "'", null, null);
        private XmlEntityTokenSyntax.Green XmlMakeAposLiteralToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar(6); // "&apos;".Length
            return precedingTrivia.Node == null ? _xmlAposToken : XmlEntityToken("&apos;", "'", precedingTrivia.Node, null);
        }

        private static readonly XmlEntityTokenSyntax.Green _xmlGtToken = XmlEntityToken("&gt;", ">", null, null);
        private XmlEntityTokenSyntax.Green XmlMakeGtLiteralToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar(4); // "&gt;".Length
            return precedingTrivia.Node == null ? _xmlGtToken : XmlEntityToken("&gt;", ">", precedingTrivia.Node, null);
        }

        private static readonly XmlEntityTokenSyntax.Green _xmlLtToken = XmlEntityToken("&lt;", "<", null, null);
        private XmlEntityTokenSyntax.Green XmlMakeLtLiteralToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar(4); // "&lt;".Length
            return precedingTrivia.Node == null ? _xmlLtToken : XmlEntityToken("&lt;", "<", precedingTrivia.Node, null);
        }

        private static readonly XmlEntityTokenSyntax.Green _xmlQuotToken = XmlEntityToken("&quot;", "\"", null, null);
        private XmlEntityTokenSyntax.Green XmlMakeQuotLiteralToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar(6); // "&quot;".Length
            return precedingTrivia.Node == null ? _xmlQuotToken : XmlEntityToken("&quot;", "\"", precedingTrivia.Node, null);
        }

        private XmlEntityTokenSyntax.Green XmlMakeEntityLiteralToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia, int tokenWidth, string value)
        {
            return XmlEntityToken(GetText(tokenWidth), value, precedingTrivia.Node, null);
        }

        private XmlCharResult ScanXmlCharRef(ref int index)
        {
            Debug.Assert(index >= 0);
            if (!CanGetCharAtOffset(index))
            {
                return default(XmlCharResult);
            }

            var charRefSb = new StringBuilder();
            var Here = index;
            var ch = PeekAheadChar(Here);
            if (ch == 'x')
            {
                Here += 1;
                while (CanGetCharAtOffset(Here))
                {
                    ch = PeekAheadChar(Here);
                    if (XmlCharType.IsHexDigit(ch))
                    {
                        charRefSb.Append(ch);
                    }
                    else
                    {
                        break;
                    }

                    Here += 1;
                }

                if (charRefSb.Length > 0)
                {
                    var result = HexToUTF16(charRefSb);
                    if (result.Length != 0)
                    {
                        index = Here;
                    }

                    return result;
                }
            }
            else
            {
                while (CanGetCharAtOffset(Here))
                {
                    ch = PeekAheadChar(Here);
                    if (XmlCharType.IsDigit(ch))
                    {
                        charRefSb.Append(ch);
                    }
                    else
                    {
                        break;
                    }

                    Here += 1;
                }

                if (charRefSb.Length > 0)
                {
                    var result = DecToUTF16(charRefSb);
                    if (result.Length != 0)
                    {
                        index = Here;
                    }

                    return result;
                }
            }

            return default(XmlCharResult);
        }

        internal Scanner.XmlCharResult HexToUTF16(StringBuilder pwcText)
        {
            Debug.Assert(pwcText != null);
            uint ulCode = 0;
            if (TryHexToUnicode(pwcText, ref ulCode))
            {
                if (ValidateXmlChar(ulCode))
                {
                    return UnicodeToUTF16(ulCode);
                }
            }

            return default(XmlCharResult);
        }

        internal bool TryHexToUnicode(StringBuilder pwcText, ref uint pulCode)
        {
            Debug.Assert(pwcText != null);
            uint ulCode = 0;
            char wch;
            var n = pwcText.Length - 1;
            for (var i = 0; i <= n; i++)
            {
                wch = pwcText[i];
                if (XmlCharType.InRange(wch, '0', '9'))
                {
                    ulCode = (ulCode * 16) + ((uint)(wch)) - ((uint)('0'));
                }
                else if (XmlCharType.InRange(wch, 'a', 'f'))
                {
                    ulCode = (ulCode * 16) + 10 + ((uint)(wch)) - ((uint)('a'));
                }
                else if (XmlCharType.InRange(wch, 'A', 'F'))
                {
                    ulCode = (ulCode * 16) + 10 + ((uint)(wch)) - ((uint)('A'));
                }
                else
                {
                    return false;
                }

                if (ulCode > 0x10FFFF)
                {
                    return false;
                }
            }

            pulCode = ((uint)ulCode);
            return true;
        }

        internal Scanner.XmlCharResult DecToUTF16(StringBuilder pwcText)
        {
            Debug.Assert(pwcText != null);
            ushort ulCode = 0;
            if (TryDecToUnicode(pwcText, ref ulCode))
            {
                if (ValidateXmlChar(ulCode))
                {
                    return UnicodeToUTF16(ulCode);
                }
            }

            return default(XmlCharResult);
        }

        private Scanner.XmlCharResult UnicodeToUTF16(uint ulCode)
        {
            if (ulCode > 0xFFFF)
            {
                return new Scanner.XmlCharResult(Convert.ToChar(0xD7C0 + (ulCode >> 10)), Convert.ToChar(0xDC00 | (ulCode & 0x3FF)));
            }
            else
            {
                return new Scanner.XmlCharResult(Convert.ToChar(ulCode));
            }
        }

        private bool ValidateXmlChar(uint ulCode)
        {
            if ((ulCode < 0xD800 && (ulCode > 0x1F || XmlCharType.IsWhiteSpace(Convert.ToChar(ulCode)))) || (ulCode < 0xFFFE && ulCode > 0xDFFF) || (ulCode < 0x110000 && ulCode > 0xFFFF))
            {
                return true;
            }

            return false;
        }

        internal bool TryDecToUnicode(StringBuilder pwcText, ref ushort pulCode)
        {
            Debug.Assert(pwcText != null);
            int ulCode = 0;
            char wch;
            var n = pwcText.Length - 1;
            for (var i = 0; i <= n; i++)
            {
                wch = pwcText[i];
                if (XmlCharType.InRange(wch, '0', '9'))
                {
                    ulCode = (ulCode * 10) + (int)(wch) - (int)('0');
                }
                else
                {
                    return false;
                }

                if (ulCode > 0x10FFFF)
                {
                    return false;
                }
            }

            pulCode = ((ushort)ulCode);
            return true;
        }

        private int SkipLineBreak(char StartCharacter, int index)
        {
            return index + LengthOfLineBreak(StartCharacter, index);
        }

        private int LengthOfLineBreak(char StartCharacter, int here = 0)
        {
            Debug.Assert(CanGetCharAtOffset(here));
            Debug.Assert(IsNewLine(StartCharacter));
            Debug.Assert(StartCharacter == PeekAheadChar(here));
            if (StartCharacter == UCH_CR && CanGetCharAtOffset(here + 1) && PeekAheadChar(here + 1) == UCH_LF)
            {
                return 2;
            }

            return 1;
        }

        private static readonly Func<InternalSyntax.SyntaxList<GreenNode>> _scanNoTriviaFunc = () => default(InternalSyntax.SyntaxList<GreenNode>);

        private SyntaxToken.Green ScanXmlMisc()
        {
            InternalSyntax.SyntaxList<GreenNode> precedingTrivia = null;
            while (CanGetChar())
            {
                char c = PeekChar();
                switch (c)
                {
                    // // Whitespace
                    // //  S    ::=    (#x20 | #x9 | #xD | #xA)+
                    case UCH_CR:
                    case UCH_LF:
                    case ' ':
                    case UCH_TAB:
                        // we should not visit this place twice
                        precedingTrivia = ScanXmlTrivia(c);
                        break;
                    case '<':
                        if (CanGetCharAtOffset(1))
                        {
                            char ch = PeekAheadChar(1);
                            switch (ch)
                            {
                                case '!':
                                    if (CanGetCharAtOffset(3) && PeekAheadChar(2) == '-' && PeekAheadChar(3) == '-')
                                    {
                                        return XmlMakeBeginCommentToken(precedingTrivia, _scanNoTriviaFunc);
                                    }
                                    else if (CanGetCharAtOffset(8) &&
                                        PeekAheadChar(2) == 'D' &&
                                        PeekAheadChar(3) == 'O' &&
                                        PeekAheadChar(4) == 'C' &&
                                        PeekAheadChar(5) == 'T' &&
                                        PeekAheadChar(6) == 'Y' &&
                                        PeekAheadChar(7) == 'P' &&
                                        PeekAheadChar(8) == 'E')
                                    {
                                        return XmlMakeBeginDTDToken(precedingTrivia);
                                    }

                                    break;
                                case '?':
                                    return XmlMakeBeginProcessingInstructionToken(precedingTrivia, _scanNoTriviaFunc);
                            }
                        }

                        return XmlMakeLessToken(precedingTrivia);
                    default:
                        return EndOfXml(precedingTrivia);
                }
            }

            return EofToken(precedingTrivia);
        }

        private SyntaxToken.Green XmlMakeLessToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            AdvanceChar();

            var followingTrivia = ScanXmlWhitespace();
            return Punctuation(SyntaxKind.LessThanToken, "<", precedingTrivia, followingTrivia);
        }

        private GreenNode ScanXmlWhitespace()
        {
            int length = GetXmlWhitespaceLength();
            if (length > 0)
            {
                return MakeWhiteSpaceTrivia(GetText(length));
            }

            return null;
        }

        private int GetXmlWhitespaceLength()
        {
            int length = 0;
            while (CanGetCharAtOffset(length) && IsXmlWhitespace(PeekAheadChar(length)))
            {
                length++;
            }

            return length;
        }

        private bool IsXmlWhitespace(char ch)
        {
            return
                ch == UCH_SPACE ||
                ch == UCH_TAB ||
                ch > 128 && XmlCharType.IsWhiteSpace(ch);
        }

        internal GreenNode MakeWhiteSpaceTrivia(string text)
        {
            Debug.Assert(text.Length > 0);
            Debug.Assert(text.All(IsWhitespace));
            var hashCode = Hash.GetFNVHashCode(text);
            var ws = _triviaCache.FindItem(text, 0, text.Length, hashCode);
            if (ws == null) {
                ws = new SyntaxTrivia.Green(SyntaxKind.WhitespaceTrivia, text);
                _triviaCache.AddItem(text, 0, text.Length, hashCode, ws);
            }
            return ws;
        }

        public static bool IsWhitespace(char c)
        {
            return (UCH_SPACE == c) || (UCH_TAB == c) || (c > (char)128 && char.IsWhiteSpace(c));
        }

        private string GetText(int length)
        {
            if (length == 1)
            {
                return GetNextChar();
            }

            string str;

            if (length <= _internBuffer.Length)
            {
                this.buffer.CopyTo(_lineBufferOffset, _internBuffer, 0, length);
                str = _stringTable.Add(_internBuffer, 0, length);
            }
            else
            {
                str = this.buffer.GetText(_lineBufferOffset, length);
            }
            AdvanceChar(length);
            return str;
        }

        private void AdvanceChar(int delta = 1)
        {
            _lineBufferOffset += delta;
        }

        private string GetNextChar()
        {
            var ch = GetChar();
            _lineBufferOffset += 1;
            return ch;
        }

        private string GetChar()
        {
            return Intern(PeekChar());
        }

        private PunctuationSyntax.Green XmlMakeBeginProcessingInstructionToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia, Func<InternalSyntax.SyntaxList<GreenNode>> scanTrailingTrivia)
        {
            Debug.Assert(PeekChar() == '<');
            Debug.Assert(PeekAheadChar(1) == '?');
            AdvanceChar(2);
            var followingTrivia = scanTrailingTrivia();
            return Punctuation(SyntaxKind.LessThanQuestionToken, "<?", precedingTrivia, followingTrivia);
        }

        private SyntaxToken.Green XmlMakeBeginDTDToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia)
        {
            return XmlMakeBadToken(SyntaxSubKind.BeginDocTypeToken, precedingTrivia, 9, ERRID.ERR_DTDNotSupported);
        }

        private PunctuationSyntax.Green XmlMakeBeginCommentToken(InternalSyntax.SyntaxList<GreenNode> precedingTrivia, Func<InternalSyntax.SyntaxList<GreenNode>> scanTrailingTrivia)
        {
            Debug.Assert(PeekChar() == '<');
            Debug.Assert(PeekAheadChar(1) == '!');
            Debug.Assert(PeekAheadChar(2) == '-');
            Debug.Assert(PeekAheadChar(3) == '-');
            AdvanceChar(4);
            var followingTrivia = scanTrailingTrivia();
            return Punctuation(SyntaxKind.LessThanExclamationMinusMinusToken, "<!--", precedingTrivia, followingTrivia);
        }

        private char PeekAheadChar(int offset)
        {
            return buffer[_lineBufferOffset + offset];
        }

        private bool CanGetCharAtOffset(int offset)
        {
            return _lineBufferOffset + offset < _bufferLen;
        }

        private InternalSyntax.SyntaxList<GreenNode> ScanXmlTrivia(char c)
        {
            Debug.Assert(c == UCH_CR || c == UCH_LF || c == ' ' || c == UCH_TAB);
            var builder = triviaListPool.Allocate();
            var len = 0;
            bool exitWhile = false;
            while (!exitWhile)
            {
                if (c == ' ' || c == UCH_TAB)
                {
                    len += 1;
                }
                else if (c == UCH_CR || c == UCH_LF)
                {
                    if (len > 0)
                    {
                        builder.Add(MakeWhiteSpaceTrivia(GetText(len)));
                        len = 0;
                    }

                    builder.Add(ScanNewlineAsTrivia(c));
                }
                else
                {
                    exitWhile = true;
                    continue;
                }

                if (!CanGetCharAtOffset(len))
                {
                    exitWhile = true;
                    continue;
                }

                c = PeekAheadChar(len);
            }

            if (len > 0)
            {
                builder.Add(MakeWhiteSpaceTrivia(GetText(len)));
                len = 0;
            }

            Debug.Assert(builder.Count > 0);
            var result = builder.ToList();
            triviaListPool.Free(builder);
            return result;
        }

        /*  <summary>
        ''' Accept a CR/LF pair or either in isolation as a newline.
        ''' Make it a whitespace
        ''' </summary>
        */
        private SyntaxTrivia.Green ScanNewlineAsTrivia(char startCharacter)
        {
            if (LengthOfLineBreak(startCharacter) == 2)
            {
                return MakeEndOfLineTriviaCRLF();
            }

            return EndOfLine(GetNextChar());
        }

        private SyntaxTrivia.Green MakeEndOfLineTriviaCRLF()
        {
            AdvanceChar(2);
            return CrLfEndOfLine;
        }

        private char PeekChar()
        {
            return buffer[_lineBufferOffset];
        }

        private bool CanGetChar()
        {
            return _lineBufferOffset < _bufferLen;
        }

        protected readonly struct ScannerToken
        {
            internal ScannerToken(int lineBufferOffset, int endOfTerminatorTrivia, SyntaxToken.Green token, ScannerState state)
            {
                this.Position = lineBufferOffset;
                this.EndOfTerminatorTrivia = endOfTerminatorTrivia;
                this.InnerTokenObject = token;
                this.State = state;
            }

            internal ScannerToken With(ScannerState state, SyntaxToken.Green token)
            {
                return new ScannerToken(this.Position, this.EndOfTerminatorTrivia, token, state);
            }

            internal readonly SyntaxToken.Green InnerTokenObject;
            internal readonly int Position;
            internal readonly int EndOfTerminatorTrivia;
            internal readonly ScannerState State;

            public override string ToString()
            {
                return $"{Position} {State} {InnerTokenObject}";
            }
        }

        internal static bool IsNewLine(char c)
        {
            return UCH_CR == c || UCH_LF == c || (c >= UCH_NEL && (UCH_NEL == c || UCH_LS == c || UCH_PS == c));
        }

        public const char UCH_NULL = (char)0x00;
        public const char UCH_TAB = (char)0x09;
        public const char UCH_LF = (char)0x0A;
        public const char UCH_CR = (char)0x0D;
        public const char UCH_SPACE = (char)(0x20);
        public const char UCH_NBSP = (char)(0xA0);
        public const char UCH_IDEOSP = (char)(0x3000);
        public const char UCH_LS = (char)(0x2028);
        public const char UCH_PS = (char)(0x2029);
        public const char UCH_NEL = (char)(0x85);
        public const char DWCH_SQ = (char)(0xFF07);
        public const char DWCH_LSMART_Q = (char)(0x2018);
        public const char DWCH_RSMART_Q = (char)(0x2019);
        public const char DWCH_LSMART_DQ = (char)(0x201C);
        public const char DWCH_RSMART_DQ = (char)(0x201D);
        public const char DWCH_DQ = (char)(('"') + (0xFF00 - 0x20));
        public const char FULLWIDTH_0 = (char)(('0') + (0xFF00 - 0x20));
        public const char FULLWIDTH_7 = (char)(('7') + (0xFF00 - 0x20));
        public const char FULLWIDTH_9 = (char)(('9') + (0xFF00 - 0x20));
        public const char FULLWIDTH_LC = (char)(('_') + (0xFF00 - 0x20));
        public const char FULLWIDTH_COL = (char)((':') + (0xFF00 - 0x20));
        public const char FULLWIDTH_SLASH = (char)(('/') + (0xFF00 - 0x20));
        public const char FULLWIDTH_DASH = (char)(('-') + (0xFF00 - 0x20));
        public const char FULLWIDTH_HASH = (char)(('#') + (0xFF00 - 0x20));
        public const char FULLWIDTH_EQ = (char)(('=') + (0xFF00 - 0x20));
        public const char FULLWIDTH_LT = (char)(('<') + (0xFF00 - 0x20));
        public const char FULLWIDTH_GT = (char)(('>') + (0xFF00 - 0x20));
        public const char FULLWIDTH_LPAREN = (char)(('(') + (0xFF00 - 0x20));
        public const char FULLWIDTH_RPAREN = (char)((')') + (0xFF00 - 0x20));
        public const char FULLWIDTH_LBR = (char)(('[') + (0xFF00 - 0x20));
        public const char FULLWIDTH_RBR = (char)((']') + (0xFF00 - 0x20));
        public const char FULLWIDTH_AMP = (char)(('&') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Q = (char)(('?') + (0xFF00 - 0x20));
        public const char FULLWIDTH_AT = (char)(('@') + (0xFF00 - 0x20));
        public const char FULLWIDTH_DOT = (char)(('.') + (0xFF00 - 0x20));
        public const char FULLWIDTH_PERCENT = (char)(('%') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Hh = (char)(('H') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Hl = (char)(('h') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Oh = (char)(('O') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Ol = (char)(('o') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Eh = (char)(('E') + (0xFF00 - 0x20));
        public const char FULLWIDTH_El = (char)(('e') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Ah = (char)(('A') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Al = (char)(('a') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Fh = (char)(('F') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Fl = (char)(('f') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Ch = (char)(('C') + (0xFF00 - 0x20));
        public const char FULLWIDTH_cl = (char)(('c') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Ph = (char)(('P') + (0xFF00 - 0x20));
        public const char FULLWIDTH_pl = (char)(('p') + (0xFF00 - 0x20));
        public const char FULLWIDTH_Mh = (char)(('M') + (0xFF00 - 0x20));
        public const char FULLWIDTH_ml = (char)(('m') + (0xFF00 - 0x20));

        /*  <summary>
      ''' The possible states that the mini scanning can be in.
      ''' </summary>
    */
        private enum AccumulatorState
        {
            Initial,
            InitialAllowLeadingMultilineTrivia,
            Ident,
            TypeChar,
            FollowingWhite,
            Punctuation,
            CompoundPunctStart,
            CR,
            Done,
            Bad
        }

        // Flags used to classify characters.
        [Flags()]
        private enum CharFlags : ushort
        {
            White = 1 << 0 // simple whitespace (space/tab)
,
            Letter = 1 << 1 // letter, except for "R" (because of REM) and "_"
,
            IdentOnly = 1 << 2 // alowed only in identifiers (cannot start one) - letter "R" (because of REM), "_"
,
            TypeChar = 1 << 3 // legal type character (except !, which is contextually dictionary lookup
,
            Punct = 1 << 4 // some simple punctuation (parens, braces, dot, comma, equals, question)
,
            CompoundPunctStart = 1 << 5 // may be a part of compound punctuation. will be used only if followed by (not white) && (not punct)
,
            CR = 1 << 6 // CR
,
            LF = 1 << 7 // LF
,
            Digit = 1 << 8 // digit 0-9
,
            Complex = 1 << 9 // complex - causes scanning to abort
        }

        private static CharFlags[] charProperties =
        {
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.White, CharFlags.LF, CharFlags.Complex, CharFlags.Complex, CharFlags.CR, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.White, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.TypeChar, CharFlags.TypeChar, CharFlags.TypeChar, CharFlags.Complex, CharFlags.Punct, CharFlags.Punct, CharFlags.CompoundPunctStart, CharFlags.CompoundPunctStart, CharFlags.Punct, CharFlags.CompoundPunctStart, CharFlags.Punct, CharFlags.CompoundPunctStart, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Complex, CharFlags.Complex, CharFlags.CompoundPunctStart, CharFlags.Punct, CharFlags.CompoundPunctStart, CharFlags.Punct, CharFlags.TypeChar, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.IdentOnly, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Complex, CharFlags.CompoundPunctStart, CharFlags.Complex, CharFlags.CompoundPunctStart, CharFlags.IdentOnly, CharFlags.Complex, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.IdentOnly, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Punct, CharFlags.Complex, CharFlags.Punct, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Letter, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Letter, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Letter, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Complex, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Complex, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter
        }

        ;
        private const int CHARPROP_LENGTH = 0x180;
        internal const int MAXTOKENSIZE = 42;
        public readonly struct QuickScanResult
        {
            public QuickScanResult(int start, int length, char[] chars, int hashCode, byte terminatorLength)
            {
                this.Start = start;
                this.Length = length;
                this.Chars = chars;
                this.HashCode = hashCode;
                this.TerminatorLength = terminatorLength;
            }

            public readonly char[] Chars;
            public readonly int Start;
            public readonly int Length;
            public readonly int HashCode;
            public readonly byte TerminatorLength;
            public bool Succeeded
            {
                get
                {
                    return this.Length > 0;
                }
            }
        }

        // Attempt to scan a single token.
        // If it succeeds, return True, and the characters, length, and hashcode of the token
        // can be retrieved by other functions.
        // If it fails (the token is too complex), return False.
        public QuickScanResult QuickScanToken(bool allowLeadingMultilineTrivia)
        {
            AccumulatorState state = allowLeadingMultilineTrivia ? AccumulatorState.InitialAllowLeadingMultilineTrivia : AccumulatorState.Initial;
            var offset = _lineBufferOffset;
            var index = _lineBufferOffset;
            var qtStart = index;
            var limit = index + Math.Min(MAXTOKENSIZE, _bufferLen - offset);
            int hashCode = Hash.FnvOffsetBias;
            byte terminatorLength = 0;
            int unicodeValue = 0;
            while (index < limit)
            {
                var c = buffer[index];
                // Get the flags for that character.
                unicodeValue = (int)(c);
                if (unicodeValue >= CHARPROP_LENGTH)
                {
                    continue;
                }

                var flags = charProperties[unicodeValue];
                // Advance the scanner state.
                switch (state)
                {
                    case AccumulatorState.InitialAllowLeadingMultilineTrivia:
                        if (flags == CharFlags.Letter)
                        {
                            state = AccumulatorState.Ident;
                        }
                        else if (flags == CharFlags.Punct)
                        {
                            state = AccumulatorState.Punctuation;
                        }
                        else if (flags == CharFlags.CompoundPunctStart)
                        {
                            state = AccumulatorState.CompoundPunctStart;
                        }
                        else if ((flags & (CharFlags.White | CharFlags.CR | CharFlags.LF)) != 0)
                        {
                        }
                        else
                        {
                            state = AccumulatorState.Bad;
                            continue;
                        }

                        break;
                    case AccumulatorState.Initial:
                        if (flags == CharFlags.Letter)
                        {
                            state = AccumulatorState.Ident;
                        }
                        else if (flags == CharFlags.Punct)
                        {
                            state = AccumulatorState.Punctuation;
                        }
                        else if (flags == CharFlags.CompoundPunctStart)
                        {
                            state = AccumulatorState.CompoundPunctStart;
                        }
                        else if (flags == CharFlags.White)
                        {
                        }
                        else
                        {
                            state = AccumulatorState.Bad;
                            continue;
                        }

                        break;
                    case AccumulatorState.Ident:
                        if ((flags & (CharFlags.Letter | CharFlags.IdentOnly | CharFlags.Digit)) != 0)
                        {
                        }
                        else if (flags == CharFlags.White)
                        {
                            state = AccumulatorState.FollowingWhite;
                        }
                        else if (flags == CharFlags.CR)
                        {
                            state = AccumulatorState.CR;
                        }
                        else if (flags == CharFlags.LF)
                        {
                            terminatorLength = 1;
                            state = AccumulatorState.Done;
                            continue;
                        }
                        else if (flags == CharFlags.TypeChar)
                        {
                            state = AccumulatorState.TypeChar;
                        }
                        else if (flags == CharFlags.Punct)
                        {
                            state = AccumulatorState.Done;
                            continue;
                        }
                        else
                        {
                            state = AccumulatorState.Bad;
                            continue;
                        }

                        break;
                    case AccumulatorState.TypeChar:
                        if (flags == CharFlags.White)
                        {
                            state = AccumulatorState.FollowingWhite;
                        }
                        else if (flags == CharFlags.CR)
                        {
                            state = AccumulatorState.CR;
                        }
                        else if (flags == CharFlags.LF)
                        {
                            terminatorLength = 1;
                            state = AccumulatorState.Done;
                            continue;
                        }
                        else if ((flags & (CharFlags.Punct | CharFlags.Digit | CharFlags.TypeChar)) != 0)
                        {
                            state = AccumulatorState.Done;
                            continue;
                        }
                        else
                        {
                            state = AccumulatorState.Bad;
                            continue;
                        }

                        break;
                    case AccumulatorState.FollowingWhite:
                        if (flags == CharFlags.White)
                        {
                        }
                        else if (flags == CharFlags.CR)
                        {
                            state = AccumulatorState.CR;
                        }
                        else if (flags == CharFlags.LF)
                        {
                            terminatorLength = 1;
                            state = AccumulatorState.Done;
                            continue;
                        }
                        else if ((flags & (CharFlags.Complex | CharFlags.IdentOnly)) != 0)
                        {
                            state = AccumulatorState.Bad;
                            continue;
                        }
                        else
                        {
                            state = AccumulatorState.Done;
                            continue;
                        }

                        break;
                    case AccumulatorState.Punctuation:
                        if (flags == CharFlags.White)
                        {
                            state = AccumulatorState.FollowingWhite;
                        }
                        else if (flags == CharFlags.CR)
                        {
                            state = AccumulatorState.CR;
                        }
                        else if (flags == CharFlags.LF)
                        {
                            terminatorLength = 1;
                            state = AccumulatorState.Done;
                            continue;
                        }
                        else if ((flags & (CharFlags.Letter | CharFlags.Punct)) != 0)
                        {
                            state = AccumulatorState.Done;
                            continue;
                        }
                        else
                        {
                            state = AccumulatorState.Bad;
                            continue;
                        }

                        break;
                    case AccumulatorState.CompoundPunctStart:
                        if (flags == CharFlags.White)
                        {
                        }
                        else if ((flags & (CharFlags.Letter | CharFlags.Digit)) != 0)
                        {
                            state = AccumulatorState.Done;
                            continue;
                        }
                        else
                        {
                            state = AccumulatorState.Bad;
                            continue;
                        }

                        break;
                    case AccumulatorState.CR:
                        if (flags == CharFlags.LF)
                        {
                            terminatorLength = 2;
                            state = AccumulatorState.Done;
                            continue;
                        }
                        else
                        {
                            state = AccumulatorState.Bad;
                        }

                        continue;
                    default:
                        Debug.Assert(false, "should not get here");
                        break;
                }

                index = 1;
                //FNV-like hash should work here
                //since these strings are short and mostly ASCII
                hashCode = (hashCode ^ unicodeValue) * Hash.FnvPrime;
            }

            if (state == AccumulatorState.Done && terminatorLength == 0)
            {
                if (terminatorLength != 0)
                {
                    index = 1;
                    hashCode = (hashCode ^ unicodeValue) * Hash.FnvPrime;
                }

                return new QuickScanResult(qtStart, index - qtStart, new char[0], hashCode, terminatorLength);
            }
            else
            {
                return default(QuickScanResult);
            }
        }
    }
}
