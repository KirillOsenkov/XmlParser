using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Language.Xml
{
    internal partial class SyntaxTrivia : SyntaxNode
    {
        private readonly string _text;
        internal SyntaxTrivia(SyntaxKind kind, string text) : base(kind, text.Length)
        {
            this._text = text;
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitSyntaxTrivia(this);
        }

        public override SyntaxNode GetSlot(int index)
        {
            throw new InvalidOperationException();
        }

        internal string Text
        {
            get
            {
                return this._text;
            }
        }

        public sealed override SyntaxNode GetTrailingTrivia()
        {
            return null;
        }

        public sealed override int GetTrailingTriviaWidth()
        {
            return 0;
        }

        public sealed override SyntaxNode GetLeadingTrivia()
        {
            return null;
        }

        public sealed override int GetLeadingTriviaWidth()
        {
            return 0;
        }

        internal override void WriteToOrFlatten(TextWriter writer, Stack<SyntaxNode> stack)
        {
            writer.Write(Text); //write text of token itself
        }

        public sealed override string ToFullString()
        {
            return this._text;
        }

        public override string ToString()
        {
            return this._text;
        }
    }
}
