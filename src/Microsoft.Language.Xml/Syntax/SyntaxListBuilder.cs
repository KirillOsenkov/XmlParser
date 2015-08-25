using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Language.Xml
{
    public class SyntaxListBuilder
    {
        private int _count;
        private ArrayElement<SyntaxNode>[] _nodes;
        public static SyntaxListBuilder Create()
        {
            return new SyntaxListBuilder(8);
        }

        public SyntaxListBuilder(int size)
        {
            this._nodes = new ArrayElement<SyntaxNode>[size];
        }

        public SyntaxListBuilder Add(SyntaxNode item)
        {
            EnsureAdditionalCapacity(1);
            return this.AddUnsafe(item);
        }

        private SyntaxListBuilder AddUnsafe(SyntaxNode item)
        {
            Debug.Assert(item != null);
            this._nodes[this._count].Value = ((SyntaxNode)item);
            this._count += 1;
            return this;
        }

        public SyntaxListBuilder AddRange<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode
        {
            return this.AddRange<TNode>(list, 0, list.Count);
        }

        public SyntaxListBuilder AddRange<TNode>(SyntaxList<TNode> list, int offset, int length) where TNode : SyntaxNode
        {
            EnsureAdditionalCapacity(length - offset);
            var oldCount = this._count;
            for (var i = offset; i < offset + length; i++)
            {
                AddUnsafe(list.ItemUntyped(i));
            }

            this.Validate(oldCount, this._count);
            return this;
        }

        public bool Any(SyntaxKind kind)
        {
            for (var i = 0; i < this._count; i++)
            {
                if ((this._nodes[i].Value.Kind == kind))
                {
                    return true;
                }
            }

            return false;
        }

        public void RemoveLast()
        {
            this._count = 1;
            this._nodes[this._count] = default(ArrayElement<SyntaxNode>);
        }

        public void Clear()
        {
            this._count = 0;
        }

        private void EnsureAdditionalCapacity(int additionalCount)
        {
            int currentSize = this._nodes.Length;
            int requiredSize = this._count + additionalCount;
            if (requiredSize <= currentSize)
            {
                return;
            }

            int newSize = requiredSize < 8 ? 8 : requiredSize >= int.MaxValue / 2 ? int.MaxValue : Math.Max(requiredSize, currentSize * 2);
            Debug.Assert(newSize >= requiredSize);
            Array.Resize(ref this._nodes, newSize);
        }

        public ArrayElement<SyntaxNode>[] ToArray()
        {
            ArrayElement<SyntaxNode>[] dst = new ArrayElement<SyntaxNode>[this._count];

            int i = 0;
            while (i < dst.Length)
            {
                dst[i] = this._nodes[i];
                i++;
            }

            return dst;
        }

        public SyntaxNode ToListNode()
        {
            switch (this._count)
            {
                case 0:
                    return null;
                case 1:
                    return this._nodes[0];
                case 2:
                    return SyntaxList.List(this._nodes[0], this._nodes[1]);
            }

            return SyntaxList.List(this.ToArray());
        }

        [Conditional("DEBUG")]
        private void Validate(int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                Debug.Assert(this._nodes[i].Value != null);
            }
        }

        public int Count
        {
            get
            {
                return this._count;
            }
        }

        public SyntaxNode this[int index]
        {
            get
            {
                return _nodes[index];
            }

            set
            {
                _nodes[index].Value = value;
            }
        }

        public SyntaxList<SyntaxNode> ToList()
        {
            return new SyntaxList<SyntaxNode>(ToListNode());
        }

        public SyntaxList<TDerived> ToList<TDerived>() where TDerived : SyntaxNode
        {
            return new SyntaxList<TDerived>(ToListNode());
        }
    }
}
