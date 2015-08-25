using System;

namespace Microsoft.Language.Xml
{
    internal class FirstTokenReplacer : SyntaxRewriter
    {
        private readonly Func<SyntaxToken, SyntaxToken> _newItem;
        private bool _isFirst = true;

        private FirstTokenReplacer(Func<SyntaxToken, SyntaxToken> newItem)
        {
            _newItem = newItem;
        }

        internal static TTree Replace<TTree>(TTree root, Func<SyntaxToken, SyntaxToken> newItem) where TTree : SyntaxNode
        {
            return ((TTree)new FirstTokenReplacer(newItem).Visit(root));
        }

        public override SyntaxNode VisitSyntaxNode(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            // we are not interested in nodes that are not first
            if (!_isFirst)
            {
                return node;
            }

            var result = base.VisitSyntaxNode(node);
            _isFirst = false;
            return result;
        }

        public override SyntaxToken VisitSyntaxToken(SyntaxToken token)
        {
            return _newItem(token);
        }
    }
}
