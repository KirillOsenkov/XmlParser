using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS8602
#pragma warning disable CS8604

namespace Microsoft.Language.Xml.InternalSyntax
{
    internal abstract class GreenNode
    {
        int fullWidth;
        byte slotCount;

        NodeFlags flags;

        static readonly ConditionalWeakTable<GreenNode, DiagnosticInfo[]> diagnosticsTable =
            new ConditionalWeakTable<GreenNode, DiagnosticInfo[]>();

        static readonly ConditionalWeakTable<GreenNode, SyntaxAnnotation[]> annotationsTable =
            new ConditionalWeakTable<GreenNode, SyntaxAnnotation[]>();

        static readonly DiagnosticInfo[] s_noDiagnostics = Array.Empty<DiagnosticInfo>();
        static readonly SyntaxAnnotation[] s_noAnnotations = Array.Empty<SyntaxAnnotation>();
        static readonly IEnumerable<SyntaxAnnotation> s_noAnnotationsEnumerable = SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();

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

        protected GreenNode(SyntaxKind kind, DiagnosticInfo[]? diagnostics, SyntaxAnnotation[] annotations)
            : this(kind, 0, diagnostics, annotations)
        {
        }

        protected GreenNode(SyntaxKind kind, int fullWidth, DiagnosticInfo[]? diagnostics, SyntaxAnnotation[] annotations)
            : this(kind, fullWidth)
        {
            if (diagnostics?.Length > 0)
            {
                this.flags |= NodeFlags.ContainsDiagnostics;
                diagnosticsTable.Add(this, diagnostics);
            }
            if (annotations?.Length > 0)
            {
                foreach (var annotation in annotations)
                    if (annotation == null)
                        throw new ArgumentException(paramName: nameof(annotations), message: "Annotation cannot be null");
                this.flags |= NodeFlags.ContainsAnnotations;
                annotationsTable.Add(this, annotations);
            }
        }

