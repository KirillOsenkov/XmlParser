using System;

namespace Microsoft.Language.Xml
{
    internal class LastTokenReplacer : SyntaxRewriter
    {
        private readonly Func<SyntaxToken, SyntaxToken> _newItem;
        private int _skipCnt;
        private LastTokenReplacer(Func<SyntaxToken, SyntaxToken> newItem)
        {
            _newItem = newItem;
        }

        internal static TTree Replace<TTree>(TTree root, Func<SyntaxToken, SyntaxToken> newItem) where TTree : SyntaxNode
        {
            return (TTree)new LastTokenReplacer(newItem).Visit(root);
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            // node is not interesting until skip count is 0
            if (_skipCnt != 0)
            {
                _skipCnt -= 1;
                return node;
            } // not interested in trivia

            if (!node.IsToken)
            {
                var allChildrenCnt = 0;
                for (int i = 0; i < node.SlotCount; i++)
                {
                    var child = node.GetSlot(i);
                    if (child == null)
                    {
                        continue;
                    }

                    if (child.IsList)
                    {
                        allChildrenCnt += child.SlotCount;
                    }
                    else
                    {
                        allChildrenCnt += 1;
                    }
                }

                // no children
                if (allChildrenCnt == 0)
                {
                    return node;
                }

                var prevIdx = _skipCnt;
                _skipCnt = allChildrenCnt - 1;
                SyntaxNode result;
                if (node.IsList)
                {
                    result = VisitList<SyntaxNode>(node).Node;
                }
                else
                {
                    result = base.Visit(node);
                }

                _skipCnt = prevIdx;
                return result;
            }
            else
            {
                return base.Visit(node);
            }
        }

        public override SyntaxToken VisitSyntaxToken(SyntaxToken token)
        {
            return _newItem(token);
        }
    }
}
