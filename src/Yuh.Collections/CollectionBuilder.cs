using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SysCollectionsMarshal = System.Runtime.InteropServices.CollectionsMarshal;

namespace Yuh.Collections
{
    /// <summary>
    /// Repreents a temporary collection that is used to build new collections.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public ref struct CollectionBuilder<T>// : IDisposable
    {
        public const int MinSegmentLength = 16;

        private int _allocatedCount = 0; // in the range [0, 27]
        private int _count = 0;
        private Span<T> _currentSegment = [];
        private int _countInCurrentSegment = 0;
        private int _nextSegmentLength = MinSegmentLength;

#if NET8_0_OR_GREATER
        private SegmentsArray _segments;
#else
        private readonly T[][] _segments;
#endif

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        public CollectionBuilder() { }

        public CollectionBuilder(int firstSegmentLength)
        {
            if (firstSegmentLength < MinSegmentLength || Array.MaxLength < firstSegmentLength)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(firstSegmentLength), "The value is less than the minimum length of a segment, or greater than the maximum length of an array.");
            }
            _nextSegmentLength = firstSegmentLength;

#if NET8_0_OR_GREATER
            _segments = new();
#else
            _segments = GC.AllocateUninitializedArray<T[]>(27);
#endif
        }

        /// <summary>
        /// Adds a element to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="item">An object to add.</param>
        /// <exception cref="Exception">The <see cref="CollectionBuilder{T}"/> is already full.</exception>
        public void Add(T item)
        {
            if (_count == Array.MaxLength)
            {
                ThrowHelpers.ThrowException("This collection is already full.");
            }

            if (_countInCurrentSegment == _currentSegment.Length)
            {
                Grow();
            }

            _currentSegment[_countInCurrentSegment] = item;
            _countInCurrentSegment++;
            _count++;
        }

        /// <summary>
        /// Adds elements in a <see cref="IEnumerable{T}"/> to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{T}"/> whose elements are copied to the <see cref="CollectionBuilder{T}"/>.</param>
        public void AddRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var currentSegment = _currentSegment;

            foreach (var item in items)
            {
                if (_countInCurrentSegment == currentSegment.Length)
                {
                    Grow();
                    currentSegment = _currentSegment;
                }

                currentSegment[_countInCurrentSegment] = item;
                _countInCurrentSegment++;
                _count++;
            }
        }

        /// <summary>
        /// Adds elements in a <see cref="IEnumerable{T}"/> to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <remarks>
        /// This implementation assumes that <paramref name="items"/> is NOT <see cref="ICollection{T}"/> and thus doesn't check if it is.
        /// </remarks>
        /// <param name="items"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNonICollectionRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            AddNonICollectionRangeInternal(items);
        }

        /// <remarks>
        /// This doesn't check if <paramref name="items"/> is null.
        /// </remarks>
        private void AddNonICollectionRangeInternal(IEnumerable<T> items)
        {
            var currentSegment = _currentSegment;

            foreach (var item in items)
            {
                if (_countInCurrentSegment == currentSegment.Length)
                {
                    Grow();
                    currentSegment = _currentSegment;
                }

                currentSegment[_countInCurrentSegment] = item;
                _countInCurrentSegment++;
                _count++;
            }
        }

        /// <summary>
        /// Adds elements in the span to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">A span over elements to add.</param>
        /// <exception cref="ArgumentException">The <see cref="CollectionBuilder{T}"/> doesn't have enough space to accomodate elements contained in <paramref name="items"/>.</exception>
        public void AddRange(ReadOnlySpan<T> items)
        {
            if (items.Length == 0)
            {
                return;
            }

            if (items.Length > Array.MaxLength - _count) // use this way to avoid overflow
            {
                ThrowHelpers.ThrowArgumentException("This collection doesn't have enough space to accomodate elements of the specified span.", nameof(items));
            }

            int newCount = _count + items.Length;
            int remainsCount = items.Length;
            ref readonly T beginningRef = ref MemoryMarshal.GetReference(items);

            while (true)
            {
                int maxCopyLength = _currentSegment.Length - _countInCurrentSegment;

                if (maxCopyLength == 0)
                {
                    Grow();
                    continue;
                }

                if (remainsCount <= maxCopyLength)
                {
                    MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in beginningRef), remainsCount)
                        .CopyTo(MemoryMarshal.CreateSpan(
                            ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSegment), _countInCurrentSegment),
                            remainsCount
                        ));
                    _countInCurrentSegment += remainsCount;
                    break;
                }
                else
                {
                    MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in beginningRef), maxCopyLength)
                        .CopyTo(MemoryMarshal.CreateSpan(
                            ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSegment), _countInCurrentSegment),
                            remainsCount
                        ));
                    beginningRef = ref Unsafe.Add(ref Unsafe.AsRef(in beginningRef), maxCopyLength);
                    remainsCount -= maxCopyLength;
                    Grow();
                }
            }

            _count = newCount;
        }

        /// <summary>
        /// Copies elements in the <see cref="CollectionBuilder{T}"/> to the specified span.
        /// </summary>
        /// <param name="destination"></param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> doesn't have enough space to accomodate elements copied.</exception>
        public readonly void CopyTo(Span<T> destination)
        {
            if (_count == 0)
            {
                return;
            }

            if (destination.Length < _count)
            {
                ThrowHelpers.ThrowArgumentException("The destination span doesn't have enough space to accomodate elements in this collection.", nameof(destination));
            }

            int remainsCount = _count;
            ref T destRef = ref MemoryMarshal.GetReference(destination);

            foreach (var segment in _segments)
            {
                int segmentLength = segment.Length;

                if (remainsCount <= segmentLength)
                {
                    MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(segment), remainsCount)
                        .CopyTo(MemoryMarshal.CreateSpan(ref destRef, remainsCount));
                    break;
                }

                segment.AsSpan().CopyTo(MemoryMarshal.CreateSpan(ref destRef, segmentLength));
                destRef = ref Unsafe.Add(ref Unsafe.AsRef(in destRef), segmentLength);
                remainsCount -= segment.Length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow()
            => GrowExact(_nextSegmentLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GrowExact(int length)
        {
            var newSegment = GC.AllocateUninitializedArray<T>(length);
            _segments[_allocatedCount] = newSegment;

            _allocatedCount++;
            _currentSegment = newSegment.AsSpan();
            _countInCurrentSegment = 0;
            _nextSegmentLength <<= 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeCurrentSegment(int length)
        {
            var newSegment = GC.AllocateUninitializedArray<T>(length);
            var countInNewSegment = Math.Min(length, _currentSegment.Length);
            ref var currentSegmentRef = ref MemoryMarshal.GetReference(_currentSegment);

            MemoryMarshal.CreateReadOnlySpan(ref currentSegmentRef, countInNewSegment).CopyTo(newSegment);
            CollectionHelpers.ClearIfReferenceOrContainsReferences(
                MemoryMarshal.CreateSpan(ref currentSegmentRef, _countInCurrentSegment)
            );

            _segments[_allocatedCount - 1] = newSegment;
            _currentSegment = newSegment.AsSpan();
            _countInCurrentSegment = countInNewSegment;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray()
        {
            if (_count == 0)
            {
                return [];
            }

            var array = GC.AllocateUninitializedArray<T>(_count);
            CopyTo(array.AsSpan());
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly List<T> ToList()
        {
            if (_count == 0)
            {
                return [];
            }

#if NET8_0_OR_GREATER
            var list = new List<T>(_count);
            SysCollectionsMarshal.SetCount(list, _count);
            CopyTo(SysCollectionsMarshal.AsSpan(list));
#else
            var list = Enumerable.Repeat(default(T)!, _count).ToList();
            CopyTo(SysCollectionsMarshal.AsSpan(list));
#endif
            return list;
        }

#if NET8_0_OR_GREATER
        [InlineArray(27)]
        private struct SegmentsArray
        {
            private T[] _value;
        }
#endif
    }
}
