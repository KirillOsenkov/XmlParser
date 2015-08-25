using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Language.Xml
{
    public struct SyntaxListBuilder<TNode>
        where TNode : SyntaxNode
    {
        private SyntaxListBuilder builder;
        public static SyntaxListBuilder<TNode> Create()
        {
            return new SyntaxListBuilder<TNode>(8);
        }

        public SyntaxListBuilder(int size) : this(new SyntaxListBuilder(size))
        {
        }

        public SyntaxListBuilder(SyntaxListBuilder builder)
        {
            this.builder = builder;
        }

        public bool IsNull
        {
            get
            {
                return (this.builder == null);
            }
        }

        public int Count
        {
            get
            {
                return this.builder.Count;
            }
        }

        public TNode this[int index]
        {
            get
            {
                return (TNode)this.builder[index];
            }

            set
            {
                this.builder[index] = value;
            }
        }

        public void RemoveLast()
        {
            this.builder.RemoveLast();
        }

        public void Clear()
        {
            this.builder.Clear();
        }

        public void Add(TNode node)
        {
            this.builder.Add(node);
        }

        public void AddRange(SyntaxList<TNode> nodes)
        {
            this.builder.AddRange<TNode>(nodes);
        }

        public void AddRange(SyntaxList<TNode> nodes, int offset, int length)
        {
            this.builder.AddRange<TNode>(nodes, offset, length);
        }

        public bool Any(SyntaxKind kind)
        {
            return this.builder.Any(kind);
        }

        public SyntaxList<TNode> ToList()
        {
            Debug.Assert(this.builder != null);
            return this.builder.ToList<TNode>();
        }

        public SyntaxList<TDerivedNode> ToList<TDerivedNode>() where TDerivedNode : TNode
        {
            Debug.Assert(this.builder != null);
            return this.builder.ToList<TDerivedNode>();
        }

        public static implicit operator SyntaxListBuilder(SyntaxListBuilder<TNode> builder)
        {
            return builder.builder;
        }

        public static implicit operator SyntaxList<TNode>(SyntaxListBuilder<TNode> builder)
        {
            return builder.ToList();
        }
    }
}
