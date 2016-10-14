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
            public int windowStart;
            public int windowLength;
            public int start;
            public XmlClassificationTypes[] childTypes;
            public int visitedCount;
            public int windowEnd;
            public int targetOffset;
            public int offset;
            public int index;
            public int currentStart;
            public int currentLength;
            public int i;
            public SyntaxNode child;
            public XmlClassificationTypes childType;
            public bool continueInsideForLoop;
        }

        public static int Visit(
            SyntaxNode node,
            int windowStart,
            int windowLength,
            Action<int, int, SyntaxNode, XmlClassificationTypes> resultCollector,
            int start = 0)
        {
            VisitState currentState = CreateState(node, windowStart, windowLength, start);

            Stack<VisitState> stateStack = new Stack<VisitState>();
            stateStack.Push(currentState);

            int result = 0;

            while (stateStack.Count != 0)
            {
                currentState = stateStack.Pop();

                AfterPopCurrentState:
                if (currentState.continueInsideForLoop)
                {
                    currentState.visitedCount += result;
                    currentState.continueInsideForLoop = false;
                    currentState.i++;
                    goto ForLoop;
                }

                if (currentState.node == null)
                {
                    result = 0;
                    continue;
                }

                kindMap.TryGetValue(currentState.node.Kind, out currentState.childTypes);

                currentState.visitedCount = 0;
                currentState.windowEnd = currentState.windowStart + currentState.windowLength;
                currentState.targetOffset = currentState.windowStart - currentState.start;

                currentState.node.GetIndexAndOffset(currentState.targetOffset, out currentState.index, out currentState.offset);
                currentState.start += currentState.offset;

                currentState.i = currentState.index;

                ForLoop:
                for (; currentState.i < currentState.node.SlotCount; currentState.i++)
                {
                    if (currentState.start > currentState.windowEnd)
                    {
                        break;
                    }

                    currentState.child = currentState.node.GetSlot(currentState.i);
                    currentState.visitedCount++;
                    if (currentState.child == null)
                    {
                        continue;
                    }

                    currentState.currentStart = Math.Max(currentState.start, currentState.windowStart);
                    currentState.currentLength = Math.Min(currentState.windowEnd, currentState.start + currentState.child.ComputeFullWidthIterative()) - currentState.currentStart;
                    if (currentState.currentLength >= 0)
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

                            currentState = CreateState(currentState.child, windowStart, windowLength, start);
                            goto AfterPopCurrentState;
                        }
                        else
                        {
                            if (currentState.currentLength > 0)
                            {
                                resultCollector(currentState.currentStart, currentState.currentLength, currentState.child, currentState.childType);
                            }
                        }
                    }

                    currentState.start += currentState.child.ComputeFullWidthIterative();
                }

                result = currentState.visitedCount;
            }

            return result;
        }

        private static VisitState CreateState(SyntaxNode node, int windowStart, int windowLength, int start)
        {
            return new VisitState()
            {
                node = node,
                windowLength = windowLength,
                windowStart = windowStart,
                start = start
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

                var child = node.GetSlot(i);
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
