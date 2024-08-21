using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8602

namespace Microsoft.Language.Xml.Comments
{
    public static class CommentUtilities
    {
        public static IEnumerable<TextSpan> GetValidCommentSpans(this SyntaxNode node, TextSpan commentSpan)
        {
            return GetCommentSpans(node, commentSpan, returnComments: false);
        }

        public static IEnumerable<TextSpan> GetCommentedSpans(this SyntaxNode node, TextSpan commentSpan)
        {
            return GetCommentSpans(node, commentSpan, returnComments: true);
        }

        private static IEnumerable<TextSpan> GetCommentSpans(this SyntaxNode node, TextSpan commentSpan, bool returnComments)
        {
            List<TextSpan> commentSpans = new List<TextSpan>();

            var validCommentRegion = node.GetValidCommentRegion(commentSpan);
            int currentStart = validCommentRegion.Start;

            // Creates comments such that current comments are excluded
            var tokens = node.Tokens(validCommentRegion, descendIntoChildren: n =>
            {
                switch (n.Kind)
                {
                    case SyntaxKind.XmlElementStartTag:
                    case SyntaxKind.XmlElementEndTag:
                    case SyntaxKind.XmlEmptyElement:
                        // Some common cases of high level nodes that cannot contain comments
                        return false;
                    case SyntaxKind.XmlComment:
                        var commentNodeSpan = n.Span;
                        if (returnComments)
                        {
                            commentSpans.Add(commentNodeSpan);
                        }
                        else
                        {
                            var validCommentSpan = TextSpan.FromBounds(currentStart, commentNodeSpan.Start);
                            if (!validCommentSpan.IsEmpty)
                            {
                                commentSpans.Add(validCommentSpan);
                            }

                            currentStart = commentNodeSpan.End;
                        }

                        return false;
                }

                return true;
            });

            // Enumerate in order to force descendIntoChildren anonymous function to be
            // invoked to create text spans
            foreach (var token in tokens)
            {
            }

            if (!returnComments)
            {
                if (currentStart <= validCommentRegion.End)
                {
                    var remainingCommentSpan = TextSpan.FromBounds(currentStart, validCommentRegion.End);
                    if (remainingCommentSpan == validCommentRegion || !remainingCommentSpan.IsEmpty)
                    {
                        // Comment any remaining uncommented area
                        commentSpans.Add(remainingCommentSpan);
                    }
                }
            }

            return commentSpans;
        }

        public static TextSpan GetValidCommentRegion(this SyntaxNode node, TextSpan commentSpan)
        {
            var commentSpanStart = GetCommentStartRegion(node, commentSpan.Start, commentSpan);

            if (commentSpan.Length == 0)
            {
                return commentSpanStart;
            }

            var commentSpanEnd = GetCommentEndRegion(node, commentSpan.End - 1, commentSpan);

            return TextSpan.FromBounds(
                start: Math.Min(commentSpanStart.Start, commentSpanEnd.Start),
                end: Math.Max(Math.Max(commentSpanStart.End, commentSpanEnd.End), commentSpan.End));
        }

        private static TextSpan GetCommentStartRegion(this SyntaxNode node, int position, TextSpan span)
        {
            return GetCommentRegion(node, position, span, isStart: true);
        }

        private static TextSpan GetCommentEndRegion(this SyntaxNode node, int position, TextSpan span)
        {
            return GetCommentRegion(node, position, span, isStart: false);
        }

        private static TextSpan GetCommentRegion(this SyntaxNode node, int position, TextSpan span, bool isStart)
        {
            var commentNode = node.FindNode(position,
                n =>
                {
                    switch (n.Kind)
                    {
                        case SyntaxKind.XmlDocument:
                        case SyntaxKind.List:
                            return true;
                        case SyntaxKind.XmlElement:
                            XmlElementSyntax element = (XmlElementSyntax)n;
                            if (element.StartTag == null || element.EndTag == null)
                            {
                                return true;
                            }

                            var innerSpan = TextSpan.FromBounds(element.StartTag.Span.End, element.EndTag.Span.Start);
                            return innerSpan.Contains(span);
                    }

                    return false;
                });

            if ((commentNode.GetLeadingTriviaSpan().Contains(position)) ||
                (commentNode.GetTrailingTriviaSpan().Contains(position)))
            {
                return new TextSpan(position, 0);
            }

            switch (commentNode.Kind)
            {
                case SyntaxKind.XmlComment:
                case SyntaxKind.XmlEmptyElement:
                case SyntaxKind.XmlDeclaration:
                case SyntaxKind.XmlElement:
                    return commentNode.Span;
                //if (isStart)
                //{
                //    // If position is inside or starting on comment node, the comment should end before the comment
                //    var beginComment = ((XmlCommentSyntax)commentNode).BeginComment;
                //    position = beginComment.Start;
                //}
                //else
                //{
                //    // If position is inside or starting on comment node, the comment should start after the comment
                //    var endComment = ((XmlCommentSyntax)commentNode).EndComment;
                //    position = endComment.Start + endComment.Text.Length;
                //}
                //break;
                case SyntaxKind.XmlElementStartTag:
                case SyntaxKind.XmlElementEndTag:
                    return commentNode.Parent.Span;
                case SyntaxKind.WhitespaceTrivia:
                // TODO: what to do about this case?
                default:
                    //position = commentNode.GetFirstTerminal().Start;
                    break;
            }

            return new TextSpan(position, 0);
        }

        public static bool CanContainComments(this SyntaxNode node)
        {
            switch (node.Kind)
            {
                case SyntaxKind.XmlProcessingInstruction:
                case SyntaxKind.XmlDeclaration:
                case SyntaxKind.XmlElement:
                case SyntaxKind.XmlEmptyElement:
                case SyntaxKind.XmlTextLiteralToken:
                case SyntaxKind.XmlText:
                case SyntaxKind.XmlDocument:
                    return true;
            }

            return false;
        }
    }
}
