using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Language.Xml
{
    public struct SeparatedSyntaxListBuilder<TNode>
        where TNode : SyntaxNode
    {
        private SyntaxListBuilder builder;
        public SeparatedSyntaxListBuilder(int size) : this(new SyntaxListBuilder(size))
        {
        }

        public SeparatedSyntaxListBuilder(SyntaxListBuilder builder)
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

        public void Clear()
        {
            this.builder.Clear();
        }

        public void Add(TNode node)
        {
            this.builder.Add(node);
        }

        public void AddSeparator(SyntaxToken separatorToken)
        {
            this.builder.Add(separatorToken);
        }

        public void AddRange(SeparatedSyntaxList<TNode> nodes, int count)
        {
            var list = nodes.GetWithSeparators();
            this.builder.AddRange(list, this.Count, Math.Min(count * 2, list.Count));
        }

        public void RemoveLast()
        {
            this.builder.RemoveLast();
        }

        public bool Any(SyntaxKind kind)
        {
            return this.builder.Any(kind);
        }

        public SeparatedSyntaxList<TNode> ToList()
        {
            return new SeparatedSyntaxList<TNode>(new SyntaxList<SyntaxNode>(this.builder.ToListNode()));
        }

        public SeparatedSyntaxList<TDerivedNode> ToList<TDerivedNode>() where TDerivedNode : TNode
        {
            return new SeparatedSyntaxList<TDerivedNode>(new SyntaxList<SyntaxNode>(this.builder.ToListNode()));
        }

        public static implicit operator SyntaxListBuilder(SeparatedSyntaxListBuilder<TNode> builder)
        {
            return builder.builder;
        }
    }
}
