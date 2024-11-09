using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yuh.Collections.Helpers;

namespace Yuh.Collections
{
    internal static class CollectionBuilderConstants
    {
        internal const int MaxSegmentCount = 27;
        internal const int MaxArraySizeFromArrayPool = 1 << 27;
        internal const int MinArraySizeFromArrayPool = 1 << 11;
        internal const int MaxArraySizeFromStack = 1 << 10;
        internal const int MinSegmentLength = 16;

#if NET8_0_OR_GREATER
        [InlineArray(MaxSegmentCount)]
        internal struct InternalArray<T>
        {
            public T Value;
        }
#endif
    }

    /// <summary>
    /// Represents a temporary collection that is used to build new collections.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public ref struct CollectionBuilder<T>
#if NET9_0_OR_GREATER
        : IEnumerable<T>, IReadOnlyCollection<T>
#endif
    {
        /// <summary>
        /// The span over allocated segments.
        /// </summary>
        /// <remarks>
        /// The length of the span is equal to <see cref="_segmentCount"/>.
        /// </remarks>
        private ReadOnlySpan<T[]> _allocatedSegments;

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
        /// The length of segments.
        /// </summary>
#if NET8_0_OR_GREATER
        private CollectionBuilderConstants.InternalArray<int> _segmentLength;
#else
        private readonly int[] _segmentLength = new int[CollectionBuilderConstants.MaxSegmentCount];
#endif

        /// <summary>
        /// Sequence of <typeparamref name="T"/>[] that has fixed capacity.
        /// </summary>
#if NET8_0_OR_GREATER
        private CollectionBuilderConstants.InternalArray<T[]> _segments;
#else
        private readonly T[][] _segments = new T[CollectionBuilderConstants.MaxSegmentCount][];
#endif

        /// <summary>
        /// Whether to use array pool.
        /// If <see langword="false"/>, the collection builder never use arrays from <see cref="ArrayPool{T}"/>.
        /// </summary>
        private readonly bool _usesArrayPool = true;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// Gets the number of elements that can be added without allocating a new segment array.
        /// </summary>
        public readonly int RemainingCapacity
        {
            get
            {
                var segmentCount = _segmentCount;
                if (segmentCount == 0)
                {
                    return 0;
                }
                ReadOnlySpan<int> segmentLength = _segmentLength;
                return segmentLength.UnsafeAccess(segmentCount - 1) - _countInCurrentSegment;
            }
        }

        /// <summary>
        /// Initializes a collection builder whose fields are set to default value.
        /// </summary>
        public CollectionBuilder()
        {
            // If `_allocatedSegments` is set to default empty span, NullReferenceException will be thrown because the span has null reference.
            // So, the initializer assigns 0-length span that has reference to `_segments` field below.
#if NET8_0_OR_GREATER
            ReadOnlySpan<T[]> segments = _segments;
            _allocatedSegments = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(segments), 0);
#else
            _allocatedSegments = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(_segments), 0);
#endif
        }
        
        /// <summary>
         /// Initializes a collection builder which can/never use <see cref="ArrayPool{T}"/>.
         /// </summary>
         /// <param name="usesArrayPool">
         /// Whether the collection builder can use arrays from <see cref="ArrayPool{T}"/>.
         /// If <see langword="false"/>, it never use <see cref="ArrayPool{T}"/>.
         /// </param>
        public CollectionBuilder(bool usesArrayPool) : this()
        {
            _usesArrayPool = usesArrayPool;
        }

        /// <summary>
        /// Initializes a collection builder whose first segment can contain exactly specified number of elements.
        /// </summary>
        /// <param name="firstSegmentLength">The number of elements that can be contained in the first segment.</param>
        /// <param name="usesArrayPool">
        /// Whether the collection builder can use arrays from <see cref="ArrayPool{T}"/>.
        /// If <see langword="false"/>, it never use <see cref="ArrayPool{T}"/>.
        /// </param>
        public CollectionBuilder(int firstSegmentLength, bool usesArrayPool = true) : this(usesArrayPool)
        {
            _nextSegmentLength = Math.Max(firstSegmentLength, CollectionBuilderConstants.MinSegmentLength);
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
            currentSegment.UnsafeAccess(countInCurrentSegment) = item;

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
            var itemsLength = items.Count;
            if (itemsLength == 0)
            {
                return;
            }
            else if (itemsLength == 1)
            {
                Append(items.First());
                return;
            }

            Reserve(itemsLength);
            items.CopyTo(
                _allocatedSegments.UnsafeAccess(_segmentCount - 1),
                _countInCurrentSegment - itemsLength
            );
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
            if (_growIsNeeded)
            {
                Grow();
            }

            using var enumerator = items.GetEnumerator();
            if (!enumerator.MoveNext()) // If `items` is empty.
            {
                return;
            }

            var currentSegment = _currentSegment;
            var countInCurrentSegment = _countInCurrentSegment;
            int itemsCount = 1;

            currentSegment.UnsafeAccess(countInCurrentSegment) = enumerator.Current;
            countInCurrentSegment++;

            while (enumerator.MoveNext())
            {
                itemsCount++;

                if (countInCurrentSegment == currentSegment.Length)
                {
                    GrowExact(_nextSegmentLength);
                    currentSegment = _currentSegment;

                    currentSegment.First() = enumerator.Current;
                    countInCurrentSegment = 1;
                    continue;
                }

                currentSegment.UnsafeAccess(countInCurrentSegment) = enumerator.Current;
                countInCurrentSegment++;
            }
            _count += itemsCount;

            if (countInCurrentSegment == currentSegment.Length)
            {
                _growIsNeeded = true;
            }
            _countInCurrentSegment = countInCurrentSegment;
        }

        /// <summary>
        /// Adds elements in an <see cref="IEnumerable{T}"/> to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{T}"/> whose elements are copied to the <see cref="CollectionBuilder{T}"/>.</param>
        public void AppendRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items is T[] array)
            {
                AppendRange(array.AsSpan());
            }
            else if (items is List<T> list)
            {
                AppendRange(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list));
            }
            else if (items is ICollection<T> collection)
            {
                AppendICollectionRangeInternal(collection);
            }
            else
            {
                AppendIEnumerableRangeInternal(items);
            }
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
                Append(MemoryMarshal.GetReference(items)); // Append(items[0])
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
                items.CopyTo(MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), countInCurrentSegment),
                    itemsLength
                ));
                countInCurrentSegment += itemsLength;
            }
            else
            {
                ref T srcRef = ref MemoryMarshal.GetReference(items);
                var src1 = MemoryMarshal.CreateSpan(ref srcRef, remainingCapacityInCurrentSegment);
                var src2 = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref srcRef, remainingCapacityInCurrentSegment),
                    itemsLength - remainingCapacityInCurrentSegment
                );

                src1.CopyTo(MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), countInCurrentSegment),
                    remainingCapacityInCurrentSegment
                ));

                Grow(src2.Length);
                currentSegment = _currentSegment;
                src2.CopyTo(currentSegment);

                countInCurrentSegment = src2.Length;
            }
            _count += itemsLength;

            if (countInCurrentSegment == currentSegment.Length)
            {
                _growIsNeeded = true;
            }
            _countInCurrentSegment = countInCurrentSegment;
            return;
        }

        /// <summary>
        /// Create sequence of span over the collection builder with a buffer.
        /// </summary>
        /// <returns>A <see cref="SpanSequence{TElement, TSegmentList}"/> over the collection builder.</returns>
        public readonly SpanSequence<T, Segments> AsSpanSequence()
        {
            var bufferArray = new int[_segmentCount];
            return AsSpanSequenceInternal(bufferArray);
        }

        /// <summary>
        /// Create sequence of span over the collection builder with a buffer.
        /// </summary>
        /// <param name="buffer">
        /// <para>A span whose length is equal to or greater than the number of segments contained in the collection builder.</para>
        /// <para>Recommended: 27-length span.</para>
        /// </param>
        /// <returns>A <see cref="SpanSequence{TElement, TSegmentList}"/> over the collection builder.</returns>
        public readonly SpanSequence<T, Segments> AsSpanSequence(Span<int> buffer)
        {
            var segmentCount = _segmentCount;
            if (segmentCount == buffer.Length)
            {
                return AsSpanSequenceInternal(buffer);
            }
            else if (segmentCount < buffer.Length)
            {
                return AsSpanSequenceInternal(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(buffer), segmentCount));
            }
            else
            {
                var bufferArray = new int[segmentCount];
                return AsSpanSequenceInternal(bufferArray.AsSpan());
            }
        }

        /// <summary>
        /// Create sequence of span over the collection builder with a buffer.
        /// </summary>
        /// <param name="countBefore">A span whose length is equal to <see cref="_segmentCount"/>.</param>
        /// <returns>A <see cref="SpanSequence{TElement, TSegmentList}"/> over the collection builder.</returns>
        private readonly SpanSequence<T, Segments> AsSpanSequenceInternal(Span<int> countBefore)
        {
            var segmentCount = _segmentCount;
            if (segmentCount == 0)
            {
                return SpanSequence<T, Segments>.Empty;
            }

            var countInSegment = _segmentLength;
            Segments segments = new(_segments, countInSegment, segmentCount);

            Span<int> countInSegment_Span = countInSegment;
            int countSum = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                countBefore.UnsafeAccess(i) = countSum;
                countSum += countInSegment_Span.UnsafeAccess(i);
            }

            return new(segments, countBefore, 0);
        }

        private readonly int ComputeNextSegmentLength()
        {
            int segmentCount = _segmentCount;
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

#if NET9_0_OR_GREATER
        readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            int count = _count;
            if (count == 0)
            {
                return;
            }
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            if ((uint)(array.Length - arrayIndex) < count)
            {
                ThrowHelpers.ThrowArgumentException("The number of elements in the source collection is greater than the available space from `arrayIndex` to the end of the destination array.");
            }
            CopyToInternal(MemoryMarshal.CreateSpan(
                ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), arrayIndex),
                count
            ));
        }
