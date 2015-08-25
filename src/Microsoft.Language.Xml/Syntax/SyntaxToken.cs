using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Language.Xml
{
    public abstract class SyntaxToken : SyntaxNode
    {
        public SyntaxToken(SyntaxKind kind, string text, SyntaxNode leadingTrivia, SyntaxNode trailingTrivia)
            : base(kind, text.Length)
        {
            Text = text;
            if (trailingTrivia != null)
            {
                AdjustWidth(trailingTrivia);
                _trailingTriviaOrTriviaInfo = trailingTrivia;
            }

            if (leadingTrivia != null)
            {
                AdjustWidth(leadingTrivia);
                _trailingTriviaOrTriviaInfo = TriviaInfo.Create(leadingTrivia, _trailingTriviaOrTriviaInfo as SyntaxNode);
            }

            ClearFlagIfMissing();
        }

        public override SyntaxNode GetSlot(int index)
        {
            throw new InvalidOperationException();
        }

        internal override void CollectConstituentTokensAndDiagnostics(SyntaxListBuilder<SyntaxToken> tokenListBuilder, IList<DiagnosticInfo> nonTokenDiagnostics)
        {
            tokenListBuilder.Add(this);
        }

        private void ClearFlagIfMissing()
        {
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitSyntaxToken(this);
        }

        public override bool IsToken
        {
            get
            {
                return true;
            }
        }

        public string Text { get; internal set; }
        private object _trailingTriviaOrTriviaInfo = null;

        public override int GetLeadingTriviaWidth()
        {
            var triviaInfo = _trailingTriviaOrTriviaInfo as TriviaInfo;
            if (triviaInfo != null)
            {
                return triviaInfo._leadingTrivia.FullWidth;
            }

            return 0;
        }

        public override int GetTrailingTriviaWidth()
        {
            return FullWidth - GetLeadingTriviaWidth() - Text.Length;
        }

        public override SyntaxNode GetLeadingTrivia()
        {
            var t = (_trailingTriviaOrTriviaInfo as TriviaInfo);
            if (t != null)
            {
                return t._leadingTrivia;
            }

            return null;
        }

        public override SyntaxNode GetTrailingTrivia()
        {
            var arr = _trailingTriviaOrTriviaInfo as SyntaxNode;
            if (arr != null)
            {
                return arr;
            }

            var t = _trailingTriviaOrTriviaInfo as TriviaInfo;
            if (t != null)
            {
                return t._trailingTrivia;
            }

            return null;
        }

        internal override void WriteToOrFlatten(TextWriter writer, Stack<SyntaxNode> stack)
        {
            var leadingTrivia = GetLeadingTrivia();
            if (leadingTrivia != null)
            {
                leadingTrivia.WriteTo(writer); //Append leading trivia
            }

            writer.Write(Text); //Append text of token itself

            var trailingTrivia = GetTrailingTrivia();
            if (trailingTrivia != null)
            {
                trailingTrivia.WriteTo(writer); // Append trailing trivia
            }
        }

        public override string ToString()
        {
            return Text;
        }

        /*  <summary>
        ''' Create a new token with the trivia prepended to the existing preceding trivia
        ''' </summary>
        */
        public static T AddLeadingTrivia<T>(T token, SyntaxList<SyntaxNode> newTrivia) where T : SyntaxToken
        {
            Debug.Assert(token != null);
            if (newTrivia.Node == null)
            {
                return token;
            }

            var oldTrivia = new SyntaxList<SyntaxNode>(token.GetLeadingTrivia());
            SyntaxNode leadingTrivia;
            if (oldTrivia.Node == null)
            {
                leadingTrivia = newTrivia.Node;
            }
            else
            {
                var leadingTriviaBuilder = SyntaxListBuilder<SyntaxNode>.Create();
                leadingTriviaBuilder.AddRange(newTrivia);
                leadingTriviaBuilder.AddRange(oldTrivia);
                leadingTrivia = leadingTriviaBuilder.ToList().Node;
            }

            return (T)token.WithLeadingTrivia(leadingTrivia);
        }

        /*  <summary>
        ''' Create a new token with the trivia appended to the existing following trivia
        ''' </summary>
        */
        public static T AddTrailingTrivia<T>(T token, SyntaxList<SyntaxNode> newTrivia) where T : SyntaxToken
        {
            Debug.Assert(token != null);
            if (newTrivia.Node == null)
            {
                return token;
            }

            var oldTrivia = new SyntaxList<SyntaxNode>(token.GetTrailingTrivia());
            SyntaxNode trailingTrivia;
            if (oldTrivia.Node == null)
            {
                trailingTrivia = newTrivia.Node;
            }
            else
            {
                var trailingTriviaBuilder = SyntaxListBuilder<SyntaxNode>.Create();
                trailingTriviaBuilder.AddRange(oldTrivia);
                trailingTriviaBuilder.AddRange(newTrivia);
                trailingTrivia = trailingTriviaBuilder.ToList().Node;
            }

            return ((T)token.WithTrailingTrivia(trailingTrivia));
        }
    }
}
