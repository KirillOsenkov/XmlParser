using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    internal class FirstTokenReplacer : InternalSyntax.SyntaxRewriter
    {
        private readonly Func<SyntaxToken.Green, SyntaxToken.Green> _newItem;
        private bool _isFirst = true;

        private FirstTokenReplacer(Func<SyntaxToken.Green, SyntaxToken.Green> newItem)
        {
            _newItem = newItem;
        }

        internal static TTree Replace<TTree>(TTree root, Func<SyntaxToken.Green, SyntaxToken.Green> newItem) where TTree : GreenNode
        {
            return ((TTree)new FirstTokenReplacer(newItem).Visit(root));
        }

        public override SyntaxToken.Green VisitSyntaxToken(SyntaxToken.Green token)
        {
            if (token == null)
            {
                return null;
            }

            if (_isFirst)
            {
                _isFirst = false;
                return _newItem(token);
            }

            return token;
        }
    }
}