#endif

        /// <summary>
        /// Copies elements in the <see cref="CollectionBuilder{T}"/> to the specified span.
        /// </summary>
        /// <param name="destination"></param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> doesn't have enough space to accommodate elements copied.</exception>
        public readonly void CopyTo(Span<T> destination)
        {
            int count = _count;
            if (count == 0)
            {
                return;
            }
            if (destination.Length < count)
            {
                ThrowHelpers.ThrowArgumentException("The span doesn't have enough space to accommodate elements copied.", nameof(destination));
            }
            CopyToInternal(destination);
        }

        private readonly void CopyToInternal(Span<T> destination)
        {
            ref T destinationRef = ref MemoryMarshal.GetReference(destination);
            int remainsCount = _count;
            int segmentCount = _segmentCount;
            ReadOnlySpan<T[]> segments = _allocatedSegments;
            ReadOnlySpan<int> segmentLength = _segmentLength;

            for (int i = 0; i < segments.Length; i++)
            {
                int srcLen = segmentLength.UnsafeAccess(i);
                if (srcLen == 0)
                {
                    continue;
                }

                Debug.Assert((uint)srcLen <= segments[i].Length, "Tried to create too long span or negative-length span.");
                Debug.Assert(
                    (ulong)Unsafe.ByteOffset(ref destinationRef, ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), destination.Length)).ToInt64() >= (ulong)(Math.Min(srcLen, remainsCount) * Unsafe.SizeOf<T>()),
                    "The remaining capacity of destination span is less than the number of elements to copy."
                );

                ReadOnlySpan<T> src;
                if (remainsCount <= srcLen)
                {
                    src = MemoryMarshal.CreateReadOnlySpan(
                        ref MemoryMarshal.GetArrayDataReference(segments[i]),
                        remainsCount
                    );
                    src.CopyTo(MemoryMarshal.CreateSpan(ref destinationRef, remainsCount));
                    break;
                }

                src = MemoryMarshal.CreateReadOnlySpan(
                    ref MemoryMarshal.GetArrayDataReference(segments[i]),
                    srcLen
                );
                src.CopyTo(MemoryMarshal.CreateSpan(ref destinationRef, srcLen));
                remainsCount -= srcLen;
                destinationRef = ref Unsafe.Add(ref destinationRef, srcLen);
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public readonly void Dispose()
        {
            var usesArrayPool = _usesArrayPool;
            var isTRef = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
            if (!usesArrayPool && !isTRef)
            {
                return;
            }

            int segmentCount = _segmentCount;
            if (segmentCount == 0)
            {
                return;
            }

            var segments = _allocatedSegments;
            ReadOnlySpan<int> segmentLength = _segmentLength;

            if (usesArrayPool)
            {
                if (isTRef)
                {
                    for (int i = 0; i < segments.Length; i++)
                    {
                        var segArray = segments[i];
                        var seg = MemoryMarshal.CreateSpan(
                            ref MemoryMarshal.GetArrayDataReference(segments[i]),
                            segmentLength.UnsafeAccess(i)
                        );
                        seg.Clear();
                        ReturnRentedArray(segArray);
                    }
                }
                else
                {
                    for (int i = 0; i < segments.Length; i++)
                    {
                        var segArray = segments[i];
                        ReturnRentedArray(segArray);
                    }
                }
            }
            else // In this case, `isTRef` is always true.
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    var seg = MemoryMarshal.CreateSpan(
                        ref MemoryMarshal.GetArrayDataReference(segments[i]),
                        segmentLength.UnsafeAccess(i)
                    );
                    seg.Clear();
                }
            }
        }

        private void ExpandCurrentSegment(int minimumLength)
        {
            var segmentCount = _segmentCount;
            if (segmentCount == 0)
            {
                return;
            }

            var oldSegment = _currentSegment;
            if (minimumLength <= oldSegment.Length)
            {
                return;
            }

            Span<T[]> segments = _segments;
            var countInCurrentSegment = _countInCurrentSegment;
            Span<T> cpySrc = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(oldSegment), countInCurrentSegment);

            T[] newSegmentArray;
            Span<T> newSegment;

            if (_usesArrayPool)
            {
                newSegmentArray = RentArray(minimumLength);
                newSegment = newSegmentArray.AsSpan();
                cpySrc.CopyTo(newSegment);

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    cpySrc.Clear();
                }
                ReturnRentedArray(segments.UnsafeAccess(segmentCount - 1));
            }
            else
            {
                newSegmentArray = GC.AllocateUninitializedArray<T>(minimumLength);
                newSegment = newSegmentArray.AsSpan();
                cpySrc.CopyTo(newSegment);

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    cpySrc.Clear();
                }
            }

            segments.UnsafeAccess(segmentCount - 1) = newSegmentArray;
            _currentSegment = newSegment;

            Span<int> segmentLength = _segmentLength;
            segmentLength.UnsafeAccess(segmentCount - 1) = newSegment.Length;
        }


        /// <summary>
        /// Returns the total length of segments contained in the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <returns>The total length of segments contained in the <see cref="CollectionBuilder{T}"/>.</returns>
        public readonly int GetAllocatedCapacity()
        {
            var segmentCount = _segmentCount;
            if (segmentCount == 0)
            {
                return 0;
            }
            else if (segmentCount == 1)
            {
                return _segmentLength[0];
            }

            ReadOnlySpan<int> segmentLength = _segmentLength;
            int capacity = 0;
            for (int i = 0; i < segmentLength.Length; i++)
            {
                capacity += segmentLength[i];
            }
            return capacity;
        }

        /// <summary>
        /// Allocates new buffer than can accommodate at least <see cref="_nextSegmentLength"/> elements.
        /// </summary>
        private void Grow()
            => GrowExact(ComputeNextSegmentLength());

        /// <summary>
        /// Allocates new buffer than can accommodate at least specified number of elements.
        /// </summary>
        /// <remarks>
        /// If <paramref name="neededLength"/> is greater than <see cref="_nextSegmentLength"/>, this method allocate new buffer that can accommodate <paramref name="neededLength"/> elements; otherwise, <see cref="_nextSegmentLength"/> elements.
        /// </remarks>
        /// <param name="neededLength"></param>
        private void Grow(int neededLength)
            => GrowExact(Math.Max(neededLength, ComputeNextSegmentLength()));

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

            T[] newSegmentArray = _usesArrayPool ? RentArray(length) : GC.AllocateUninitializedArray<T>(length);
            var newSegment = newSegmentArray.AsSpan();

            Debug.Assert((uint)segmentCount < CollectionBuilderConstants.MaxSegmentCount, "Invalid segment count.");
            _allocatedSegments = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(_allocatedSegments), segmentCount + 1);
            _countInCurrentSegment = 0;
            _currentSegment = newSegment;
            _growIsNeeded = false;
            _nextSegmentLength = checked(_nextSegmentLength << 1);
            _segmentCount = segmentCount + 1;

            Span<T[]> segments = _segments;
            segments.UnsafeAccess(segmentCount) = newSegmentArray;

            // _segmentLength[segmentCount] = newSegment.Length;
            Span<int> segmentLength = _segmentLength;
            segmentLength.UnsafeAccess(segmentCount) = newSegment.Length;
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
                _countInCurrentSegment -= length;
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
            int segmentCount = _segmentCount;
            Span<int> segmentLength = _segmentLength;
            int countInCurrentSegment = _countInCurrentSegment;
            T[] currentSegmentArray = segments.UnsafeAccess(segmentCount - 1);
            Span<T> currentSegment = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_currentSegment), _countInCurrentSegment);

            while (countInCurrentSegment < length)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    currentSegment.Clear();
                }
                if (_usesArrayPool)
                {
                    ReturnRentedArray(currentSegmentArray);
                }

                segmentCount--;
                segments.UnsafeAccess(segmentCount) = [];
                segmentLength.UnsafeAccess(segmentCount) = 0;

                length -= countInCurrentSegment;
                countInCurrentSegment = segmentLength.UnsafeAccess(segmentCount - 1);
                currentSegmentArray = segments.UnsafeAccess(segmentCount - 1);
                currentSegment = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(currentSegmentArray), countInCurrentSegment);
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), countInCurrentSegment - length),
                    length
                ).Clear();
            }

            _currentSegment = currentSegment;
            _countInCurrentSegment = countInCurrentSegment - length;
            _growIsNeeded = false;
        }

        /// <summary>
        /// Reserves the specified length of memory region from the back of the <see cref="CollectionBuilder{T}"/> and returns the span over the region.
        /// </summary>
        /// <param name="length">The number of elements that the reserved memory region can exactly accommodate.</param>
        /// <returns>The span over the reserved memory region.</returns>
        public Span<T> ReserveRange(int length)
        {
            if (length == 0)
            {
                return [];
            }

            Reserve(length);
            Debug.Assert(checked(_countInCurrentSegment + length) <= _currentSegment.Length);
            return MemoryMarshal.CreateSpan(
                ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSegment), _countInCurrentSegment),
                length
            );
        }

        /// <summary>
        /// Reserves specified length of contiguous memory region from the back of the <see cref="CollectionBuilder{T}"/> and returns a span over the region.
        /// </summary>
        /// <param name="length">The number of elements that the reserved memory region can exactly accommodates.</param>
        /// <returns>A span over the reserved memory region.</returns>
        /// <exception cref="NotImplementedException"></exception>
