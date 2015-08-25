using System;
using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    public class SyntaxListPool
    {
        private Stack<SyntaxListBuilder> _freeList = new Stack<SyntaxListBuilder>();
        public SyntaxListBuilder Allocate()
        {
            if (_freeList.Count > 0)
            {
                return _freeList.Pop();
            }

            return SyntaxListBuilder.Create();
        }

        public SyntaxListBuilder<TNode> Allocate<TNode>() where TNode : SyntaxNode
        {
            return new SyntaxListBuilder<TNode>(this.Allocate());
        }

        public SeparatedSyntaxListBuilder<TNode> AllocateSeparated<TNode>() where TNode : SyntaxNode
        {
            return new SeparatedSyntaxListBuilder<TNode>(this.Allocate());
        }

        public void Free(SyntaxListBuilder item)
        {
            if (item != null)
            {
                item.Clear();
                _freeList.Push(item);
            }
        }
    }
}