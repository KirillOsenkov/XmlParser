using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Language.Xml
{
    public struct SeparatedSyntaxList<TNode>
        where TNode : SyntaxNode
    {
        private SyntaxList<SyntaxNode> _list;
        public SeparatedSyntaxList(SyntaxList<SyntaxNode> list)
        {
            this._list = list;
        }

        public SyntaxNode Node
        {
            get
            {
                return this._list.Node;
            }
        }

        public int Count
        {
            get
            {
                return (this._list.Count + 1) >> 1;
            }
        }

        public TNode this[int index]
        {
            get
            {
                return (TNode)_list[index << 1];
            }
        }

        public int SeparatorCount
        {
            get
            {
                return (this._list.Count) >> 1;
            }
        }

        /*  <summary>
        ''' Gets the separator at the given index in this list.
        ''' </summary>
        ''' <param name="index">The index.</param><returns></returns>
         */
        public SyntaxToken GetSeparator(int index)
        {
            return ((SyntaxToken)this._list[(index << 1) + 1]);
        }

        public bool Any()
        {
            return (this.Count > 0);
        }

        public bool Any(SyntaxKind kind)
        {
            for (var i = 0; i < this.Count; i++)
            {
                var element = this[i];
                if ((element.Kind == kind))
                {
                    return true;
                }
            }

            return false;
        }

        public SyntaxList<SyntaxNode> GetWithSeparators()
        {
            return this._list;
        }
    }
}
