using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Language.Xml
{
    public abstract class SyntaxList : SyntaxNode
    {
        protected SyntaxList() : base(SyntaxKind.List)
        {
        }

        internal static SyntaxNode List(SyntaxNode child)
        {
            return child;
        }

        internal static WithTwoChildren List(SyntaxNode child0, SyntaxNode child1)
        {
            var result = new WithTwoChildren(child0, child1);
            return result;
        }

        internal static WithThreeChildren List(SyntaxNode child0, SyntaxNode child1, SyntaxNode child2)
        {
            var result = new WithThreeChildren(child0, child1, child2);
            return result;
        }

        internal static SyntaxList List(ArrayElement<SyntaxNode>[] nodes)
        {
            // "WithLotsOfChildren" list will alocate a separate array to hold
            // precomputed node offsets. It may not be worth it for smallish lists.
            if (nodes.Length < 10)
            {
                return new WithManyChildren(nodes);
            }
            else
            {
                return new WithLotsOfChildren(nodes);
            }
        }

        internal static SyntaxList List(SyntaxNode[] nodes)
        {
            return List(nodes, nodes.Length);
        }

        internal static SyntaxList List(SyntaxNode[] nodes, int count)
        {
            var array = new ArrayElement<SyntaxNode>[count];
            Debug.Assert(array.Length == count);
            for (var i = 0; i < count; i++)
            {
                array[i].Value = nodes[i];
                Debug.Assert(array[i].Value != null);
            }

            return List(array);
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.Visit(this);
        }

        internal abstract void CopyTo(ArrayElement<SyntaxNode>[] array, int offset);

        internal static SyntaxNode Concat(SyntaxNode left, SyntaxNode right)
        {
            if ((left == null))
            {
                return right;
            }

            if ((right == null))
            {
                return left;
            }

            ArrayElement<SyntaxNode>[] tmp;
            SyntaxList leftList = (left as SyntaxList);
            SyntaxList rightList = (right as SyntaxList);
            if (leftList != null)
            {
                if (rightList != null)
                {
                    tmp = new ArrayElement<SyntaxNode>[left.SlotCount + right.SlotCount];
                    leftList.CopyTo(tmp, 0);
                    rightList.CopyTo(tmp, left.SlotCount);
                    return SyntaxList.List(tmp);
                }

                tmp = new ArrayElement<SyntaxNode>[(left.SlotCount + 1)];
                leftList.CopyTo(tmp, 0);
                tmp[left.SlotCount].Value = right;
                return SyntaxList.List(tmp);
            }

            if (rightList != null)
            {
                tmp = new ArrayElement<SyntaxNode>[(rightList.SlotCount + 1)];
                tmp[0].Value = left;
                rightList.CopyTo(tmp, 1);
                return SyntaxList.List(tmp);
            }

            return SyntaxList.List(left, right);
        }

        internal sealed class WithTwoChildren : SyntaxList
        {
            private SyntaxNode _child0;
            private SyntaxNode _child1;
            internal WithTwoChildren(SyntaxNode child0, SyntaxNode child1) : base()
            {
                base.SlotCount = 2;
                this._child0 = child0;
                this._child1 = child1;
            }

            public override SyntaxNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return this._child0;
                    case 1:
                        return this._child1;
                }

                return null;
            }

            internal override void CopyTo(ArrayElement<SyntaxNode>[] array, int offset)
            {
                array[offset].Value = _child0;
                array[offset + 1].Value = _child1;
            }
        }

        internal sealed class WithThreeChildren : SyntaxList
        {
            private SyntaxNode _child0;
            private SyntaxNode _child1;
            private SyntaxNode _child2;
            internal WithThreeChildren(SyntaxNode child0, SyntaxNode child1, SyntaxNode child2) : base()
            {
                base.SlotCount = 3;
                this._child0 = child0;
                this._child1 = child1;
                this._child2 = child2;
            }

            public override SyntaxNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return this._child0;
                    case 1:
                        return this._child1;
                    case 2:
                        return this._child2;
                }

                return null;
            }

            internal override void CopyTo(ArrayElement<SyntaxNode>[] array, int offset)
            {
                array[offset].Value = _child0;
                array[offset + 1].Value = _child1;
                array[offset + 2].Value = _child2;
            }
        }

        internal abstract class
            WithManyChildrenBase : SyntaxList
        {
            protected readonly ArrayElement<SyntaxNode>[] _children;
            internal WithManyChildrenBase(ArrayElement<SyntaxNode>[] children) : base()
            {
                this._children = children;
                InitChildren();
            }

            private void InitChildren()
            {
                var n = _children.Length;
                if ((n < byte.MaxValue))
                {
                    this.SlotCount = ((byte)n);
                }
                else
                {
                    this.SlotCount = byte.MaxValue;
                }
            }

            protected override int GetSlotCount()
            {
                return this._children.Length;
            }

            public override SyntaxNode GetSlot(int index)
            {
                return this._children[index];
            }

            internal override void CopyTo(ArrayElement<SyntaxNode>[] array, int offset)
            {
                Array.Copy(this._children, 0, array, offset, this._children.Length);
            }
        }

        internal sealed class WithManyChildren : WithManyChildrenBase
        {
            internal WithManyChildren(ArrayElement<SyntaxNode>[] children) : base(children)
            {
            }
        }

        internal sealed class WithLotsOfChildren : WithManyChildrenBase
        {
            private readonly int[] _childOffsets;
            internal WithLotsOfChildren(ArrayElement<SyntaxNode>[] children) : base(children)
            {
                _childOffsets = CalculateOffsets(children);
            }

            private int[] CalculateOffsets(ArrayElement<SyntaxNode>[] children)
            {
                var n = children.Length;
                var childOffsets = new int[n];
                var offset = 0;
                for (var i = 0; i < n; i++)
                {
                    childOffsets[i] = offset;
                    offset += children[i].Value.FullWidth;
                }

                this.FullWidth = offset;

                return childOffsets;
            }

            public override void GetIndexAndOffset(int targetOffset, out int index, out int offset)
            {
                if (targetOffset >= FullWidth)
                {
                    index = _childOffsets.Length;
                    offset = FullWidth;
                }

                index = BinarySearch(_childOffsets, targetOffset);
                if (index < 0)
                {
                    index = ~index;

                    if (index != 0)
                    {
                        index--;
                        offset = _childOffsets[index];
                    }
                    else
                    {
                        offset = 0;
                    }
                }
                else
                {
                    offset = _childOffsets[index];
                }
            }

            // same as Array.BinarySearch, but without using IComparer to compare ints
            internal static int BinarySearch(int[] array, int value)
            {
                var low = 0;
                var high = array.Length - 1;

                while (low <= high)
                {
                    var middle = low + ((high - low) >> 1);
                    var midValue = array[middle];

                    if (midValue == value)
                    {
                        return middle;
                    }
                    else if (midValue > value)
                    {
                        high = middle - 1;
                    }
                    else
                    {
                        low = middle + 1;
                    }
                }

                return ~low;
            }
        }
    }
}
