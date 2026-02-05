using System;

namespace Microsoft.Language.Xml
{
    /// <summary>
    /// Abstract text buffer
    /// </summary>
    public abstract class Buffer
    {
        public abstract int Length { get; }

        public abstract char this[int index] { get; }

        public abstract string GetText(int start, int length);

        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{Char}"/> over a portion of the buffer.
        /// The default implementation copies into a temporary array; subclasses
        /// backed by contiguous memory (e.g. <see cref="StringBuffer"/>) should
        /// override this to avoid the copy.
        /// </summary>
        public virtual ReadOnlySpan<char> GetSpan(int start, int length)
        {
            var tmp = new char[length];
            CopyTo(start, tmp, 0, length);
            return tmp;
        }
    }
}
