using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

#pragma warning disable CS8602

namespace Microsoft.Language.Xml
{
    /// <summary>
    /// This is basically a lossy cache of strings that is searchable by
    /// strings, string sub ranges, character array ranges or string-builder.
    /// </summary>
    internal class StringTable
    {
        // entry in the caches
        private struct Entry
        {
            // hash code of the entry
            public int HashCode;

            // full text of the item
            public string Text;
        }

        // TODO: Need to tweak the size with more scenarios.
        //       for now this is what works well enough with
        //       Roslyn C# compiler project

        // Size of local cache.
        private const int LocalSizeBits = 10;
        private const int LocalSize = (1 << LocalSizeBits);
        private const int LocalSizeMask = LocalSize - 1;

        // max size of shared cache.
        private const int SharedSizeBits = 13;
        private const int SharedSize = (1 << SharedSizeBits);
        private const int SharedSizeMask = SharedSize - 1;

        // size of bucket in shared cache. (local cache has bucket size 1).
        private const int SharedBucketBits = 4;
        private const int SharedBucketSize = (1 << SharedBucketBits);
        private const int SharedBucketSizeMask = SharedBucketSize - 1;

        // local (L1) cache
        // simple fast and not threadsafe cache
        // with lmited size and "last add wins" expiration policy
        //
        // The main purpose of the local cache is to use in long lived
        // single threaded operations with lots of locality (like parsing).
        // Local cache is smaller (and thus faster) and is not affected
        // by cache misses on other threads.
        private readonly Entry[] localTable = new Entry[LocalSize];

        // shared (L2) threadsafe cache
        // slightly slower than local cache
        // we read this cache when having a miss in local cache
        // writes to local cache will update shared cache as well.
        private static Entry[] sharedTable = new Entry[SharedSize];

        // essentially a random number
        // the usage pattern will randomly use and increment this
        // the counter is not static to avoid interlocked operations and cross-thread traffic
        private int localRandom = Environment.TickCount;

        // same asabove but for users that go directly with unbuffered shared cache.
        private static int sharedRandom = Environment.TickCount;

        internal StringTable() :
            this(null)
        {
        }

        // implement Poolable object pattern
        #region "Poolable"

        private StringTable(ObjectPool<StringTable>? pool)
        {
            this.pool = pool;
        }

        private readonly ObjectPool<StringTable>? pool;
        private static readonly ObjectPool<StringTable> StaticPool = CreatePool();

        private static ObjectPool<StringTable> CreatePool()
        {
            ObjectPool<StringTable>? pool = null;
            pool = new ObjectPool<StringTable>(() => new StringTable(pool), Environment.ProcessorCount * 2);
            return pool;
        }

        public static StringTable GetInstance()
        {
            return StaticPool.Allocate();
        }

        public void Free()
        {
            // leave cache content in the cache, just return it to the pool
            // Array.Clear(this.localTable, 0, this.localTable.Length);
            // Array.Clear(sharedTable, 0, sharedTable.Length);

            pool.Free(this);
        }

        #endregion // Poolable

        internal string Add(char[] chars, int start, int len)
        {
            var hashCode = Hash.GetFNVHashCode(chars, start, len);

            // capture array to avoid extra range checks
            var arr = localTable;
            var idx = LocalIdxFromHash(hashCode);

            var text = arr[idx].Text;

            if (text != null && arr[idx].HashCode == hashCode)
            {
                var result = arr[idx].Text;
                if (StringTable.TextEquals(result, chars, start, len))
                {
                    return result;
                }
            }

            string? shared = FindSharedEntry(chars, start, len, hashCode);
            if (shared != null)
            {
                // PERF: the following code does elementwise assignment of a struct
                //       because current JIT produces better code compared to
                //       arr[idx] = new Entry(...)
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;

                return shared;
            }

            return AddItem(chars, start, len, hashCode);
        }

        internal string Add(string chars, int start, int len)
        {
            var hashCode = Hash.GetFNVHashCode(chars, start, len);

            // capture array to avoid extra range checks
            var arr = localTable;
            var idx = LocalIdxFromHash(hashCode);

            var text = arr[idx].Text;

            if (text != null && arr[idx].HashCode == hashCode)
            {
                var result = arr[idx].Text;
                if (StringTable.TextEquals(result, chars, start, len))
                {
                    return result;
                }
            }

            string? shared = FindSharedEntry(chars, start, len, hashCode);
            if (shared != null)
            {
                // PERF: the following code does elementwise assignment of a struct
                //       because current JIT produces better code compared to
                //       arr[idx] = new Entry(...)
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;

                return shared;
            }

            return AddItem(chars, start, len, hashCode);
        }

