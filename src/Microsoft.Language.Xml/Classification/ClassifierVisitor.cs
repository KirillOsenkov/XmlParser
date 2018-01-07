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

        private struct VisitState
        {
            public SyntaxNode node;
            public XmlClassificationTypes[] childTypes;
            public int i;
            public SyntaxNode child;
            public XmlClassificationTypes childType;
            public bool continueInsideForLoop;
        }

        public static void Visit(
            SyntaxNode node,
            int windowStart,
            int windowLength,
            Action<int, int, SyntaxNode, XmlClassificationTypes> resultCollector)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            int start = 0;
            int windowEnd = windowStart + windowLength;

            VisitState currentState = CreateState(node);

            Stack<VisitState> stateStack = new Stack<VisitState>();
            stateStack.Push(currentState);

            while (stateStack.Count != 0)
            {
                currentState = stateStack.Pop();

            AfterPopCurrentState:
                if (currentState.continueInsideForLoop)
                {
                    currentState.continueInsideForLoop = false;
                    currentState.i++;
                    goto ForLoop;
                }

                kindMap.TryGetValue(currentState.node.Kind, out currentState.childTypes);

                currentState.i = 0;

            ForLoop:
                for (; currentState.i < currentState.node.SlotCount; currentState.i++)
                {
                    if (start > windowEnd)
                    {
                        return;
                    }

                    currentState.child = currentState.node.GetNodeSlot(currentState.i);
                    if (currentState.child == null)
                    {
                        continue;
                    }

                    int currentStart = Math.Max(start, windowStart);
                    int currentLength = Math.Min(windowEnd, start + currentState.child.FullWidth) - currentStart;
                    if (currentLength >= 0)
                    {
                        currentState.childType = currentState.childTypes == null ? XmlClassificationTypes.None : currentState.childTypes[currentState.i];

                        if (currentState.childType == XmlClassificationTypes.None)
                        {
                            if (currentState.child.Kind == SyntaxKind.XmlTextLiteralToken)
                            {
                                currentState.childType = XmlClassificationTypes.XmlText;
                            }
                            else if (currentState.child.Kind == SyntaxKind.XmlEntityLiteralToken)
                            {
                                currentState.childType = XmlClassificationTypes.XmlEntityReference;
                            }
                        }

                        if (currentState.childType == XmlClassificationTypes.None)
                        {
                            currentState.continueInsideForLoop = true;
                            stateStack.Push(currentState);

                            currentState = CreateState(
                                currentState.child);
                            goto AfterPopCurrentState;
                        }
                        else
                        {
                            if (currentLength > 0)
                            {
                                var returnNode = currentState.child;
                                if (start > windowEnd)
                                {
                                    return;
                                }

                                if (currentStart + currentLength >= windowStart)
                                {
                                    resultCollector(
                                        currentStart,
                                        currentLength,
                                        returnNode,
                                        currentState.childType);
                                }
                            }
                        }
                    }

                    start += currentState.child.FullWidth;
                }
            }
        }

        private static VisitState CreateState(SyntaxNode node)
        {
            return new VisitState()
            {
                node = node,
            };
        }

        public static int VisitRecursive(
            SyntaxNode node,
            int windowStart,
            int windowLength,
            Action<int, int, SyntaxNode, XmlClassificationTypes> resultCollector,
            int start = 0)
        {
            if (node == null)
            {
                return 0;
            }

            XmlClassificationTypes[] childTypes = null;
            kindMap.TryGetValue(node.Kind, out childTypes);

            int visitedCount = 0;
            int windowEnd = windowStart + windowLength;
            var targetOffset = windowStart - start;

            int offset;
            int index;
            node.GetIndexAndOffset(targetOffset, out index, out offset);
            start += offset;

            for (int i = index; i < node.SlotCount; i++)
            {
                if (start > windowEnd)
                {
                    break;
                }

                var child = node.GetNodeSlot(i);
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
                        visitedCount += VisitRecursive(child, windowStart, windowLength, resultCollector, start);
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
