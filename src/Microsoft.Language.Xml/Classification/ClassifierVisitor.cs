using System;
using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    public class ClassifierVisitor
    {
        private static IDictionary<SyntaxKind, XmlClassificationTypes[]> kindMap = new Dictionary<SyntaxKind, XmlClassificationTypes[]>()
        {
            {
                SyntaxKind.XmlElementStartTag, new[]
                {
                    XmlClassificationTypes.XmlDelimiter,
                    XmlClassificationTypes.XmlName,
                    XmlClassificationTypes.None,
                    XmlClassificationTypes.XmlDelimiter
                }
            },
            {
                SyntaxKind.XmlElementEndTag, new[]
                {
                    XmlClassificationTypes.XmlDelimiter,
                    XmlClassificationTypes.XmlName,
                    XmlClassificationTypes.XmlDelimiter
                }
            },
            {
                SyntaxKind.XmlAttribute, new[]
                {
                    XmlClassificationTypes.XmlAttributeName,
                    XmlClassificationTypes.XmlDelimiter,
                    XmlClassificationTypes.None
                }
            },
            {
                SyntaxKind.XmlString, new[]
                {
                    XmlClassificationTypes.XmlAttributeQuotes,
                    XmlClassificationTypes.XmlAttributeValue,
                    XmlClassificationTypes.XmlAttributeQuotes
                }
            },
            {
                SyntaxKind.XmlComment, new[]
                {
                    XmlClassificationTypes.XmlDelimiter,
                    XmlClassificationTypes.XmlComment,
                    XmlClassificationTypes.XmlDelimiter
                }
            },
            {
                SyntaxKind.XmlCDataSection, new[]
                {
                    XmlClassificationTypes.XmlDelimiter,
                    XmlClassificationTypes.XmlCDataSection,
                    XmlClassificationTypes.XmlDelimiter
                }
            },
            {
                SyntaxKind.XmlEmptyElement, new[]
                {
                    XmlClassificationTypes.XmlDelimiter,
                    XmlClassificationTypes.XmlName,
                    XmlClassificationTypes.None,
                    XmlClassificationTypes.XmlDelimiter
                }
            },
            {
                SyntaxKind.XmlDeclaration, new[]
                {
                    XmlClassificationTypes.XmlDelimiter,
                    XmlClassificationTypes.XmlName,
                    XmlClassificationTypes.None,
                    XmlClassificationTypes.None,
                    XmlClassificationTypes.None,
                    XmlClassificationTypes.XmlDelimiter
                }
            },
            {
                SyntaxKind.XmlDeclarationOption, new[]
                {
                    XmlClassificationTypes.XmlAttributeName,
                    XmlClassificationTypes.XmlDelimiter,
                    XmlClassificationTypes.None
                }
            },
        };

        public static int Visit(
            SyntaxNode node,
            int windowStart,
            int windowLength,
            Action<int, int, SyntaxNode, XmlClassificationTypes>
            resultCollector,
            int start = 0)
        {
            if (node == null)
            {
                return 0;
            }

            XmlClassificationTypes[] childTypes = null;
            kindMap.TryGetValue(node.Kind, out childTypes);
            return VisitChildren(node, windowStart, windowLength, resultCollector, start, childTypes);
        }

        private static int VisitChildren(
            SyntaxNode syntaxNode,
            int windowStart,
            int windowLength,
            Action<int, int, SyntaxNode, XmlClassificationTypes> resultCollector,
            int start,
            XmlClassificationTypes[] childTypes)
        {
            int visitedCount = 0;
            int windowEnd = windowStart + windowLength;
            var targetOffset = windowStart - start;

            int offset;
            int index;
            syntaxNode.GetIndexAndOffset(targetOffset, out index, out offset);
            start += offset;

            for (int i = index; i < syntaxNode.SlotCount; i++)
            {
                if (start > windowEnd)
                {
                    break;
                }

                var child = syntaxNode.GetSlot(i);
                visitedCount++;
                if (child == null)
                {
                    continue;
                }

                var currentStart = Math.Max(start, windowStart);
                var currentLength = Math.Min(windowEnd, start + child.FullWidth) - currentStart;
                if (currentLength >= 0)
                {
                    var childType = childTypes == null ? XmlClassificationTypes.None : childTypes[i];

                    if (childType == XmlClassificationTypes.None)
                    {
                        if (child.Kind == SyntaxKind.XmlTextLiteralToken)
                        {
                            childType = XmlClassificationTypes.XmlText;
                        }
                        else if (child.Kind == SyntaxKind.XmlEntityLiteralToken)
                        {
                            childType = XmlClassificationTypes.XmlEntityReference;
                        }
                    }

                    if (childType == XmlClassificationTypes.None)
                    {
                        visitedCount += Visit(child, windowStart, windowLength, resultCollector, start);
                    }
                    else
                    {
                        if (currentLength > 0)
                        {
                            resultCollector(currentStart, currentLength, child, childType);
                        }
                    }
                }

                start += child.FullWidth;
            }

            return visitedCount;
        }
    }
}