#if NET7_0_OR_GREATER
        [UnscopedRef]
#endif
        public Span<T> ReserveSpan(int length)
        {
            if (length == 0)
            {
                return [];
            }
            else if (length == 1)
            {
                Append(default!);
#if NET7_0_OR_GREATER
                return new Span<T>(ref _currentSegment.UnsafeAccess(_countInCurrentSegment - 1));
#else
                return MemoryMarshal.CreateSpan(ref _currentSegment.UnsafeAccess(_countInCurrentSegment - 1), 1);
#endif
            }
            else
            {
                Reserve(length);
                return MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSegment), _countInCurrentSegment - length),
                    length
                );
            }
        }

        /// <summary>
        /// Ensures that the current segment has enough space to accommodate specified length of items and reserves contiguous memory region over the range.
        /// </summary>
        /// <param name="length"></param>
        private void Reserve(int length)
        {
            var countInCurrentSegment = _countInCurrentSegment;

            if (_growIsNeeded)
            {
                Grow(length);
            }
            else
            {
                var currentSegment = _currentSegment;

                if (currentSegment.Length - countInCurrentSegment < length)
                {
                    var nextSegmentLength = ComputeNextSegmentLength();
                    var neededLength = countInCurrentSegment + length;

                    if (countInCurrentSegment < nextSegmentLength && neededLength < checked(currentSegment.Length + nextSegmentLength))
                    {
                        ExpandCurrentSegment(neededLength);
                    }
                    else
                    {
                        Span<int> segmentLength = _segmentLength;
                        segmentLength.UnsafeAccess(_segmentCount - 1) = countInCurrentSegment;
                        GrowExact(Math.Max(length, nextSegmentLength));
                    }
                }
            }

            _count += length;
            countInCurrentSegment = (_countInCurrentSegment += length);
            if (_currentSegment.Length == countInCurrentSegment)
            {
                _growIsNeeded = true;
            }
        }

        /// <summary>
        /// Reserves specified length of memory from the back of the <see cref="CollectionBuilder{T}"/> and returns a span over the region.
        /// </summary>
        /// <remarks>
        /// The reserved memory consists of contiguous regions.
        /// </remarks>
        /// <param name="length">The number of elements that the reserved memory can exactly accommodates.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void ReserveSpanSequence(int length)
        {
            throw new NotImplementedException();
        }

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
            T[] array = GC.AllocateUninitializedArray<T>(_count);
            CopyTo(array.AsSpan());
            return array;
        }

        public struct Segments : IRefList<T>
        {
            /// <summary>
            /// Number of elements in segments.
            /// </summary>
#if NET8_0_OR_GREATER
            private CollectionBuilderConstants.InternalArray<int> _countInSegment;
#else
            private readonly int[] _countInSegment;
#endif

            /// <summary>
            /// Sequence of <typeparamref name="T"/>[] that has fixed capacity.
            /// </summary>
#if NET8_0_OR_GREATER
            private CollectionBuilderConstants.InternalArray<T[]> _segments;
#else
            private readonly T[][] _segments;
#endif

            /// <summary>
            /// The number of segments contained in a collection builder.
            /// </summary>
            public readonly int Count;

            readonly int IRefList<T>.Count => Count;

            public readonly Span<T> this[int index]
            {
                get
                {
                    if ((uint)index >= Count)
                    {
                        ThrowHelpers.ThrowIndexOutOfRangeException();
                    }
                    ReadOnlySpan<T[]> segments = _segments;
                    ReadOnlySpan<int> counts = _countInSegment;
                    return MemoryMarshal.CreateSpan(
                        ref MemoryMarshal.GetArrayDataReference(segments.UnsafeAccess(index)),
                        counts.UnsafeAccess(index)
                    );
                }
            }

#if NET8_0_OR_GREATER
            internal Segments(CollectionBuilderConstants.InternalArray<T[]> segments, CollectionBuilderConstants.InternalArray<int> countInSegment, int segmentCount)
            {
                _countInSegment = countInSegment;
                _segments = segments;
                Count = segmentCount;
            }
#else
            internal Segments(T[][] segments, int[] countInSegment, int segmentCount)
            {
                _countInSegment = countInSegment;
                _segments = segments;
                Count = segmentCount;
            }
#endif
        }
    }
}
