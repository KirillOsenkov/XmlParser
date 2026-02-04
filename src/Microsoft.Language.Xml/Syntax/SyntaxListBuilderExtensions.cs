using System;
using System.Diagnostics;


namespace Microsoft.Language.Xml
{
    internal static class SyntaxListBuilderExtensions
    {
        public static SyntaxList<SyntaxNode> ToList(this SyntaxListBuilder builder)
        {
            if (builder == null || builder.Count == 0)
            {
                return default(SyntaxList<SyntaxNode>);
            }

            var listNode = builder.ToListNode();
            Debug.Assert(listNode != null);
            return new SyntaxList<SyntaxNode>(listNode.CreateRed());
        }

        public static SyntaxList<TNode> ToList<TNode>(this SyntaxListBuilder builder)
            where TNode : SyntaxNode
        {
            if (builder == null || builder.Count == 0)
            {
                return new SyntaxList<TNode>();
            }

            var listNode = builder.ToListNode();
            Debug.Assert(listNode != null);
            return new SyntaxList<TNode>(listNode.CreateRed());
        }
    }
}
