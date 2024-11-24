using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    internal static class CollectionBuilderConstants
    {
        internal const int MaxArraySizeFromArrayPool = 1 << 27;
        internal const int MinArraySizeFromArrayPool = 1 << 11;
        internal const int MaxArraySizeFromStack = 1 << 10;
        internal const int MinSegmentLength = 16;
        internal const int MaxSegmentCount = 27;

#if NET8_0_OR_GREATER
        [InlineArray(MaxSegmentCount)]
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
    public unsafe ref struct CollectionBuilder<T> // : IDisposable
    {
        /// <summary>
        /// The number of elements contained in the collection.
        /// </summary>
        private int _count = 0;

        /// <summary>
        /// The number of elements 
        /// </summary>
        private int _countInCurrentSegment = 0;

        /// <summary>
        /// Current segment to stack elements.
        /// </summary>
        private Span<T> _currentSegment = [];

        /// <summary>
        /// Whether to allocate a new segment in the next addition operation.
        /// If <see langword="true"/>, the collection builder allocates a new segment and uses it.
        /// </summary>
        private bool _growIsNeeded = true;

        /// <summary>
        /// The minimum length of segment allocated in the next allocation.
        /// </summary>
        private int _nextSegmentLength = CollectionBuilderConstants.MinSegmentLength;

        /// <summary>
        /// The number of segments contained in the collection builder.
        /// </summary>
        /// <remarks>
        /// It is ensured that the value is not negative and less than <see cref="CollectionBuilderConstants.MaxSegmentCount"/>, the maximum number of segments that may be contained in the collection builder.
        /// </remarks>
        private int _segmentsCount = 0;

        /// <summary>
        /// Sequence of <typeparamref name="T"/>[] that has fixed capacity.
        /// </summary>
#if NET8_0_OR_GREATER
        private CollectionBuilderConstants.Array27<T[]> _segments;
#else
        private readonly T[][] _segmentsArray = new T[CollectionBuilderConstants.MaxSegmentCount][];
        private readonly Span<T[]> _segments;
#endif

        private readonly ReadOnlySpan<T[]> AllocatedSegments => MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference((ReadOnlySpan<T[]>)_segments), _segmentsCount);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// Gets the number of elements that can be added without resizing the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        public readonly int RemainingCapacity => _currentSegment.Length - _countInCurrentSegment;

        /// <summary>
        /// Initializes a collection builder whose fields are set to default value.
        /// </summary>
        public CollectionBuilder()
        {
#if !NET8_0_OR_GREATER
            _segments = _segmentsArray.AsSpan();
#endif
        }

        /// <summary>
        /// Initializes a collection builder whose first segment can contain exactly specified number of elements.
        /// </summary>
        /// <param name="firstSegmentLength">The number of elements that can be contained in the first segment.</param>
        public CollectionBuilder(int firstSegmentLength) : this()
        {
            if (firstSegmentLength < CollectionBuilderConstants.MinSegmentLength || Array.MaxLength < firstSegmentLength)
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
        public void Append(T item)
        {
            if (_count == Array.MaxLength)
            {
                ThrowHelpers.ThrowException("This collection is already full.");
            }

            GrowIfNeeded();
            _currentSegment[_countInCurrentSegment] = item;
            _count++;
            _countInCurrentSegment++;
            _growIsNeeded = (_countInCurrentSegment == _currentSegment.Length);
        }

        /// <summary>
        /// Adds elements in an <see cref="ICollection{T}"/> to the back of the <see cref="CollectionBuilder{T}"/>
        /// </summary>
        /// <param name="items">An <see cref="ICollection{T}"/> whose elements are copied to the <see cref="CollectionBuilder{T}"/>.</param>
        public void AppendICollectionRange(ICollection<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            AppendICollectionRangeInternal(items);
        }

        private void AppendICollectionRangeInternal(ICollection<T> items)
        {
            var itemsCount = items.Count;
            if (itemsCount == 0)
            {
                return;
            }
            else if (itemsCount == 1)
            {
                Append(items.First());
                return;
            }
            else if (_growIsNeeded)
            {
                Grow(itemsCount);
                items.CopyTo(AllocatedSegments[^1], 0);

                _count += itemsCount;
                _growIsNeeded = (_currentSegment.Length == (_countInCurrentSegment = itemsCount));
                return;
            }

            var currentSegment = _currentSegment;
            var countInCurrentSegment = _countInCurrentSegment;

            if (itemsCount <= currentSegment.Length - countInCurrentSegment)
            {
                items.CopyTo(AllocatedSegments[^1], countInCurrentSegment);

                _count += itemsCount;
                _growIsNeeded = (currentSegment.Length == (_countInCurrentSegment = countInCurrentSegment + itemsCount));
                return;
            }
            else
            {
                var nextSegmentLength = ComputeNextSegmentLength();
                var neededLength = countInCurrentSegment + itemsCount;

                if (countInCurrentSegment < nextSegmentLength && neededLength < checked(currentSegment.Length + nextSegmentLength))
                {
                    ExpandCurrentSegment(neededLength);
                    items.CopyTo(AllocatedSegments[^1], countInCurrentSegment);

                    _count += itemsCount;
                    _growIsNeeded = (_currentSegment.Length == (_countInCurrentSegment = neededLength));
                    return;
                }
            }

            AppendIEnumerableRangeInternal(items);
        }

        /// <summary>
        /// Adds elements in an <see cref="IEnumerable{T}"/> to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{T}"/> whose elements are copied to the <see cref="CollectionBuilder{T}"/>.</param>
        public void AppendRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items is ICollection<T> collection)
            {
                AppendICollectionRangeInternal(collection);
            }
            else
            {
                AppendIEnumerableRangeInternal(items);
            }
        }

        /// <summary>
        /// Adds elements in an <see cref="IEnumerable{T}"/> to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <remarks>
        /// This implementation assumes that <paramref name="items"/> is NOT <see cref="ICollection{T}"/> and thus doesn't check if it is.
        /// </remarks>
        /// <param name="items">An <see cref="IEnumerable{T}"/> whose elements are copied to the <see cref="CollectionBuilder{T}"/>.</param>
        public void AppendIEnumerableRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            AppendIEnumerableRangeInternal(items);
        }

        /// <remarks>
        /// This doesn't check if <paramref name="items"/> is null.
        /// </remarks>
        private void AppendIEnumerableRangeInternal(IEnumerable<T> items)
        {
            int countInCurrentSegment = _countInCurrentSegment;
            var currentSegment = _currentSegment;

            foreach (var item in items)
            {
                if (countInCurrentSegment == currentSegment.Length)
                {
                    _count += countInCurrentSegment - _countInCurrentSegment;

                    Grow();
                    countInCurrentSegment = 0;
                    currentSegment = _currentSegment;
                }

                Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), countInCurrentSegment) = item;
                countInCurrentSegment++;
            }

            _count += countInCurrentSegment - _countInCurrentSegment;
            _growIsNeeded = (currentSegment.Length == (_countInCurrentSegment = countInCurrentSegment));
        }

        /// <summary>
        /// Adds elements in the span to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">A span over elements to add.</param>
        /// <exception cref="ArgumentException">The <see cref="CollectionBuilder{T}"/> doesn't have enough space to accommodate elements contained in <paramref name="items"/>.</exception>
        public void AppendRange(scoped ReadOnlySpan<T> items)
        {
            int itemsLength = items.Length;
            if (itemsLength == 0)
            {
                return;
            }
            else if (itemsLength == 1)
            {
                Append(items[0]);
                return;
            }

            Span<T> currentSegment;
            int countInCurrentSegment;

            if (_growIsNeeded)
            {
                Grow(itemsLength);

                currentSegment = _currentSegment;
                items.CopyTo(currentSegment);
                _count += itemsLength;

                //
                // It is ensured that item count in `_currentSegment` is equal to `itemsLength`.
                //
                if (itemsLength == currentSegment.Length)
                {
                    _growIsNeeded = true;
            }
                _countInCurrentSegment = itemsLength;
                return;
            }

            currentSegment = _currentSegment;
            countInCurrentSegment = _countInCurrentSegment;

            int remainingCapacityInCurrentSegment = currentSegment.Length - countInCurrentSegment;

            if (itemsLength <= remainingCapacityInCurrentSegment)
            {
                items.CopyTo(currentSegment[countInCurrentSegment..]);
                countInCurrentSegment += itemsLength;
            }
            else
            {
                items[..remainingCapacityInCurrentSegment].CopyTo(currentSegment[countInCurrentSegment..]);

                var nextSegmentMinimumLength = itemsLength - remainingCapacityInCurrentSegment;
                Grow(nextSegmentMinimumLength);
                currentSegment = _currentSegment;

                items[remainingCapacityInCurrentSegment..].CopyTo(currentSegment);
                countInCurrentSegment = nextSegmentMinimumLength;
            }
            _count += itemsLength;

            if (countInCurrentSegment == currentSegment.Length)
            {
                _growIsNeeded = true;
            }
            _countInCurrentSegment = countInCurrentSegment;
                return;
            }

        private static T[] AllocateNewArray(int length)
        {
            return checked(Unsafe.SizeOf<T>() * length) switch
            {
                < CollectionBuilderConstants.MinArraySizeFromArrayPool => new T[length],
                <= CollectionBuilderConstants.MaxArraySizeFromArrayPool => ArrayPool<T>.Shared.Rent(length),
                _ => GC.AllocateUninitializedArray<T>(length)
            };
        }

        private readonly int ComputeNextSegmentLength()
        {
            int segmentCount = _segmentsCount;
            int nextSegmentLength = _nextSegmentLength;
            if (segmentCount == 0)
            {
                return nextSegmentLength;
            }

            int currentSegmentMinimumLength = unchecked((int)((uint)nextSegmentLength >> 1));
            int nextSegmentAdditionalLength = currentSegmentMinimumLength - _countInCurrentSegment;
            if (nextSegmentAdditionalLength <= 0)
            {
                return nextSegmentLength;
            }
            else
            {
                return checked(nextSegmentLength + nextSegmentAdditionalLength);
            }
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

            if (_segmentsCount == 1)
            {
                MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(_currentSegment), _count).CopyTo(destination);
                return;
            }
            CopyToInternal(destination);
        }

        private readonly void CopyToInternal(Span<T> destination)
        {
            Debug.Assert(_count <= destination.Length, "The destination span doesn't have enough space to accommodate elements contained in the collection builder.");

            switch (_segmentsCount)
            {
                case 1:
                    _currentSegment[.._countInCurrentSegment].CopyTo(destination);
                    return;
                case 2:
                    var firstSegment = _segments[0];
                    firstSegment.CopyTo(destination);
                    _currentSegment[.._countInCurrentSegment].CopyTo(destination[firstSegment.Length..]);
                    return;
            }

            var allocatedSegments = AllocatedSegments;
            ref T destRef = ref MemoryMarshal.GetReference(destination);

            for (int i = 0; i < allocatedSegments.Length - 1; i++)
            {
                var segment = allocatedSegments[i].AsSpan();
                segment.CopyTo(MemoryMarshal.CreateSpan(ref destRef, segment.Length));
                destRef = ref Unsafe.Add(ref destRef, segment.Length);
            }

            var countInLastSegment = _countInCurrentSegment;
            allocatedSegments[^1].AsSpan()[..countInLastSegment].CopyTo(MemoryMarshal.CreateSpan(ref destRef, countInLastSegment));
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public readonly void Dispose()
        {
            var allocatedSegments = AllocatedSegments;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                foreach (var segmentArray in allocatedSegments)
                {
                    Array.Clear(segmentArray);
                    ReturnIfArrayIsFromArrayPool(segmentArray);
                }
            }
            else
            {
                foreach (var segmentArray in allocatedSegments)
                {
                    ReturnIfArrayIsFromArrayPool(segmentArray);
                }
            }
        }

        private void ExpandCurrentSegment(int length)
        {
            var newSegment = AllocateNewArray(Math.Max(length, checked(_currentSegment.Length << 1)));
            var newSegmentSpan = newSegment.AsSpan();
            var oldSegment = Interlocked.Exchange(ref _segments[_segmentsCount - 1], newSegment);

            var copySource = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_currentSegment), _countInCurrentSegment);
            copySource.CopyTo(newSegmentSpan);

            CollectionHelpers.ClearIfReferenceOrContainsReferences(copySource);
            ReturnIfArrayIsFromArrayPool(oldSegment);

            _currentSegment = newSegmentSpan;
        }

        /// <summary>
        /// Returns the number of elements that can be contained in the <see cref="CollectionBuilder{T}"/> without allocate new internal array.
        /// </summary>
        /// <returns>The number of elements that can be contained in the <see cref="CollectionBuilder{T}"/> without allocating new internal array.</returns>
        public readonly int GetAllocatedCapacity()
        {
            int capacity = 0;
            for (int i = 0; i < _segmentsCount; i++)
            {
                capacity += _segments[i].Length;
            }
            return capacity;
        }

        /// <summary>
        /// Allocates new buffer than can accommodate at least <see cref="_nextSegmentLength"/> elements.
        /// </summary>
        private void Grow()
            => GrowExact(_nextSegmentLength);

        /// <summary>
        /// Allocates new buffer than can accommodate at least specified number of elements.
        /// </summary>
        /// <remarks>
        /// If <paramref name="neededLength"/> is greater than <see cref="_nextSegmentLength"/>, this method allocate new buffer that can accommodate <paramref name="neededLength"/> elements; otherwise, <see cref="_nextSegmentLength"/> elements.
        /// </remarks>
        /// <param name="neededLength"></param>
        private void Grow(int neededLength)
            => GrowExact(Math.Max(neededLength, _nextSegmentLength));

        /// <summary>
        /// Allocates new buffer that can accommodate at least specified number of elements.
        /// </summary>
        /// <param name="length"></param>
        private void GrowExact(int length)
        {
            if (_segmentsCount == CollectionBuilderConstants.MaxSegmentCount)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            var newSegment = AllocateNewArray(length);
            _segments[_segmentsCount] = newSegment;

            _countInCurrentSegment = 0;
            _currentSegment = newSegment.AsSpan();
            _growIsNeeded = false;
            _nextSegmentLength <<= 1;
            _segmentsCount++;
        }

        /// <summary>
        /// If grow is needed, allocates new buffer than can accommodate at least <see cref="_nextSegmentLength"/> elements.
        /// </summary>
        private void GrowIfNeeded()
        {
            if (_growIsNeeded)
            {
                Grow();
                _growIsNeeded = false;
            }
        }

        private void RemoveCurrentSegment()
        {
            if (_segmentsCount == 0)
            {
                return;
            }

            var currentSegment = Interlocked.Exchange(ref _segments[_segmentsCount - 1], null!);

            if (_countInCurrentSegment != 0)
            {
                CollectionHelpers.ClearIfReferenceOrContainsReferences(_currentSegment);
            }

            ReturnIfArrayIsFromArrayPool(currentSegment);
            _segmentsCount--;
            _currentSegment = AllocatedSegments[^1];
            _countInCurrentSegment = _currentSegment.Length;
            _growIsNeeded = true;
            return;
        }

        /// <summary>
        /// Removes specified number of elements from the end of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="length">The number of elements to remove from the <see cref="CollectionBuilder{T}"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative or greater than <see cref="Count"/>.</exception>
        public void RemoveRange(int length)
        {
            if (length == 0)
            {
                return;
            }
            if ((uint)length > _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(length), "The value is negative or greater than the number of elements in the collection builder.");
            }
            else if (length > _countInCurrentSegment)
            {
                length -= _countInCurrentSegment;
                RemoveCurrentSegment();
                RemoveRange(length);
                return;
            }

            _count -= length;
            var countInCurrentSegment = (_countInCurrentSegment -= length);
            _growIsNeeded = false; // It is ensured that `_countInCurrentSegment` is less than `_currentSegment.Length` because `length` is positive.

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                var removedRange = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSegment), countInCurrentSegment),
                    length
                );
                removedRange.Clear();
            }
        }

        /// <summary>
        /// Reserves the specified length of memory region from the back of the <see cref="CollectionBuilder{T}"/> and returns a <see cref="SegmentPair"/> that has spans over reserved memory regions.
        /// </summary>
        /// <param name="length">A number of elements that reserved memory regions can exactly accommodate.</param>
        /// <returns>A <see cref="SegmentPair"/> over the reserved memory regions.</returns>
        public SegmentPair ReserveSegmentedRange(int length)
        {
            Span<T> currentSegment;

            if (_growIsNeeded)
            {
                Grow(length);
                currentSegment = _currentSegment;
                _count += length;
                _growIsNeeded = (currentSegment.Length == (_countInCurrentSegment += length));
                return new SegmentPair(currentSegment[..length]);
            }

            currentSegment = _currentSegment;
            var countInCurrentSegment = _countInCurrentSegment;
            var minimumLengthOfNextSegment = length - (currentSegment.Length - countInCurrentSegment);

            if (minimumLengthOfNextSegment <= 0)
            {
                _count += length;
                _growIsNeeded = (currentSegment.Length == (_countInCurrentSegment = countInCurrentSegment + length));
                return new SegmentPair(currentSegment.Slice(countInCurrentSegment, length));
            }
            else
            {
                Grow(minimumLengthOfNextSegment);
                var prevSegment = currentSegment;
                currentSegment = _currentSegment;
                _count += length;
                _growIsNeeded = (currentSegment.Length == (_countInCurrentSegment = minimumLengthOfNextSegment));
                return new SegmentPair(prevSegment[countInCurrentSegment..], currentSegment[..minimumLengthOfNextSegment]);
            }
        }

        /// <summary>
        /// Reserves the specified length of memory region from the back of the <see cref="CollectionBuilder{T}"/> and returns the span over the region.
        /// </summary>
        /// <param name="length">The number of elements that the reserved memory region can exactly accommodate.</param>
        /// <returns>The span over the reserved memory region.</returns>
        public Span<T> ReserveRange(int length)
        {
            if (Array.MaxLength - _count < (uint)length)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(length), "The value is negative or greater than the maximum number of elements that can be added to this builder.");
            }

            var countInCurrentSegment = _countInCurrentSegment;
            var currentSegment = _currentSegment;
            Span<T> range;

            //
            // Note: Please be careful that _currentSegment changes after calling Grow or ExpandCurrentSegment method.
            //
            if (_growIsNeeded)
            {
                Grow(length);
                currentSegment = _currentSegment;
                countInCurrentSegment = 0;
                range = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(currentSegment), length);
            }
            else
            {
                var minimumSegmentLength = countInCurrentSegment + length;
                if (minimumSegmentLength <= currentSegment.Length)
                {
                    range = MemoryMarshal.CreateSpan(
                        ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), countInCurrentSegment),
                        length
                    );
                }
                else
                {
                    ExpandCurrentSegment(minimumSegmentLength);
                    currentSegment = _currentSegment;
                    range = currentSegment.Slice(countInCurrentSegment, length);
                }
            }

            _count += length;
            _growIsNeeded = (currentSegment.Length == (_countInCurrentSegment = countInCurrentSegment + length));
            return range;
        }

        private static void ReturnIfArrayIsFromArrayPool(T[] array)
        {
            if ((uint)(checked(Unsafe.SizeOf<T>() * array.Length) - CollectionBuilderConstants.MinArraySizeFromArrayPool) <= (CollectionBuilderConstants.MaxArraySizeFromArrayPool - CollectionBuilderConstants.MinArraySizeFromArrayPool))
            {
                ArrayPool<T>.Shared.Return(array);
            }
        }

        /// <summary>
        /// Creates an array from the <see cref="CollectionBuilder{T}"/> and returns it.
        /// </summary>
        /// <returns>An array which contains elements copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public readonly T[] ToArray()
        {
            T[] array;
            switch (_segmentsCount)
            {
                case 0:
                    return [];
                case 1:
                    return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_segments[0]), _count).ToArray();
                default:
                    array = GC.AllocateUninitializedArray<T>(_count);
                    CopyTo(array.AsSpan());
                    return array;
            }
        }

        /// <summary>
        /// Represents a read-only collection that has at most two segments.
        /// </summary>
        public readonly ref struct SegmentPair()
        {
            /// <summary>
            /// The number of segments contained in the collection.
            /// </summary>
            public readonly int Count = 0;

            /// <summary>
            /// A span over the first segment of the collection.
            /// </summary>
            public readonly Span<T> FirstSegment = [];

            /// <summary>
            /// A span over the last segment of the collection.
            /// </summary>
            public readonly Span<T> LastSegment = [];

            internal SegmentPair(Span<T> span1) : this()
            {
                Count = 1;
                FirstSegment = span1;
            }

            internal SegmentPair(Span<T> span1, Span<T> span2) : this()
            {
                Count = 2;
                FirstSegment = span1;
                LastSegment = span2;
            }
        }
    }
}
