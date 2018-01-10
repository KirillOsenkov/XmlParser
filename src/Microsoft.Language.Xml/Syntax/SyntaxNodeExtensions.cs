using System;

namespace Microsoft.Language.Xml
{
    public static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Creates a new tree of nodes with the specified old token replaced with a new token.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="oldToken">The token to be replaced.</param>
        /// <param name="newToken">The new token to use in the new tree in place of the old
        /// token.</param>
        public static TRoot ReplaceToken<TRoot>(this TRoot root, SyntaxToken oldToken, SyntaxToken newToken)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore<SyntaxNode>(tokens: new[] { oldToken }, computeReplacementToken: (o, r) => newToken);
        }

        /// <summary>
        /// Creates a new node from this node with the leading trivia replaced.
        /// </summary>
        public static TSyntax WithLeadingTrivia<TSyntax>(
            this TSyntax node,
            SyntaxNode trivia) where TSyntax : SyntaxNode
        {
            var first = node.GetFirstToken();
            var newFirst = first.WithLeadingTrivia(trivia);
            return node.ReplaceToken(first, newFirst);
        }

        /// <summary>
        /// Creates a new node from this node with the trailing trivia replaced.
        /// </summary>
        public static TSyntax WithTrailingTrivia<TSyntax>(
            this TSyntax node,
            SyntaxNode trivia) where TSyntax : SyntaxNode
        {
            var last = node.GetLastToken();
            var newLast = last.WithTrailingTrivia(trivia);
            return node.ReplaceToken(last, newLast);
        }

        /// <summary>
        /// Returns true if the node is a XmlElementSyntax or XmlEmptyElementSyntax
        /// </summary>
        public static bool IsElement(this SyntaxNode node)
        {
            return node.Kind == SyntaxKind.XmlElement || node.Kind == SyntaxKind.XmlEmptyElement;
        }

        internal static bool IsXmlNodeName(this XmlNameSyntax name)
        {
            var p = name.Parent;
            switch (p.Kind)
            {
                case SyntaxKind.XmlElement:
                case SyntaxKind.XmlEmptyElement:
                case SyntaxKind.XmlElementStartTag:
                case SyntaxKind.XmlElementEndTag:
                    return true;
                default: return false;
            }
        }
    }
}
