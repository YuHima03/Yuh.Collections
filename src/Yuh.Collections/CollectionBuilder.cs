using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        internal struct Segments<T>() : IDisposable
        {
#if NET8_0_OR_GREATER
            private Array27<T> Array = new();
#else
            private T[] Array = ArrayPool<T>.Shared.Rent(MaxSegmentCount);
#endif

#if NET8_0_OR_GREATER
            [UnscopedRef]
            public Span<T> AsSpan() => Array;
#else
            public readonly Span<T> AsSpan() => Array.AsSpan()[..MaxSegmentCount];
#endif

            public void Dispose()
            {
#if NET8_0_OR_GREATER
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    ((Span<T>)Array).Clear();
                }
#else
                ArrayPool<T>.Shared.Return(Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                Array = [];
#endif
            }
        }
    }

    /// <summary>
    /// Represents a temporary collection that is used to build new collections.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public unsafe ref struct CollectionBuilder<T> : IDisposable, IEnumerable<T>
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
        private int _segmentCount = 0;

        /// <summary>
        /// Sequence of <typeparamref name="T"/>[] that has fixed capacity.
        /// </summary>
#if NET8_0_OR_GREATER
        private CollectionBuilderConstants.Array27<T[]> _segments;
#else
        private readonly T[][] _segmentsArray = new T[CollectionBuilderConstants.MaxSegmentCount][];
        private readonly Span<T[]> _segments;
