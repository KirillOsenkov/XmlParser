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

        public bool Contains(TNode node)
        {
            return this.IndexOf(node) >= 0;
        }

        public int IndexOf(TNode node)
        {
            for (int i = 0, n = this.Count; i < n; i++)
            {
                if (object.Equals(this[i], node))
                {
                    return i;
                }
            }

            return -1;
        }

        public int IndexOf(Func<TNode, bool> predicate)
        {
            for (int i = 0, n = this.Count; i < n; i++)
            {
                if (predicate(this[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        internal int IndexOf(SyntaxKind kind)
        {
            for (int i = 0, n = this.Count; i < n; i++)
            {
                if (this[i].Kind == kind)
                {
                    return i;
                }
            }

            return -1;
        }

        public int LastIndexOf(TNode node)
        {
            for (int i = this.Count - 1; i >= 0; i--)
            {
                if (object.Equals(this[i], node))
                {
                    return i;
                }
            }

            return -1;
        }

        public int LastIndexOf(Func<TNode, bool> predicate)
        {
            for (int i = this.Count - 1; i >= 0; i--)
            {
                if (predicate(this[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        internal bool Any(Func<TNode, bool> predicate)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (predicate(this[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
