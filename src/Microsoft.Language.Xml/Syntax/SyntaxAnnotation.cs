using System;
using System.Diagnostics;

namespace Microsoft.Language.Xml
{
    /// <summary>
    /// A SyntaxAnnotation is used to annotate syntax elements with additional information.
    ///
    /// Since syntax elements are immutable, annotating them requires creating new instances of them
    /// with the annotations attached.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class SyntaxAnnotation : IEquatable<SyntaxAnnotation>
    {
        // use a value identity instead of object identity so a deserialized instance matches the original instance.
        private readonly long _id;
        private static long s_nextId;

        // use a value identity instead of object identity so a deserialized instance matches the original instance.
        public string? Kind { get; }
        public string? Data { get; }

        public SyntaxAnnotation()
        {
            _id = System.Threading.Interlocked.Increment(ref s_nextId);
        }

        public SyntaxAnnotation(string kind)
            : this()
        {
            this.Kind = kind;
        }

        public SyntaxAnnotation(string kind, string data)
            : this(kind)
        {
            this.Data = data;
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("Annotation: Kind='{0}' Data='{1}'", this.Kind ?? "", this.Data ?? "");
        }

        public bool Equals(SyntaxAnnotation? other)
        {
            return (object?)other != null && _id == other._id;
        }

        public static bool operator ==(SyntaxAnnotation? left, SyntaxAnnotation? right)
        {
            if ((object?)left == (object?)right)
            {
                return true;
            }

            if ((object?)left == null || (object?)right == null)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(SyntaxAnnotation? left, SyntaxAnnotation? right)
        {
            if ((object?)left == (object?)right)
            {
                return false;
            }

            if ((object?)left == null || (object?)right == null)
            {
                return true;
            }

            return !left.Equals(right);
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as SyntaxAnnotation);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
}