#endif

        /// <summary>
        /// Whether to use array pool.
        /// If <see langword="false"/>, the collection builder never use arrays from <see cref="ArrayPool{T}"/>.
        /// </summary>
        private readonly bool _usesArrayPool = true;

        private readonly ReadOnlySpan<T[]> AllocatedSegments => MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference((ReadOnlySpan<T[]>)_segments), _segmentCount);

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
            if (_growIsNeeded)
            {
                Grow();
            }

            var currentSegment = _currentSegment;
            var countInCurrentSegment = _countInCurrentSegment++;
            currentSegment[countInCurrentSegment] = item;

            if (countInCurrentSegment + 1 == currentSegment.Length)
            {
                _growIsNeeded = true;
            }
            _count++;

            //
            // NOTE: `_countInCurrentSegment` is incremented above, so we don't have to assign `countInCurrentSegment + 1` to it.
            //
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
                var nextSegmentLength = _nextSegmentLength;
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

        private readonly T[] AllocateNewArray(int length)
        {
            return _usesArrayPool switch
            {
                true => RentArray(length),
                false => GC.AllocateUninitializedArray<T>(length)
            };
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

            if (_segmentCount == 1)
            {
                MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(_currentSegment), _count).CopyTo(destination);
                return;
            }
            CopyToInternal(destination);
        }

        private readonly void CopyToInternal(Span<T> destination)
        {
            Debug.Assert(_count <= destination.Length, "The destination span doesn't have enough space to accommodate elements contained in the collection builder.");

            switch (_segmentCount)
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
        public void Dispose()
        {
            var segmentCount = _segmentCount;
            if (segmentCount != 0)
            {
                Span<T[]> segments = _segments[.._segmentCount];
                switch ((_usesArrayPool, RuntimeHelpers.IsReferenceOrContainsReferences<T>()))
                {
                    case (true, true):
                        for (int i = 0; i < segments.Length; i++)
                        {
                            var seg = segments[i];
                            Array.Clear(seg);
                            ReturnRentedArray(seg);
                        }
                        break;
                    case (true, false):
                        for (int i = 0; i < segments.Length; i++)
                        {
                            ReturnRentedArray(segments[i]);
                        }
                        break;
                    case (false, true):
                        for (int i = 0; i < segments.Length; i++)
                        {
                            Array.Clear(segments[i]);
                        }
                        break;
                }
                segments.Clear();
            }

            _count = 0;
            _countInCurrentSegment = 0;
            _currentSegment = [];
            _growIsNeeded = false;
            _nextSegmentLength = 0;
            _segmentCount = 0;
        }

#if NET8_0_OR_GREATER
        private void ExpandCurrentSegment(int minimumLength)
#else
        private readonly void ExpandCurrentSegment(int minimumLength)
#endif
        {
            Debug.Assert(_segmentCount != 0, "The collection builder must has at least one segment.");
            Debug.Assert(_currentSegment.Length < minimumLength, "The parameter `minimumLength` must be greater than the length of the current segment.");

            ref var currentSegmentRef = ref _segments[_segmentCount - 1];
            var countInCurrentSegment = _countInCurrentSegment;

            if (countInCurrentSegment == 0)
            {
                if (_usesArrayPool)
                {
                    ReturnRentedArray(currentSegmentRef);
                    currentSegmentRef = RentArray(minimumLength);
                }
                else
                {
                    currentSegmentRef = GC.AllocateUninitializedArray<T>(minimumLength);
                }
                return;
            }

            T[] newSegment = AllocateNewArray(minimumLength);
            Array.Copy(currentSegmentRef, newSegment, countInCurrentSegment);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(currentSegmentRef, 0, countInCurrentSegment);
            }
            if (_usesArrayPool)
            {
                ReturnRentedArray(currentSegmentRef);
            }
            currentSegmentRef = newSegment;
        }

        /// <summary>
        /// Returns the number of elements that can be contained in the <see cref="CollectionBuilder{T}"/> without allocate new internal array.
        /// </summary>
        /// <returns>The number of elements that can be contained in the <see cref="CollectionBuilder{T}"/> without allocating new internal array.</returns>
        public readonly int GetAllocatedCapacity()
        {
            int segmentCount = _segmentCount;
            if (segmentCount == 0)
            {
                return 0;
            }
            return _count - _countInCurrentSegment + _segments[segmentCount - 1].Length;
        }

        private readonly IEnumerable<T> GetEnumerableForIteration()
        {
            var segments = AllocatedSegments;
            if (segments.IsEmpty)
            {
                return [];
            }
            if (segments.Length == 1)
            {
                return GetSlicedEnumerable(segments[0], _countInCurrentSegment);
            }

            IEnumerable<T> enumerable = segments[0];
            for (int i = 1; i < segments.Length - 1; i++)
            {
                enumerable = enumerable.Concat(segments[i]);
            }
            return enumerable.Concat(GetSlicedEnumerable(segments[^1], _countInCurrentSegment));

            static IEnumerable<T> GetSlicedEnumerable(T[] array, int count)
            {
                if (count == 0)
                {
                    return [];
                }
                else if (array.Length == count)
                {
                    return array;
                }
                return new ArraySegment<T>(array, 0, count);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="CollectionBuilder{T}"/>.</returns>
        public readonly Enumerator GetEnumerator()
        {
#pragma warning disable CS9084
            return new Enumerator(AllocatedSegments, in _count);
#pragma warning restore CS9084
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerableForIteration().GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerableForIteration().GetEnumerator();

        public readonly SegmentEnumerator GetSegmentEnumerator()
        {
            return new SegmentEnumerator(AllocatedSegments, _countInCurrentSegment);
        }

        public readonly SegmentMemoryEnumerator GetSegmentMemoryEnumerator()
        {
            return new SegmentMemoryEnumerator(AllocatedSegments, _countInCurrentSegment);
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
            int segmentCount = _segmentCount;
            if (segmentCount == CollectionBuilderConstants.MaxSegmentCount)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            T[] newSegmentArray = AllocateNewArray(length);
            var newSegment = newSegmentArray.AsSpan();

            Debug.Assert((uint)segmentCount < CollectionBuilderConstants.MaxSegmentCount, "Invalid segment count.");
            _countInCurrentSegment = 0;
            _currentSegment = newSegment;
            _growIsNeeded = false;
            _nextSegmentLength = checked(_nextSegmentLength << 1);
            _segmentCount = segmentCount + 1;

            Span<T[]> segments = _segments;
            segments[segmentCount] = newSegmentArray;
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
            if ((uint)length >= _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(length), "The value is negative or greater than the number of elements contained in the collection builder.");
            }

            var countInCurrentSegment = _countInCurrentSegment;
            if (length <= countInCurrentSegment)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    MemoryMarshal.CreateSpan(
                        ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSegment), countInCurrentSegment - length),
                        length
                    ).Clear();
                }
                _count -= length;
                _countInCurrentSegment = countInCurrentSegment - length;
                _growIsNeeded = false;
                return;
            }
            else
            {
                RemoveLargeRangeInternal(length);
                return;
            }
        }

        private void RemoveLargeRangeInternal(int length)
        {
            Span<T[]> segments = _segments;
            var segmentCount = _segmentCount;
            ref T[] currentSegmentRef = ref segments[segmentCount - 1];
            var countInCurrentSegment = _countInCurrentSegment;

            var isTRef = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

            while (countInCurrentSegment < length)
            {
                if (isTRef)
                {
                    Array.Clear(currentSegmentRef, 0, countInCurrentSegment);
                }
                if (_usesArrayPool)
                {
                    ReturnRentedArray(currentSegmentRef);
                }
                currentSegmentRef = [];

                segmentCount--;
                length -= countInCurrentSegment;
                currentSegmentRef = ref segments[segmentCount - 1];
                countInCurrentSegment = currentSegmentRef.Length;
            }

            _countInCurrentSegment = (countInCurrentSegment -= length);
            _count -= length;
            _growIsNeeded = false;

            if (isTRef)
            {
                Array.Clear(currentSegmentRef, countInCurrentSegment - length, length);
            }
        }

        /// <summary>
        /// Rent an array from <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <param name="length">A number of elements that the rented array can at least accommodate.</param>
        /// <returns>A rented array that can accommodate at least specified number of elements.</returns>
        private static T[] RentArray(int length)
        {
            var size = Unsafe.SizeOf<T>() * length;
            if ((uint)(size - CollectionBuilderConstants.MinArraySizeFromArrayPool) <= (CollectionBuilderConstants.MaxArraySizeFromArrayPool - CollectionBuilderConstants.MinArraySizeFromArrayPool))
            {
                return ArrayPool<T>.Shared.Rent(length);
            }
            return GC.AllocateUninitializedArray<T>(length);
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

        /// <summary>
        /// Returns the array to <see cref="ArrayPool{T}.Shared"/> if the size of the array meets condition.
        /// </summary>
        /// <param name="array">An array to return.</param>
        private static void ReturnRentedArray(T[] array)
        {
            var size = Unsafe.SizeOf<T>() * array.Length;

            // The condition below is same as `CollectionBuilderConstants.MinArraySizeFromArrayPool <= size && size <= CollectionBuilderConstants.MaxArraySizeFromArrayPool`.
            if ((uint)(size - CollectionBuilderConstants.MinArraySizeFromArrayPool) <= (CollectionBuilderConstants.MaxArraySizeFromArrayPool - CollectionBuilderConstants.MinArraySizeFromArrayPool))
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
            switch (_segmentCount)
            {
                case 0:
                    return [];
                case 1:
                    return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_segments[0]), _count).ToArray();
                default:
                    array = GC.AllocateUninitializedArray<T>(_count);
                    CopyToInternal(array.AsSpan());
                    return array;
            }
        }

        /// <summary>
        /// An enumerator that can be used to iterate through a <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        public ref struct Enumerator : IEnumerator<T>
        {
            private readonly int _count;
            private ReadOnlySpan<T> _currentSegment = [];
            private int _enumeratedCount = -1;
            private int _index = -1;
            private int _segmentIndex = -1;
            private ReadOnlySpan<T[]> _segments;

#if NET7_0_OR_GREATER
            private ref readonly int _countRef;
#endif

            /// <inheritdoc/>
            public readonly T Current => _currentSegment[_index];

            readonly object? IEnumerator.Current => Current;

            internal Enumerator(ReadOnlySpan<T[]> segments, in int count)
            {
#if NET7_0_OR_GREATER
                _countRef = ref count;
#endif
                _count = count;
                _segments = segments;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
#if NET7_0_OR_GREATER
                _countRef = ref Unsafe.NullRef<int>();
#endif
                _currentSegment = [];
                _enumeratedCount = -1;
                _index = -1;
                _segmentIndex = -1;
                _segments = [];
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
#if NET7_0_OR_GREATER
                ThrowIfCollectionIsChanged();
#endif

                var enumeratedCount = ++_enumeratedCount;
                switch (enumeratedCount - _count)
                {
                    case 0:
                        _currentSegment = [];
                        _index = -1;
                        _segmentIndex = -1;
                        return false;
                    case > 0:
                        return false;
                }

                if (++_index == _currentSegment.Length)
                {
                    _index = 0;
                    _currentSegment = _segments[++_segmentIndex].AsSpan();
                }
                return true;
            }

            /// <inheritdoc/>
            public void Reset()
            {
#if NET7_0_OR_GREATER
                ThrowIfCollectionIsChanged();
#endif
                _currentSegment = [];
                _enumeratedCount = -1;
                _index = -1;
                _segmentIndex = -1;
            }

#if NET7_0_OR_GREATER
            private readonly void ThrowIfCollectionIsChanged()
            {
                if (_countRef != _count)
                {
                    ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_CollectionModifiedAfterEnumeratorCreated);
                }
            }
