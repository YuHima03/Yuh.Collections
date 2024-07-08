using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    public static class CollectionBuilder
    {
        /// <summary>
        /// The minimum length of each segments.
        /// </summary>
        public const int MinSegmentLength = 16;

        internal const int SegmentsCount = 27;

#if NET8_0_OR_GREATER
        [InlineArray(SegmentsCount)]
        internal struct SegmentsArray<T>
        {
#pragma warning disable IDE0051, IDE0044
            private T[] _value;
#pragma warning restore IDE0051, IDE0044
        }

        [InlineArray(SegmentsCount)]
        internal struct Array27<T>
        {
#pragma warning disable IDE0051, IDE0044
            private T _value;
#pragma warning restore IDE0051, IDE0044
        }
#endif
    }

    /// <summary>
    /// Represents a temporary collection that is used to build new collections.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public ref struct CollectionBuilder<T>// : IDisposable
    {
        private int _allocatedCount = 0; // in the range [0, 27]
        private int _count = 0;
        private Span<T> _currentSegment = [];
        private int _countInCurrentSegment = 0;
        private int _nextSegmentLength = CollectionBuilder.MinSegmentLength;

#if NET8_0_OR_GREATER
        private CollectionBuilder.SegmentsArray<T> _segments;
#else
        private readonly T[][] _segments = new T[CollectionBuilder.SegmentsCount][];
#endif

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// Initializes a collection builder whose fields are set to default value.
        /// </summary>
        public CollectionBuilder() { }

        /// <summary>
        /// Initializes a collection builder whose first segment can contain exactly specified number of elements.
        /// </summary>
        /// <param name="firstSegmentLength">The number of elements that can be contained in the first segment.</param>
        public CollectionBuilder(int firstSegmentLength)
        {
            if (firstSegmentLength < CollectionBuilder.MinSegmentLength || Array.MaxLength < firstSegmentLength)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(firstSegmentLength), "The value is less than the minimum length of a segment, or greater than the maximum length of an array.");
            }
            _nextSegmentLength = firstSegmentLength;
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
        /// Adds elements in an <see cref="ICollection{T}"/> to the back of the <see cref="CollectionBuilder{T}"/>
        /// </summary>
        /// <param name="items">An <see cref="ICollection{T}"/> whose elements are copied to the <see cref="CollectionBuilder{T}"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddICollectionRange(ICollection<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            AddICollectionRangeInternal(items);
        }

        private void AddICollectionRangeInternal(ICollection<T> items)
        {
            int itemsLength = items.Count;
            int copyStartsAt = 0;

            int remainingCapacityInCurrentSegment = _currentSegment.Length - _countInCurrentSegment;
            if (remainingCapacityInCurrentSegment == 0)
            {
                Grow(itemsLength);
            }
            else if (remainingCapacityInCurrentSegment >= itemsLength)
            {
                copyStartsAt = _countInCurrentSegment;
            }
            else if (remainingCapacityInCurrentSegment <= itemsLength - _currentSegment.Length * 2) // _countInCurrentSegment + itemsLength >= _currentSegment.Length * 3
            {
                ShrinkCurrentSegmentToFit();
                Grow(itemsLength);
            }
            else
            {
                ExpandCurrentSegment(_countInCurrentSegment + itemsLength);
                copyStartsAt = _countInCurrentSegment;
            }

            items.CopyTo(_segments[_allocatedCount - 1], copyStartsAt);
            _count += itemsLength;
            _countInCurrentSegment += itemsLength;
        }

        /// <summary>
        /// Adds elements in an <see cref="IEnumerable{T}"/> to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{T}"/> whose elements are copied to the <see cref="CollectionBuilder{T}"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items is ICollection<T> collection)
            {
                AddICollectionRangeInternal(collection);
            }
            else
            {
                AddIEnumerableRange(items);
            }
        }

        /// <summary>
        /// Adds elements in an <see cref="IEnumerable{T}"/> to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <remarks>
        /// This implementation assumes that <paramref name="items"/> is NOT <see cref="ICollection{T}"/> and thus doesn't check if it is.
        /// </remarks>
        /// <param name="items">An <see cref="IEnumerable{T}"/> whose elements are copied to the <see cref="CollectionBuilder{T}"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddIEnumerableRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            AddIEnumerableRangeInternal(items);
        }

        /// <remarks>
        /// This doesn't check if <paramref name="items"/> is null.
        /// </remarks>
        private void AddIEnumerableRangeInternal(IEnumerable<T> items)
        {
            var currentSegment = _currentSegment;
            ref T destRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), _countInCurrentSegment);

            using var enumerator = items.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (_countInCurrentSegment == currentSegment.Length)
                {
                    Grow();
                    currentSegment = _currentSegment;
                    destRef = ref MemoryMarshal.GetReference(currentSegment);
                }

                destRef = enumerator.Current;
                destRef = ref Unsafe.Add(ref destRef, 1);
                _countInCurrentSegment++;
                _count++;
            }
        }

        /// <summary>
        /// Adds elements in the span to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">A span over elements to add.</param>
        /// <exception cref="ArgumentException">The <see cref="CollectionBuilder{T}"/> doesn't have enough space to accommodate elements contained in <paramref name="items"/>.</exception>
        public void AddRange(ReadOnlySpan<T> items)
        {
            if (items.Length == 0)
            {
                return;
            }

            if (items.Length > Array.MaxLength - _count) // use this way to avoid overflow
            {
                ThrowHelpers.ThrowArgumentException("This collection doesn't have enough space to accommodate elements of the specified span.", nameof(items));
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
        /// <exception cref="ArgumentException"><paramref name="destination"/> doesn't have enough space to accommodate elements copied.</exception>
        public readonly void CopyTo(Span<T> destination)
        {
            if (_count == 0)
            {
                return;
            }

            if (destination.Length < _count)
            {
                ThrowHelpers.ThrowArgumentException("The destination span doesn't have enough space to accommodate elements in this collection.", nameof(destination));
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
                remainsCount -= segmentLength;
            }
        }

        /// <summary>
        /// Returns the number of elements that can be contained in the <see cref="CollectionBuilder{T}"/> without allocate new internal array.
        /// </summary>
        /// <returns>The number of elements that can be contained in the <see cref="CollectionBuilder{T}"/> without allocating new internal array.</returns>
        public readonly int GetAllocatedCapacity()
        {
            int capacity = 0;
            for (int i = 0; i < _allocatedCount; i++)
            {
                capacity += _segments[i].Length;
            }
            return capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow()
            => GrowExact(_nextSegmentLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(int neededLength)
            => GrowExact(Math.Max(neededLength, _nextSegmentLength));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GrowExact(int length)
        {
            if (_allocatedCount == CollectionBuilder.SegmentsCount)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            var newSegment = GC.AllocateUninitializedArray<T>(length);
            _segments[_allocatedCount] = newSegment;

            _allocatedCount++;
            _currentSegment = newSegment.AsSpan();
            _countInCurrentSegment = 0;
            _nextSegmentLength <<= 1;
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExpandCurrentSegment(int length)
        {
            var newSegment = GC.AllocateUninitializedArray<T>(length);
            var newSegmentSpan = newSegment.AsSpan();

            var src = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_currentSegment), _countInCurrentSegment);
            src.CopyTo(newSegmentSpan);

            _segments[_allocatedCount - 1] = newSegment;
            _currentSegment = newSegmentSpan;

            CollectionHelpers.ClearIfReferenceOrContainsReferences(src);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShrinkCurrentSegmentToFit()
        {
            var newSegment = GC.AllocateUninitializedArray<T>(_countInCurrentSegment);
            var newSegmentSpan = newSegment.AsSpan();

            var src = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_currentSegment), _countInCurrentSegment);
            src.CopyTo(newSegmentSpan);

            _segments[_allocatedCount - 1] = newSegment;
            _currentSegment = newSegmentSpan;

            CollectionHelpers.ClearIfReferenceOrContainsReferences(src);
        }

        /// <summary>
        /// Creates an array from the <see cref="CollectionBuilder{T}"/> and returns it.
        /// </summary>
        /// <returns>An array which contains elements copied from the <see cref="CollectionBuilder{T}"/>.</returns>
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
    }
}
