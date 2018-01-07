using System;
using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;

    internal class SyntaxListPool
    {
        private Stack<InternalSyntax.SyntaxListBuilder> _freeList = new Stack<InternalSyntax.SyntaxListBuilder>();
        public InternalSyntax.SyntaxListBuilder Allocate()
        {
            if (_freeList.Count > 0)
            {
                return _freeList.Pop();
            }

            return InternalSyntax.SyntaxListBuilder.Create();
        }

        public InternalSyntax.SyntaxListBuilder<TNode> Allocate<TNode>() where TNode : GreenNode
        {
            return new InternalSyntax.SyntaxListBuilder<TNode>(this.Allocate());
        }

        /*public SeparatedSyntaxListBuilder<TNode> AllocateSeparated<TNode>() where TNode : GreenNode
        {
            return new SeparatedSyntaxListBuilder<TNode>(this.Allocate());
        }*/

        public void Free(InternalSyntax.SyntaxListBuilder item)
        {
            if (item != null)
            {
                item.Clear();
                _freeList.Push(item);
            }
        }
    }
}