        protected void AdjustWidth(GreenNode? node)
        {
            this.fullWidth += node == null ? 0 : node.fullWidth;
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

        internal abstract GreenNode? GetSlot(int index);

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
        internal virtual bool IsMissing => (flags & NodeFlags.IsMissing) != 0;

        internal virtual GreenNode? GetLeadingTrivia()
        {
            return GetFirstTerminal()?.GetLeadingTrivia();
        }

        public virtual int GetLeadingTriviaWidth()
        {
            return this.FullWidth != 0 ?
                this.GetFirstTerminal().GetLeadingTriviaWidth() :
                0;
        }

        internal virtual GreenNode? GetTrailingTrivia()
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

        public virtual GreenNode? GetLeadingTriviaCore() { return null; }
        public virtual GreenNode? GetTrailingTriviaCore() { return null; }

        internal GreenNode? GetFirstTerminal()
        {
            GreenNode? node = this;

            do
            {
                GreenNode? firstChild = null;
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

        internal GreenNode? GetLastTerminal()
        {
            GreenNode? node = this;

            do
            {
                GreenNode? lastChild = null;
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

        internal GreenNode? GetLastNonmissingTerminal()
        {
            GreenNode? node = this;

            do
            {
                GreenNode? nonmissingChild = null;
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

        public virtual GreenNode? CreateList(IEnumerable<GreenNode>? nodes, bool alwaysCreateListNode = false)
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

        internal abstract SyntaxNode CreateRed(SyntaxNode? parent, int position);

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

        public bool ContainsDiagnostics
        {
            get
            {
                return (this.flags & NodeFlags.ContainsDiagnostics) != 0;
            }
        }

        public bool ContainsAnnotations
        {
            get
            {
                return (this.flags & NodeFlags.ContainsAnnotations) != 0;
            }
        }

        #region Diagnostics
        internal DiagnosticInfo[] GetDiagnostics()
        {
            if (this.ContainsDiagnostics)
            {
                DiagnosticInfo[]? diags;
                if (diagnosticsTable.TryGetValue(this, out diags))
                {
                    return diags;
                }
            }

            return s_noDiagnostics;
        }

        internal GreenNode SetDiagnostic(DiagnosticInfo diagnostic)
        {
            return SetDiagnostics(new[] { diagnostic });
        }

        internal GreenNode AddError(DiagnosticInfo err)
        {
            DiagnosticInfo[] errorInfos;

            // If the green node already has errors, add those on.
            if (GetDiagnostics() == null)
            {
                errorInfos = new[] { err };
            }
            else
            {
                // Add the error to the error list.
                errorInfos = GetDiagnostics();
                var length = errorInfos.Length;
                Array.Resize(ref errorInfos, length + 1);
                errorInfos[length] = err;
            }

            // Get a new green node with the errors added on.
            return SetDiagnostics(errorInfos);
        }

        internal abstract GreenNode SetDiagnostics(DiagnosticInfo[]? diagnostics);
        #endregion

        #region Annotations
        public bool HasAnnotations(string annotationKind)
        {
            var annotations = this.GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return false;
            }

            foreach (var a in annotations)
            {
                if (a.Kind == annotationKind)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnnotations(IEnumerable<string> annotationKinds)
        {
            var annotations = this.GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return false;
            }

            foreach (var a in annotations)
            {
                if (annotationKinds.Contains(a.Kind))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnnotation(SyntaxAnnotation annotation)
        {
            var annotations = this.GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return false;
            }

            foreach (var a in annotations)
            {
                if (a == annotation)
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind)
        {
            if (string.IsNullOrWhiteSpace(annotationKind))
            {
                throw new ArgumentNullException(nameof(annotationKind));
            }

            var annotations = this.GetAnnotations();

            if (annotations == s_noAnnotations)
            {
                return s_noAnnotationsEnumerable;
            }

            return GetAnnotationsSlow(annotations, annotationKind);
        }

        private static IEnumerable<SyntaxAnnotation> GetAnnotationsSlow(SyntaxAnnotation[] annotations, string annotationKind)
        {
            foreach (var annotation in annotations)
            {
                if (annotation.Kind == annotationKind)
                {
                    yield return annotation;
                }
            }
        }

        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds)
        {
            if (annotationKinds == null)
            {
                throw new ArgumentNullException(nameof(annotationKinds));
            }

            var annotations = this.GetAnnotations();

            if (annotations == s_noAnnotations)
            {
                return s_noAnnotationsEnumerable;
            }

            return GetAnnotationsSlow(annotations, annotationKinds);
        }

        private static IEnumerable<SyntaxAnnotation> GetAnnotationsSlow(SyntaxAnnotation[] annotations, IEnumerable<string> annotationKinds)
        {
            foreach (var annotation in annotations)
            {
                if (annotationKinds.Contains(annotation.Kind))
                {
                    yield return annotation;
                }
            }
        }

        public SyntaxAnnotation[] GetAnnotations()
        {
            if (this.ContainsAnnotations)
            {
                SyntaxAnnotation[]? annotations;
                if (annotationsTable.TryGetValue(this, out annotations))
                {
                    System.Diagnostics.Debug.Assert(annotations.Length != 0, "we should return nonempty annotations or NoAnnotations");
                    return annotations;
                }
            }

            return s_noAnnotations;
        }

        internal abstract GreenNode SetAnnotations(SyntaxAnnotation[] annotations);

        #endregion

        internal virtual GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
        {
            return visitor.Visit(this);
        }

        #region Caching

        internal const int MaxCachedChildNum = 3;

        internal bool IsCacheable
        {
            get
            {
                return ((this.flags & NodeFlags.InheritMask) == NodeFlags.None) &&
                    this.SlotCount <= GreenNode.MaxCachedChildNum;
            }
        }

        internal int GetCacheHash()
        {
            Debug.Assert(this.IsCacheable);

            int code = (int)(this.flags) ^ (int)this.Kind;
            int cnt = this.SlotCount;
            for (int i = 0; i < cnt; i++)
            {
                var child = GetSlot(i);
                if (child != null)
                {
                    code = Hash.Combine(RuntimeHelpers.GetHashCode(child), code);
                }
            }

            return code & Int32.MaxValue;
        }

        internal bool IsCacheEquivalent(SyntaxKind kind, NodeFlags flags, GreenNode child1)
        {
            Debug.Assert(this.IsCacheable);

            return this.Kind == kind &&
                this.flags == flags &&
                this.GetSlot(0) == child1;
        }

        internal bool IsCacheEquivalent(SyntaxKind kind, NodeFlags flags, GreenNode? child1, GreenNode? child2)
        {
            Debug.Assert(this.IsCacheable);

            return this.Kind == kind &&
                this.flags == flags &&
                this.GetSlot(0) == child1 &&
                this.GetSlot(1) == child2;
        }

        internal bool IsCacheEquivalent(SyntaxKind kind, NodeFlags flags, GreenNode child1, GreenNode child2, GreenNode child3)
        {
            Debug.Assert(this.IsCacheable);

            return this.Kind == kind &&
                this.flags == flags &&
                this.GetSlot(0) == child1 &&
                this.GetSlot(1) == child2 &&
                this.GetSlot(2) == child3;
        }
        #endregion //Caching
    }
}