        internal string Add(char chars)
        {
            var hashCode = Hash.GetFNVHashCode(chars);

            // capture array to avoid extra range checks
            var arr = localTable;
            var idx = LocalIdxFromHash(hashCode);

            var text = arr[idx].Text;

            if (text != null)
            {
                var result = arr[idx].Text;
                if (text.Length == 1 && text[0] == chars)
                {
                    return result;
                }
            }

            string? shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                // PERF: the following code does elementwise assignment of a struct
                //       because current JIT produces better code compared to
                //       arr[idx] = new Entry(...)
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;

                return shared;
            }

            return AddItem(chars, hashCode);
        }

        internal string Add(StringBuilder chars)
        {
            var hashCode = Hash.GetFNVHashCode(chars);

            // capture array to avoid extra range checks
            var arr = localTable;
            var idx = LocalIdxFromHash(hashCode);

            var text = arr[idx].Text;

            if (text != null && arr[idx].HashCode == hashCode)
            {
                var result = arr[idx].Text;
                if (StringTable.TextEquals(result, chars))
                {
                    return result;
                }
            }

            string? shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                // PERF: the following code does elementwise assignment of a struct
                //       because current JIT produces better code compared to
                //       arr[idx] = new Entry(...)
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;

                return shared;
            }

            return AddItem(chars, hashCode);
        }

        internal string Add(string chars)
        {
            var hashCode = Hash.GetFNVHashCode(chars);

            // capture array to avoid extra range checks
            var arr = localTable;
            var idx = LocalIdxFromHash(hashCode);

            var text = arr[idx].Text;

            if (text != null && arr[idx].HashCode == hashCode)
            {
                var result = arr[idx].Text;
                if (result == chars)
                {
                    return result;
                }
            }

            string? shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                // PERF: the following code does elementwise assignment of a struct
                //       because current JIT produces better code compared to
                //       arr[idx] = new Entry(...)
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;

                return shared;
            }

            AddCore(chars, hashCode);
            return chars;
        }

        private static string? FindSharedEntry(char[] chars, int start, int len, int hashCode)
        {
            var arr = sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            string? e = null;
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;

                if (e != null)
                {
                    if (hash == hashCode && TextEquals(e, chars, start, len))
                    {
                        break;
                    }

                    // this is not e we are looking for
                    e = null;
                }
                else
                {
                    // once we see unfilled entry, the rest of the bucket will be empty
                    break;
                }

                idx = (idx + i) & SharedSizeMask;
            }

            return e;
        }

        private static string? FindSharedEntry(string chars, int start, int len, int hashCode)
        {
            var arr = sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            string? e = null;
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;

                if (e != null)
                {
                    if (hash == hashCode && TextEquals(e, chars, start, len))
                    {
                        break;
                    }

                    // this is not e we are looking for
                    e = null;
                }
                else
                {
                    // once we see unfilled entry, the rest of the bucket will be empty
                    break;
                }

                idx = (idx + i) & SharedSizeMask;
            }

            return e;
        }

        private static string? FindSharedEntry(char chars, int hashCode)
        {
            var arr = sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            string? e = null;
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;

                if (e != null)
                {
                    if (e.Length == 1 && e[0] == chars)
                    {
                        break;
                    }

                    // this is not e we are looking for
                    e = null;
                }
                else
                {
                    // once we see unfilled entry, the rest of the bucket will be empty
                    break;
                }

                idx = (idx + i) & SharedSizeMask;
            }

            return e;
        }

        private static string? FindSharedEntry(StringBuilder chars, int hashCode)
        {
            var arr = sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            string? e = null;
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;

                if (e != null)
                {
                    if (hash == hashCode && TextEquals(e, chars))
                    {
                        break;
                    }

                    // this is not e we are looking for
                    e = null;
                }
                else
                {
                    // once we see unfilled entry, the rest of the bucket will be empty
                    break;
                }

                idx = (idx + i) & SharedSizeMask;
            }

            return e;
        }

        private static string? FindSharedEntry(string chars, int hashCode)
        {
            var arr = sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            string? e = null;
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;

                if (e != null)
                {
                    if (hash == hashCode && e == chars)
                    {
                        break;
                    }

                    // this is not e we are looking for
                    e = null;
                }
                else
                {
                    // once we see unfilled entry, the rest of the bucket will be empty
                    break;
                }

                idx = (idx + i) & SharedSizeMask;
            }

            return e;
        }

        private string AddItem(char[] chars, int start, int len, int hashCode)
        {
            var text = new String(chars, start, len);
            AddCore(text, hashCode);
            return text;
        }

        private string AddItem(string chars, int start, int len, int hashCode)
        {
            var text = chars.Substring(start, len);
            AddCore(text, hashCode);
            return text;

        }

        private string AddItem(char chars, int hashCode)
        {
            var text = new String(chars, 1);
            AddCore(text, hashCode);
            return text;
        }

        private string AddItem(StringBuilder chars, int hashCode)
        {
            var text = chars.ToString();
            AddCore(text, hashCode);
            return text;
        }

        private void AddCore(string chars, int hashCode)
        {
            // add to the shared table first (in case someone looks for same item)
            AddSharedEntry(hashCode, chars);

            // add to the local table too
            var arr = localTable;
            var idx = LocalIdxFromHash(hashCode);
            arr[idx].HashCode = hashCode;
            arr[idx].Text = chars;
        }

        private void AddSharedEntry(int hashCode, string text)
        {
            var arr = sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            // try finding an empty spot in the bucket
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            int curIdx = idx;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                if (arr[curIdx].Text == null)
                {
                    idx = curIdx;
                    goto foundIdx;
                }

                curIdx = (curIdx + i) & SharedSizeMask;
            }

            // or pick a random victim within the bucket range
            // and replace with new entry
            var i1 = LocalNextRandom() & SharedBucketSizeMask;
            idx = (idx + ((i1 * i1 + i1) / 2)) & SharedSizeMask;

        foundIdx:
            arr[idx].HashCode = hashCode;
            Volatile.Write(ref arr[idx].Text, text);
        }

        internal static string AddShared(StringBuilder chars)
        {
            var hashCode = Hash.GetFNVHashCode(chars);

            string? shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                return shared;
            }

            return AddSharedSlow(hashCode, chars);
        }

        private static string AddSharedSlow(int hashCode, StringBuilder builder)
        {
            var text = builder.ToString();
            var arr = sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            // try finding an empty spot in the bucket
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            int curIdx = idx;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                if (arr[curIdx].Text == null)
                {
                    idx = curIdx;
                    goto foundIdx;
                }

                curIdx = (curIdx + i) & SharedSizeMask;
            }

            // or pick a random victim within the bucket range
            // and replace with new entry
            var i1 = SharedNextRandom() & SharedBucketSizeMask;
            idx = (idx + ((i1 * i1 + i1) / 2)) & SharedSizeMask;

        foundIdx:
            arr[idx].HashCode = hashCode;
            Volatile.Write(ref arr[idx].Text, text);

            return text;
        }

        private static int LocalIdxFromHash(int hash)
        {
            return hash & LocalSizeMask;
        }

        private static int SharedIdxFromHash(int hash)
        {
            // we can afford to mix some more hash bits here
            return (hash ^ (hash >> LocalSizeBits)) & SharedSizeMask;
        }

        private int LocalNextRandom()
        {
            return this.localRandom++;
        }

        private static int SharedNextRandom()
        {
            return Interlocked.Increment(ref StringTable.sharedRandom);
        }

        internal static bool TextEquals(string array, string text) => TextEquals(array, text, 0, text.Length);

        internal static bool TextEquals(string array, string text, int start, int length)
        {
            if (array.Length != length)
            {
                return false;
            }

            // use array.Length to eliminate the rangecheck
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] != text[start + i])
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TextEquals(string array, StringBuilder text)
        {
            if (array.Length != text.Length)
            {
                return false;
            }

            // interestingly, stringbuilder holds the list of chunks by the tail
            // so accessing positions at the beginning may cost more than those at the end.
            for (var i = array.Length - 1; i >= 0; i--)
            {
                if (array[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TextEquals(string array, char[] text, int start, int length)
        {
            return array.Length == length && TextEqualsCore(array, text, start);
        }

        private static bool TextEqualsCore(string array, char[] text, int start)
        {
            // use array.Length to eliminate the rangecheck
            int s = start;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] != text[s])
                {
                    return false;
                }
                s++;
            }

            return true;
        }
    }
}