#endif
        }

        public ref struct SegmentEnumerator
#if NET9_0_OR_GREATER
            : IEnumerator<ReadOnlyMemory<T>>, IEnumerator<ReadOnlySpan<T>>
#else
            : IEnumerator<ReadOnlyMemory<T>>
#endif
        {
            private readonly int _countInFinalSegment;
            private ReadOnlyMemory<T> _currentMemory = ReadOnlyMemory<T>.Empty;
            private ReadOnlySpan<T> _currentSpan = [];
            private int _index = -1;
            private ReadOnlySpan<T[]> _segments;

            public readonly ReadOnlyMemory<T> CurrentMemory => _currentMemory;

            public readonly ReadOnlySpan<T> CurrentSpan => _currentSpan;

            readonly object? IEnumerator.Current => throw new NotSupportedException();
            readonly ReadOnlyMemory<T> IEnumerator<ReadOnlyMemory<T>>.Current => _currentMemory;
#if NET9_0_OR_GREATER
            readonly ReadOnlySpan<T> IEnumerator<ReadOnlySpan<T>>.Current => _currentSpan;
#endif

            internal SegmentEnumerator(ReadOnlySpan<T[]> segments, int countInFinalSegment) : this()
            {
                _countInFinalSegment = countInFinalSegment;
                _segments = segments;
            }

            public void Dispose()
            {
                _currentMemory = ReadOnlyMemory<T>.Empty;
                _currentSpan = [];
                _segments = [];
            }

            public bool MoveNext()
            {
                var segmentCount = _segments.Length;
                var index = ++_index;

                if (index < segmentCount - 1)
                {
                    var seg = _segments[index];
                    _currentMemory = seg.AsMemory();
                    _currentSpan = seg.AsSpan();
                    return true;
                }
                else if (index == segmentCount - 1)
                {
                    var seg = _segments[index];
                    var countInFinalSegment = _countInFinalSegment;
                    _currentMemory = new ReadOnlyMemory<T>(seg, 0, countInFinalSegment);
                    _currentSpan = new ReadOnlySpan<T>(seg, 0, countInFinalSegment);
                    return true;
                }

                _currentMemory = ReadOnlyMemory<T>.Empty;
                _currentSpan = [];
                return false;
            }

            public void Reset()
            {
                _currentMemory = ReadOnlyMemory<T>.Empty;
                _currentSpan = [];
                _index = -1;
            }
        }

        public ref struct SegmentMemoryEnumerator : IEnumerator<ReadOnlyMemory<T>>
        {
            private readonly int _countInFinalSegment;
            private ReadOnlyMemory<T> _currentSegment = ReadOnlyMemory<T>.Empty;
            private int _index = -1;
            private ReadOnlySpan<T[]> _segments;

            public readonly ReadOnlyMemory<T> Current => _currentSegment;

            readonly object? IEnumerator.Current => Current;

            internal SegmentMemoryEnumerator(ReadOnlySpan<T[]> segments, int countInFinalSegment) : this()
            {
                _segments = segments;
                _countInFinalSegment = countInFinalSegment;
            }

            public void Dispose()
            {
                _currentSegment = ReadOnlyMemory<T>.Empty;
                _segments = [];
            }

            public bool MoveNext()
            {
                var segmentCount = _segments.Length;
                var index = ++_index;

                if (index < segmentCount - 1)
                {
                    _currentSegment = _segments[index].AsMemory();
                    return true;
                }
                else if (index == segmentCount - 1)
                {
                    _currentSegment = new ReadOnlyMemory<T>(_segments[index], 0, _countInFinalSegment);
                    return true;
                }

                _currentSegment = ReadOnlyMemory<T>.Empty;
                return false;
            }

            public void Reset()
            {
                _currentSegment = ReadOnlyMemory<T>.Empty;
                _index = -1;
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
