using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Language.Xml.InternalSyntax
{
    abstract class SyntaxList : GreenNode
    {
        internal SyntaxList()
            : base(SyntaxKind.List)
        {
        }

        internal override bool IsList => true;

        internal static GreenNode List(GreenNode child)
        {
            return child;
        }

        internal static WithTwoChildren List(GreenNode child0, GreenNode child1)
        {
            Debug.Assert(child0 != null);
            Debug.Assert(child1 != null);

            var result = new WithTwoChildren(child0, child1);
            return result;
        }

        internal static WithThreeChildren List(GreenNode child0, GreenNode child1, GreenNode child2)
        {
            Debug.Assert(child0 != null);
            Debug.Assert(child1 != null);
            Debug.Assert(child2 != null);

            var result = new WithThreeChildren(child0, child1, child2);
            return result;
        }

        internal static GreenNode List(GreenNode[] nodes)
        {
            return List(nodes, nodes.Length);
        }

        internal static GreenNode List(GreenNode[] nodes, int count)
        {
            var array = new ArrayElement<GreenNode>[count];
            for (int i = 0; i < count; i++)
            {
                Debug.Assert(nodes[i] != null);
                array[i].Value = nodes[i];
            }

            return List(array);
        }

        internal static SyntaxList List(ArrayElement<GreenNode>[] children)
        {
            // "WithLotsOfChildren" list will allocate a separate array to hold
            // precomputed node offsets. It may not be worth it for smallish lists.
            if (children.Length < 10)
            {
                return new WithManyChildren(children);
            }
            else
            {
                return new WithLotsOfChildren(children);
            }
        }

        internal abstract void CopyTo(ArrayElement<GreenNode>[] array, int offset);

        internal static GreenNode Concat(GreenNode left, GreenNode right)
        {
            if (left == null)
            {
                return right;
            }

            if (right == null)
            {
                return left;
            }

            var leftList = left as SyntaxList;
            var rightList = right as SyntaxList;
            if (leftList != null)
            {
                if (rightList != null)
                {
                    var tmp = new ArrayElement<GreenNode>[left.SlotCount + right.SlotCount];
                    leftList.CopyTo(tmp, 0);
                    rightList.CopyTo(tmp, left.SlotCount);
                    return List(tmp);
                }
                else
                {
                    var tmp = new ArrayElement<GreenNode>[left.SlotCount + 1];
                    leftList.CopyTo(tmp, 0);
                    tmp[left.SlotCount].Value = right;
                    return List(tmp);
                }
            }
            else if (rightList != null)
            {
                var tmp = new ArrayElement<GreenNode>[rightList.SlotCount + 1];
                tmp[0].Value = left;
                rightList.CopyTo(tmp, 1);
                return List(tmp);
            }
            else
            {
                return List(left, right);
            }
        }

        internal class WithTwoChildren : SyntaxList
        {
            private readonly GreenNode _child0;
            private readonly GreenNode _child1;

            internal WithTwoChildren(GreenNode child0, GreenNode child1)
            {
                this.SlotCount = 2;
                this.AdjustWidth(child0);
                _child0 = child0;
                this.AdjustWidth(child1);
                _child1 = child1;
            }

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    default:
                        return null;
                }
            }

            internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
            {
                array[offset].Value = _child0;
                array[offset + 1].Value = _child1;
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
            {
                return new Xml.SyntaxList.WithTwoChildren(this, parent, position);
            }
        }

        internal class WithThreeChildren : SyntaxList
        {
            private readonly GreenNode _child0;
            private readonly GreenNode _child1;
            private readonly GreenNode _child2;

            internal WithThreeChildren(GreenNode child0, GreenNode child1, GreenNode child2)
            {
                this.SlotCount = 3;
                this.AdjustWidth(child0);
                _child0 = child0;
                this.AdjustWidth(child1);
                _child1 = child1;
                this.AdjustWidth(child2);
                _child2 = child2;
            }

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    case 2:
                        return _child2;
                    default:
                        return null;
                }
            }

            internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
            {
                array[offset].Value = _child0;
                array[offset + 1].Value = _child1;
                array[offset + 2].Value = _child2;
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
            {
                return new Xml.SyntaxList.WithThreeChildren(this, parent, position);
            }
        }

        internal abstract class WithManyChildrenBase : SyntaxList
        {
            internal readonly ArrayElement<GreenNode>[] children;

            internal WithManyChildrenBase(ArrayElement<GreenNode>[] children)
            {
                this.children = children;
                this.InitializeChildren();
            }

            private void InitializeChildren()
            {
                int n = children.Length;
                if (n < byte.MaxValue)
                {
                    this.SlotCount = (byte)n;
                }
                else
                {
                    this.SlotCount = byte.MaxValue;
                }

                for (int i = 0; i < children.Length; i++)
                {
                    this.AdjustWidth(children[i]);
                }
            }

            protected override int GetSlotCount()
            {
                return children.Length;
            }

            internal override GreenNode GetSlot(int index)
            {
                return this.children[index];
            }

            internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
            {
                Array.Copy(this.children, 0, array, offset, this.children.Length);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
            {
                return new Xml.SyntaxList.WithManyChildren(this, parent, position);
            }
        }

        internal sealed class WithManyChildren : WithManyChildrenBase
        {
            internal WithManyChildren(ArrayElement<GreenNode>[] children)
                : base(children)
            {
            }
        }

        internal sealed class WithLotsOfChildren : WithManyChildrenBase
        {
            private readonly int[] _childOffsets;

            internal WithLotsOfChildren(ArrayElement<GreenNode>[] children)
                : base(children)
            {
                _childOffsets = CalculateOffsets(children);
            }

            public override int GetSlotOffset(int index)
            {
                return _childOffsets[index];
            }

            /// <summary>
            /// Find the slot that contains the given offset.
            /// </summary>
            /// <param name="offset">The target offset. Must be between 0 and <see cref="GreenNode.FullWidth"/>.</param>
            /// <returns>The slot index of the slot containing the given offset.</returns>
            /// <remarks>
            /// This implementation uses a binary search to find the first slot that contains
            /// the given offset.
            /// </remarks>
            public override int FindSlotIndexContainingOffset(int offset)
            {
                Debug.Assert(offset >= 0 && offset < FullWidth);
                return _childOffsets.BinarySearchUpperBound(offset) - 1;
            }

            private static int[] CalculateOffsets(ArrayElement<GreenNode>[] children)
            {
                int n = children.Length;
                var childOffsets = new int[n];
                int offset = 0;
                for (int i = 0; i < n; i++)
                {
                    childOffsets[i] = offset;
                    offset += children[i].Value.FullWidth;
                }
                return childOffsets;
            }
        }
    }
}
