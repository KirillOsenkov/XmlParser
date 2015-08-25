using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Language.Xml
{
    public struct SyntaxList<TNode>
        where TNode : SyntaxNode
    {
        private SyntaxNode _node;
        public SyntaxList(SyntaxNode node)
        {
            this._node = node;
        }

        public SyntaxNode Node
        {
            get
            {
                return ((SyntaxNode)this._node);
            }
        }

        public int Count
        {
            get
            {
                return (this._node == null) ? 0 : this._node.IsList ? this._node.SlotCount : 1;
            }
        }

        public TNode Last
        {
            get
            {
                var node = this._node;
                if (node.IsList)
                {
                    return ((TNode)node.GetSlot(node.SlotCount - 1));
                }

                return ((TNode)node);
            }
        }

        /* Not Implemented: Default */
        public TNode this[int index]
        {
            get
            {
                var node = this._node;
                if (node.IsList)
                {
                    return ((TNode)node.GetSlot(index));
                }

                Debug.Assert(index == 0);
                return ((TNode)node);
            }
        }

        public SyntaxNode ItemUntyped(int index)
        {
            var node = this._node;
            if (node.IsList)
            {
                return node.GetSlot(index);
            }

            Debug.Assert(index == 0);
            return node;
        }

        public bool Any()
        {
            return this._node != null;
        }

        public bool Any(SyntaxKind kind)
        {
            for (var i = 0; i < this.Count; i++)
            {
                var element = this.ItemUntyped(i);
                if ((element.Kind == kind))
                {
                    return true;
                }
            }

            return false;
        }

        public TNode[] Nodes
        {
            get
            {
                var arr = new TNode[this.Count];
                for (var i = 0; i < this.Count; i++)
                {
                    arr[i] = this[i];
                }

                return arr;
            }
        }

        public static bool operator ==(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return (left._node == right._node);
        }

        public static bool operator !=(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return !( left._node == right._node);
        }

        public override bool Equals(object obj)
        {
            return (obj is SyntaxList<TNode> && (this._node == ((SyntaxList<TNode>)obj)._node));
        }

        public override int GetHashCode()
        {
            return this._node != null ? this._node.GetHashCode() : 0;
        }

        public SeparatedSyntaxList<TOther> AsSeparatedList<TOther>() where TOther : SyntaxNode
        {
            return new SeparatedSyntaxList<TOther>(new SyntaxList<TOther>(this._node));
        }

        public static implicit operator SyntaxList<TNode>(TNode node)
        {
            return new SyntaxList<TNode>(node);
        }

        public static implicit operator SyntaxList<SyntaxNode>(SyntaxList<TNode> nodes)
        {
            return new SyntaxList<SyntaxNode>(nodes._node);
        }
    }
}
