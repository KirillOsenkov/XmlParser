using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    public class SyntaxTrivia : SyntaxNode
    {
        internal class Green : GreenNode
        {
            public string Text { get; }

            internal Green(SyntaxKind kind, string text)
                : base(kind, text.Length)
            {
                Text = text;
            }

            public override int Width => Text.Length;

            internal override void WriteToOrFlatten(TextWriter writer, Stack<GreenNode> stack)
            {
                writer.Write(Text);
            }

            public sealed override string ToFullString() => Text;

            public sealed override int GetLeadingTriviaWidth() => 0;
            public sealed override int GetTrailingTriviaWidth() => 0;

            protected override sealed int GetSlotCount() => 0;

            internal override sealed GreenNode GetSlot(int index)
            {
                throw new InvalidOperationException();
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new SyntaxTrivia(this, parent, position);

            internal override GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitSyntaxTrivia(this);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        public string Text => GreenNode.Text;

        internal SyntaxTrivia(Green green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

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
            return visitor.VisitSyntaxTrivia(this);
        }

        protected override int GetTextWidth()
        {
            return Text.Length;
        }

		public sealed override SyntaxTriviaList GetTrailingTrivia()
        {
			return default(SyntaxTriviaList);
        }

		public sealed override SyntaxTriviaList GetLeadingTrivia()
        {
			return default(SyntaxTriviaList);
        }

        public override string ToString() => Text;
    }
}
