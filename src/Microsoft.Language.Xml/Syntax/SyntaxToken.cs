using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public abstract class SyntaxToken : SyntaxNode
    {
        internal abstract class Green : GreenNode
        {
            public string Text { get; }
            public GreenNode LeadingTrivia { get; }
            public GreenNode TrailingTrivia { get; }

            internal Green(SyntaxKind tokenKind, string text, GreenNode leadingTrivia, GreenNode trailingTrivia)
                : base(tokenKind, text.Length)
            {
                Text = text;
                LeadingTrivia = leadingTrivia;
                AdjustWidth(leadingTrivia);
                TrailingTrivia = trailingTrivia;
                AdjustWidth(trailingTrivia);
            }

            internal override bool IsToken => true;

            public override int Width => Text.Length;

            internal override void WriteToOrFlatten(TextWriter writer, Stack<GreenNode> stack)
            {
                var leadingTrivia = LeadingTrivia;
                if (leadingTrivia != null)
                {
                    leadingTrivia.WriteTo(writer); //Append leading trivia
                }

                writer.Write(Text); //Append text of token itself

                var trailingTrivia = TrailingTrivia;
                if (trailingTrivia != null)
                {
                    trailingTrivia.WriteTo(writer); // Append trailing trivia
                }
            }

            internal override sealed GreenNode GetLeadingTrivia() => LeadingTrivia;
            public override int GetLeadingTriviaWidth() => LeadingTrivia == null ? 0 : LeadingTrivia.FullWidth;
            internal override sealed GreenNode GetTrailingTrivia() => TrailingTrivia;
            public override int GetTrailingTriviaWidth() => TrailingTrivia == null ? 0 : TrailingTrivia.FullWidth;

            public abstract SyntaxToken.Green WithLeadingTrivia(GreenNode trivia);
            public abstract SyntaxToken.Green WithTrailingTrivia(GreenNode trivia);

            protected override sealed int GetSlotCount() => 0;

            internal override sealed GreenNode GetSlot(int index)
            {
                throw new InvalidOperationException();
            }

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitSyntaxToken(this);
            }

            public override string ToString() => Text;

            /*  <summary>
			 * ''' Create a new token with the trivia prepended to the existing preceding trivia
			 * ''' </summary>
			*/
            public static T AddLeadingTrivia<T>(T token, InternalSyntax.SyntaxList<GreenNode> newTrivia) where T : Green
            {
                Debug.Assert(token != null);
                if (newTrivia.Node == null)
                {
                    return token;
                }

                var oldTrivia = new InternalSyntax.SyntaxList<GreenNode>(token.GetLeadingTrivia());
                GreenNode leadingTrivia;
                if (oldTrivia.Node == null)
                {
                    leadingTrivia = newTrivia.Node;
                }
                else
                {
                    var leadingTriviaBuilder = InternalSyntax.SyntaxListBuilder<GreenNode>.Create();
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
            public static T AddTrailingTrivia<T>(T token, InternalSyntax.SyntaxList<GreenNode> newTrivia) where T : Green
            {
                Debug.Assert(token != null);
                if (newTrivia.Node == null)
                {
                    return token;
                }

                var oldTrivia = new InternalSyntax.SyntaxList<GreenNode>(token.GetTrailingTrivia());
                GreenNode trailingTrivia;
                if (oldTrivia.Node == null)
                {
                    trailingTrivia = newTrivia.Node;
                }
                else
                {
                    var trailingTriviaBuilder = InternalSyntax.SyntaxListBuilder<GreenNode>.Create();
                    trailingTriviaBuilder.AddRange(oldTrivia);
                    trailingTriviaBuilder.AddRange(newTrivia);
                    trailingTrivia = trailingTriviaBuilder.ToList().Node;
                }

                return ((T)token.WithTrailingTrivia(trailingTrivia));
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        internal SyntaxToken(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        /*internal override void CollectConstituentTokensAndDiagnostics(SyntaxListBuilder<SyntaxToken> tokenListBuilder, IList<DiagnosticInfo> nonTokenDiagnostics)
        {
            tokenListBuilder.Add(this);
        }*/

        public override SyntaxNode GetLeadingTrivia()
        {
            if (GreenNode.LeadingTrivia == null)
                return null;
            return GreenNode.LeadingTrivia.CreateRed(this, Start);
        }

        public override SyntaxNode GetTrailingTrivia()
        {
            var trailingGreen = GreenNode.TrailingTrivia;
            if (trailingGreen == null)
                return null;
            int trailingPosition = Start + this.FullWidth;
            if (trailingGreen != null)
            {
                trailingPosition -= trailingGreen.FullWidth;
            }

            return trailingGreen.CreateRed(this, trailingPosition);
        }

        public abstract SyntaxToken WithLeadingTrivia(SyntaxNode trivia);
        public abstract SyntaxToken WithTrailingTrivia(SyntaxNode trivia);

        internal override sealed SyntaxNode GetCachedSlot(int index)
        {
            throw new InvalidOperationException();
        }

        internal override sealed SyntaxNode GetNodeSlot(int slot)
        {
            throw new InvalidOperationException();
        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitSyntaxToken(this);
        }

        public override bool IsToken => true;

        public string Text => GreenNode.Text;

        protected override int GetTextWidth()
        {
            return Text.Length;
        }

        public override int GetSlotCountIncludingTrivia()
        {
            int triviaSlots = 0;
            if (GetLeadingTrivia() != null)
                triviaSlots++;
            if (GetTrailingTrivia() != null)
                triviaSlots++;

            return triviaSlots;
        }

        public override SyntaxNode GetSlotIncludingTrivia(int index)
        {
            if (index == 0)
            {
                var trivia = GetLeadingTrivia();
                return trivia ?? GetTrailingTrivia();
            }
            else if (index == 1)
            {
                return GetTrailingTrivia();
            }

            throw new IndexOutOfRangeException();
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
