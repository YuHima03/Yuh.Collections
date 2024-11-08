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

        private readonly int _segmentOffset = 0;

        private readonly TSegmentList _segments;

        public readonly int Length = 0;

        public static SpanSequence<TElement, TSegmentList> Empty => default;

        public readonly ref TElement this[Index index] => ref this[index.GetOffset(Length)];

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

        public readonly SpanSequence<TElement, TSegmentList> this[Range range]
        {
            get
            {
                var (offset, length) = range.GetOffsetAndLength(Length);
                return SliceInternal(offset, length);
            }
        }

        public SpanSequence(TSegmentList segments, Span<int> countBefore, int segmentOffset) : this()
        {
            if (segments.Count - segmentOffset < countBefore.Length)
            {
                ThrowHelpers.ThrowArgumentException("`countBefore` represents invalid range.", string.Join(',', nameof(segments), nameof(countBefore), nameof(segmentOffset)));
            }
            _countBefore = countBefore;
            _segmentOffset = segmentOffset;
            _segments = segments;

            if (!_countBefore.IsEmpty)
            {
                _indexOffset = MemoryMarshal.GetReference(countBefore); // _countBefore[0]
                Length = countBefore.Last() + segments[^1].Length;
            }
            }

        private SpanSequence(TSegmentList segments, Span<int> countBefore, int segmentOffset, int indexOffset, int length) : this()
        {
            _countBefore = countBefore;
            _indexOffset = indexOffset;
            _segmentOffset = segmentOffset;
            _segments = segments;
            Length = length;
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
                return (segmentCount - 1 + _segmentOffset, index - countBeforeLast);
            }

            int l = 0, r = segmentCount - 1; // countBefore[l] <= index < countBefore[r]
            while (l + 1 != r)
            {
                int m = l + ((r - l) >> 1);
                var cb = countBefore.UnsafeAccess(m);
                if (cb <= index)
                {
                    l = m;
                }
                else
                {
                    r = m;
                }
            }

            return (l + _segmentOffset, index - countBefore.UnsafeAccess(l));
        }

        public readonly SpanSequence<TElement, TSegmentList> Slice(int index)
        {

        }

        public readonly SpanSequence<TElement, TSegmentList> Slice(int index, int length)
        {

        }

        private readonly SpanSequence<TElement, TSegmentList> SliceInternal(int index, int length)
        {
            var begin = GetLocation(index);
            var end = GetLocation(index + length);

        }
    }
}
