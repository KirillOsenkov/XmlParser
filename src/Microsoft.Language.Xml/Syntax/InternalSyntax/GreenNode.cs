using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Language.Xml.InternalSyntax
{
    internal abstract class GreenNode
    {
        int fullWidth;
        byte slotCount;

        internal int FullWidth => fullWidth;
        internal SyntaxKind Kind { get; }

        protected GreenNode(SyntaxKind kind)
        {
            Kind = kind;
        }

        protected GreenNode(SyntaxKind kind, int fullWidth)
            : this(kind)
        {
            if (fullWidth == -1)
                throw new InvalidOperationException();
            this.fullWidth = fullWidth;
        }

        protected void AdjustWidth(GreenNode node)
        {
            this.fullWidth += (node?.fullWidth).GetValueOrDefault();
        }

        public int SlotCount
        {
            get
            {
                int count = slotCount;
                if (count == byte.MaxValue)
                {
                    count = GetSlotCount();
                }

                return count;
            }

            protected set
            {
                slotCount = (byte)value;
            }
        }

        internal abstract GreenNode GetSlot(int index);

        // for slot counts >= byte.MaxValue
        protected virtual int GetSlotCount()
        {
            return slotCount;
        }

        public virtual int GetSlotOffset(int index)
        {
            int offset = 0;
            for (int i = 0; i < index; i++)
            {
                var child = GetSlot(i);
                if (child != null)
                    offset += child.FullWidth;
            }

            return offset;
        }

        public virtual int FindSlotIndexContainingOffset(int offset)
        {
            Debug.Assert(0 <= offset && offset < FullWidth);

            int i;
            int accumulatedWidth = 0;
            for (i = 0; ; i++)
            {
                Debug.Assert(i < SlotCount);
                var child = GetSlot(i);
                if (child != null)
                {
                    accumulatedWidth += child.FullWidth;
                    if (offset < accumulatedWidth)
                    {
                        break;
                    }
                }
            }

            return i;
        }

        public virtual int Width
        {
            get
            {
                return FullWidth - (GetLeadingTriviaWidth() + GetTrailingTriviaWidth());
            }
        }

        internal virtual bool IsList => false;
        internal virtual bool IsToken => false;
        internal virtual bool IsMissing => false;

        internal virtual GreenNode GetLeadingTrivia()
        {
            return GetFirstTerminal()?.GetLeadingTrivia();
        }

        public virtual int GetLeadingTriviaWidth()
        {
            return this.FullWidth != 0 ?
                this.GetFirstTerminal().GetLeadingTriviaWidth() :
                0;
        }

        internal virtual GreenNode GetTrailingTrivia()
        {
            return GetLastTerminal()?.GetTrailingTrivia();
        }

        public virtual int GetTrailingTriviaWidth()
        {
            return this.FullWidth != 0 ?
                this.GetLastTerminal().GetTrailingTriviaWidth() :
                0;
        }

        public bool HasLeadingTrivia
        {
            get
            {
                return this.GetLeadingTriviaWidth() != 0;
            }
        }

        public bool HasTrailingTrivia
        {
            get
            {
                return this.GetTrailingTriviaWidth() != 0;
            }
        }

        public virtual GreenNode GetLeadingTriviaCore() { return null; }
        public virtual GreenNode GetTrailingTriviaCore() { return null; }

        internal GreenNode GetFirstTerminal()
        {
            GreenNode node = this;

            do
            {
                GreenNode firstChild = null;
                for (int i = 0, n = node.SlotCount; i < n; i++)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        firstChild = child;
                        break;
                    }
                }
                node = firstChild;
            } while (node?.SlotCount > 0);

            return node;
        }

        internal GreenNode GetLastTerminal()
        {
            GreenNode node = this;

            do
            {
                GreenNode lastChild = null;
                for (int i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        lastChild = child;
                        break;
                    }
                }
                node = lastChild;
            } while (node?.SlotCount > 0);

            return node;
        }

        internal GreenNode GetLastNonmissingTerminal()
        {
            GreenNode node = this;

            do
            {
                GreenNode nonmissingChild = null;
                for (int i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        nonmissingChild = child;
                        break;
                    }
                }
                node = nonmissingChild;
            }
            while (node?.SlotCount > 0);

            return node;
        }

        public virtual GreenNode CreateList(IEnumerable<GreenNode> nodes, bool alwaysCreateListNode = false)
        {
            if (nodes == null)
            {
                return null;
            }

            var list = nodes.ToArray();

            switch (list.Length)
            {
                case 0:
                    return null;
                case 1:
                    if (alwaysCreateListNode)
                    {
                        goto default;
                    }
                    else
                    {
                        return list[0];
                    }
                case 2:
                    return SyntaxList.List(list[0], list[1]);
                case 3:
                    return SyntaxList.List(list[0], list[1], list[2]);
                default:
                    return SyntaxList.List(list);
            }
        }

        public SyntaxNode CreateRed()
        {
            return CreateRed(null, 0);
        }

        internal abstract SyntaxNode CreateRed(SyntaxNode parent, int position);

        public virtual string ToFullString()
        {
            var sb = PooledStringBuilder.GetInstance();
            var writer = new System.IO.StringWriter(sb.Builder, System.Globalization.CultureInfo.InvariantCulture);
            this.WriteTo(writer);
            return sb.ToStringAndFree();
        }

        public virtual void WriteTo(TextWriter writer)
        {
            var stack = new Stack<GreenNode>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                stack.Pop().WriteToOrFlatten(writer, stack);
            }
        }

        /*  <summary>
        ''' NOTE: the method should write OR push children, but never do both
        ''' </summary>
        */
        internal virtual void WriteToOrFlatten(TextWriter writer, Stack<GreenNode> stack)
        {
            // By default just push children to the stack
            for (var i = this.SlotCount - 1; i >= 0; i--)
            {
                var node = GetSlot(i);
                if (node != null)
                {
                    stack.Push(GetSlot(i));
                }
            }
        }

        internal DiagnosticInfo[] GetDiagnostics()
        {
            // TODO
            return Array.Empty<DiagnosticInfo>();
            /*if (this.ContainsDiagnostics) {
				DiagnosticInfo[] diags;
				if (s_diagnosticsTable.TryGetValue (this, out diags)) {
					return diags;
				}
			}

			return s_noDiagnostics;*/
        }

        internal GreenNode SetDiagnostic(DiagnosticInfo diagnostic)
        {
            return SetDiagnostics(new[] { diagnostic });
        }

        internal virtual GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
        {
            // TODO
            return this;
        }

        internal GreenNode AddError(DiagnosticInfo err)
        {
            // TODO
            return this;
        }

        internal virtual GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
