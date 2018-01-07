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

        public override GreenNode VisitSyntaxNode(GreenNode node)
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

        public override SyntaxToken.Green VisitSyntaxToken(SyntaxToken.Green token)
        {
            return _newItem(token);
        }
    }
}
