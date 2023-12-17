using System;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    internal class LastTokenReplacer : InternalSyntax.SyntaxRewriter
    {
        private readonly Func<SyntaxToken.Green, SyntaxToken.Green> _newItem;
        private int _skipCnt;
        private LastTokenReplacer(Func<SyntaxToken.Green, SyntaxToken.Green> newItem)
        {
            _newItem = newItem;
        }

        internal static TTree Replace<TTree>(TTree root, Func<SyntaxToken.Green, SyntaxToken.Green> newItem) where TTree : GreenNode
        {
            return (TTree)new LastTokenReplacer(newItem).Visit(root);
        }

        public override GreenNode Visit(GreenNode node)
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
                GreenNode result;
                if (node.IsList)
                {
                    result = VisitList<GreenNode>(new InternalSyntax.SyntaxList<GreenNode>(node)).Node;
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

        public override SyntaxToken.Green VisitSyntaxToken(SyntaxToken.Green token)
        {
            return _newItem(token);
        }
    }
}
