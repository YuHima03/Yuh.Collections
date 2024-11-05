using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yuh.Collections.Helpers;

namespace Yuh.Collections
{
    public ref struct SpanSequence<TElement, TSegmentList>()
#if NET9_0_OR_GREATER
        : IEnumerable<TElement>, IReadOnlyCollection<TElement>, IReadOnlyList<TElement>
        where TSegmentList allows ref struct
#endif
        where TSegmentList : IRefList<TElement>
    {
        private readonly Span<int> _countBefore = [];

        private readonly int _indexOffset = 0;

        private readonly TSegmentList _segments;

        public readonly int Length = 0;

        public static SpanSequence<TElement, TSegmentList> Empty => default;

        public readonly ref TElement this[int index]
        {
            get
            {
                if (Length <= (uint)index)
                {
                    ThrowHelpers.ThrowIndexOutOfRangeException();
                }
                var (si, ii) = GetLocation(index);
                return ref Unsafe.Add(ref MemoryMarshal.GetReference(_segments[si]), ii); // _segments[si][ii]
            }
        }

        public SpanSequence(TSegmentList segments, Span<int> countBefore) : this()
        {
            if (segments.Count != countBefore.Length)
            {
                ThrowHelpers.ThrowArgumentException("The number of elements of two collections must be equal.", string.Join(',', nameof(segments), nameof(countBefore)));
            }
            _countBefore = countBefore;
            _segments = segments;

            if (!_countBefore.IsEmpty)
            {
                _indexOffset = MemoryMarshal.GetReference(_countBefore); // _countBefore[0]
            }
        }

        /// <summary>
        /// Returns a pair of index that indicates the element at the specified index in the collection.
        /// </summary>
        /// <remarks>
        /// <paramref name="index"/> must be valid index of the collection.
        /// </remarks>
        /// <param name="index"></param>
        /// <returns></returns>
        private readonly (int SegmentIndex, int ItemIndex) GetLocation(int index)
        {
            index += _indexOffset;
            var countBefore = _countBefore;
            var segmentCount = countBefore.Length;

            var countBeforeLast = countBefore.Last();
            if (countBeforeLast <= index)
            {
                return (segmentCount - 1, index - countBeforeLast);
            }

            int l = 0, r = segmentCount - 1; // countBefore[l] <= index < countBefore[r]
            while (l + 1 != r)
            {
                int m = (l + r) >> 1;
                var cb = countBefore.UnsafeAccess(m);
                if (cb <= index)
                {
                    l = cb;
                }
                else
                {
                    r = cb;
                }
            }

            return (l, index - countBefore.UnsafeAccess(l));
        }
    }
}
