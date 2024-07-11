using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    internal unsafe struct BuffersContainer<T> : IDisposable
    {
        private int _count = 0;
        private int _nextSegmentLength = CollectionBuilder.MinSegmentLength;

#if NET8_0_OR_GREATER
        private CollectionBuilder.Array27<T[]> _segments;
        private CollectionBuilder.Array27<int> _segmentsLength;
#else
        private readonly T[][] _segments = new T[CollectionBuilder.SegmentsCount][];
        private fixed int _segmentsLength[CollectionBuilder.SegmentsCount];
#endif

        public readonly Span<T> CurrentSegment => _segments[_count - 1];
        public readonly int CurrentSegmentCapacity => _segments[_count - 1].Length;
        public readonly int CurrentSegmentLength => _segmentsLength[_count - 1];
        public readonly int DefaultNextSegmentLength => _nextSegmentLength;

        public readonly Span<T> this[int index] => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_segments[index]), _segmentsLength[index]);

        public BuffersContainer() { }

        public BuffersContainer(int firstSegmentLength)
        {
            if (firstSegmentLength < CollectionBuilder.MinSegmentLength || Array.MaxLength < firstSegmentLength)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(firstSegmentLength), "The value is less than the minimum length of a segment, or greater than the maximum length of an array.");
            }
            _nextSegmentLength = firstSegmentLength;
        }

        public void AllocateNewBuffer()
            => AllocateNewBuffer(_nextSegmentLength);

        public void AllocateNewBuffer(int minimumLength)
        {
            if (_count == CollectionBuilder.SegmentsCount)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            var segment = ArrayPool<T>.Shared.Rent(minimumLength);

            _segments[_count] = segment;
            _segmentsLength[_count] = segment.Length;
            _count++;
            _nextSegmentLength = Math.Min(checked(_nextSegmentLength << 1), Array.MaxLength);
        }

        public readonly T[] ArrayAt(int index) => _segments[index];

        public readonly void Dispose()
        {
            if (_count != 0)
            {
                ReadOnlySpan<T[]> segments = _segments;
                segments = segments[.._count];

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    foreach (var s in segments)
                    {
                        Array.Clear(s);
                        ArrayPool<T>.Shared.Return(s);
                    }
                }
                else
                {
                    foreach (var s in segments)
                    {
                        ArrayPool<T>.Shared.Return(s);
                    }
                }
            }
        }

        public void ExpandCurrentSegment(int minimumLength)
        {
            var currentSegmentCapacity = CurrentSegmentCapacity;
            if (minimumLength <= currentSegmentCapacity)
            {
                _segmentsLength[_count - 1] = currentSegmentCapacity;
                return;
            }

            var segmentLength = Math.Max(minimumLength, checked(currentSegmentCapacity << 1));
            var segment = ArrayPool<T>.Shared.Rent(segmentLength);
            var oldSegment = _segments[_count - 1];
            var oldSegmentLength = _segmentsLength[_count - 1];

            Array.Copy(oldSegment, segment, oldSegmentLength);
            CollectionHelpers.ClearIfReferenceOrContainsReferences(
                MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(oldSegment), oldSegmentLength)
            );

            _segments[_count - 1] = segment;
            _segmentsLength[_count - 1] = segmentLength;
            ArrayPool<T>.Shared.Return(oldSegment);
        }

        public void ExpandCurrentSegmentWithoutResizing()
        {
            _segmentsLength[_count - 1] = _segments[_count - 1].Length;
        }

        public void ShrinkCurrentSegment(int length)
        {
            _segmentsLength[_count - 1] = length;
        }
    }
}
