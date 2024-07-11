using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    /// <summary>
    /// Represents a temporary collection that is used to build new collections.
    /// </summary>
    /// <remarks>
    /// This type is not ref-structure, so it can be used in situations where ref-structure cannot be used such as in async methods.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public struct NonRefCollectionBuilder<T> : IDisposable
    {
        private int _allocatedCount = 0;
        private int _count = 0;
        private int _countInCurrentSegment = 0;

        private BuffersContainer<T> _segments = new();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// Initializes a collection builder whose fields are set to default value.
        /// </summary>
        public NonRefCollectionBuilder() { }

        /// <summary>
        /// Initializes a collection builder whose first segment can contain at least specified number of elements.
        /// </summary>
        /// <param name="firstSegmentMinLength">The number of elements that can be contained in the first segment.</param>
        public NonRefCollectionBuilder(int firstSegmentMinLength)
        {
            _segments = new(firstSegmentMinLength);
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            _segments.Dispose();
        }

        /// <summary>
        /// Adds a element to the back of the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="item">An object to add.</param>
        /// <exception cref="Exception">The <see cref="NonRefCollectionBuilder{T}"/> is already full.</exception>
        public void Add(T item)
        {
            if (_count == Array.MaxLength)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            if (_allocatedCount == 0 || _countInCurrentSegment == _segments.CurrentSegmentCapacity)
            {
                Grow();
                _segments.CurrentSegment[0] = item;
                _count++;
                _countInCurrentSegment = 1;
                return;
            }

            _segments.ExpandCurrentSegmentWithoutResizing();
            _segments.CurrentSegment[_countInCurrentSegment] = item;
            _count++;
            _countInCurrentSegment++;
        }

        /// <summary>
        /// Adds elements in an <see cref="ICollection{T}"/> to the back of the <see cref="NonRefCollectionBuilder{T}"/>
        /// </summary>
        /// <param name="items">An <see cref="ICollection{T}"/> whose elements are copied to the <see cref="NonRefCollectionBuilder{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        public void AddICollectionRange(ICollection<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            AddICollectionRangeInternal(items);
        }

        internal void AddICollectionRangeInternal([NotNull] ICollection<T> items)
        {
            int srcCount = items.Count;
            if (srcCount == 0)
            {
                return;
            }

            if (Array.MaxLength - _count < srcCount)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionHasNoEnoughSpaceToAccommodateItems, nameof(items));
            }

            int currentSegmentCapacity; // this must be set to `_segments.CurrentSegmentCapacity` on the next if statement

            if (_allocatedCount == 0 || _countInCurrentSegment == (currentSegmentCapacity = _segments.CurrentSegmentCapacity))
            {
                Grow(Math.Max(srcCount, _segments.DefaultNextSegmentLength));
            }
            else if (checked(currentSegmentCapacity + _segments.DefaultNextSegmentLength) <= _countInCurrentSegment + srcCount)
            {
                _segments.ShrinkCurrentSegment(_countInCurrentSegment);
                Grow(srcCount);
            }
            else
            {
                _segments.ExpandCurrentSegment(_countInCurrentSegment + srcCount);
            }

            items.CopyTo(_segments.ArrayAt(_allocatedCount - 1), _countInCurrentSegment);
            _count += srcCount;
            _countInCurrentSegment += srcCount;
        }

        /// <summary>
        /// Adds elements in an <see cref="IEnumerable{T}"/> to the back of the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        /// <remarks>
        /// This implementation assumes that <paramref name="items"/> is NOT <see cref="ICollection{T}"/> and thus doesn't check if it is.
        /// </remarks>
        /// <param name="items">An <see cref="IEnumerable{T}"/> whose elements are copied to the <see cref="NonRefCollectionBuilder{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        public void AddIEnumerableRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            AddIEnumerableRangeInternal(items);
        }

        private void AddIEnumerableRangeInternal([NotNull] IEnumerable<T> items)
        {
            var enumerator = items.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                // The source collection is empty.
                return;
            }

            if (_allocatedCount == 0)
            {
                Grow();
            }
            else
            {
                _segments.ExpandCurrentSegmentWithoutResizing();
                if (_countInCurrentSegment == _segments.CurrentSegmentLength)
                {
                    Grow();
                }
            }

            var currentSegment = _segments[_allocatedCount - 1];
            ref T destRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), _countInCurrentSegment);

            // add first element of `items`
            destRef = enumerator.Current;
            destRef = ref Unsafe.Add(ref destRef, 1);
            _count++;
            _countInCurrentSegment++;

            while (enumerator.MoveNext())
            {
                if (_countInCurrentSegment == currentSegment.Length)
                {
                    Grow();
                    currentSegment = _segments[_allocatedCount - 1];
                    destRef = ref MemoryMarshal.GetReference(currentSegment);
                }

                destRef = enumerator.Current;
                destRef = ref Unsafe.Add(ref destRef, 1);
                _count++;
                _countInCurrentSegment++;
            }
        }

        /// <summary>
        /// Adds elements in an <see cref="IEnumerable{T}"/> to the back of the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{T}"/> whose elements are copied to the <see cref="NonRefCollectionBuilder{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        public void AddRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (CollectionHelpers.TryGetReadOnlySpan(items, out var span))
            {
                AddRange(span);
            }
            else if (items is ICollection<T> collection)
            {
                AddICollectionRangeInternal(collection);
            }
            else
            {
                AddIEnumerableRangeInternal(items);
            }
        }

        /// <summary>
        /// Adds elements in the span to the back of the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">A span over elements to add.</param>
        /// <exception cref="ArgumentException">The <see cref="NonRefCollectionBuilder{T}"/> doesn't have enough space to accommodate elements contained in <paramref name="items"/>.</exception>
        public void AddRange(ReadOnlySpan<T> items)
        {
            if (items.IsEmpty)
            {
                return;
            }

            var itemsLength = items.Length;
            if (Array.MaxLength - _count < itemsLength)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionHasNoEnoughSpaceToAccommodateItems, nameof(items));
            }

            int currentSegmentCapacity; // this must be set to `_segments.CurrentSegmentCapacity` on the next if statement.

            if (_allocatedCount == 0 || _countInCurrentSegment == (currentSegmentCapacity = _segments.CurrentSegmentCapacity))
            {
                Grow(Math.Max(itemsLength, _segments.DefaultNextSegmentLength));
                items.CopyTo(_segments[_allocatedCount - 1]);
                _count += itemsLength;
                _countInCurrentSegment = itemsLength;
                return;
            }

            var currentSegment = _segments[_allocatedCount - 1];
            _segments.ExpandCurrentSegmentWithoutResizing();

            if (currentSegmentCapacity - _countInCurrentSegment >= itemsLength)
            {
                items.CopyTo(MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), _countInCurrentSegment),
                    itemsLength
                ));
                _count += itemsLength;
                _countInCurrentSegment += itemsLength;
            }
            else
            {
                ref var itemsRef = ref MemoryMarshal.GetReference(items);

                var src1 = MemoryMarshal.CreateReadOnlySpan(ref itemsRef, currentSegmentCapacity - _countInCurrentSegment);
                src1.CopyTo(MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), _countInCurrentSegment),
                    src1.Length
                ));

                var src2 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref itemsRef, src1.Length), itemsLength - src1.Length);
                Grow(Math.Max(src2.Length, _segments.DefaultNextSegmentLength));
                src2.CopyTo(_segments[_allocatedCount - 1]);

                _count += itemsLength;
                _countInCurrentSegment = src2.Length;
            }
        }

        /// <summary>
        /// Returns the number of elements that can be contained in the <see cref="NonRefCollectionBuilder{T}"/> without allocate new internal array.
        /// </summary>
        /// <returns>The number of elements that can be contained in the <see cref="NonRefCollectionBuilder{T}"/> without allocating new internal array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetAllocatedCapacity()
        {
            int capacity = 0;
            for (int i = 0; i < _allocatedCount; i++)
            {
                capacity += _segments[i].Length;
            }
            return capacity;
        }

        private void Grow()
        {
            _segments.AllocateNewBuffer();
            _allocatedCount++;
            _countInCurrentSegment = 0;
        }

        private void Grow(int minimumLength)
        {
            _segments.AllocateNewBuffer(minimumLength);
            _allocatedCount++;
            _countInCurrentSegment = 0;
        }

        /// <summary>
        /// Copies elements in the <see cref="NonRefCollectionBuilder{T}"/> to the specified span.
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

            for (int i = 0; i < _allocatedCount; i++)
            {
                var segment = _segments[i];
                var segmentLength = segment.Length;

                if (remainsCount <= segmentLength)
                {
                    MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(segment), remainsCount)
                        .CopyTo(MemoryMarshal.CreateSpan(ref destRef, segmentLength));
                    break;
                }

                segment.CopyTo(MemoryMarshal.CreateSpan(ref destRef, segmentLength));
                destRef = ref Unsafe.Add(ref destRef, segmentLength);
                remainsCount -= segmentLength;
            }
        }

        /// <summary>
        /// Reserves contiguous region of memory from the <see cref="NonRefCollectionBuilder{T}"/> to write specified number of elements and returns span over the memory region.
        /// </summary>
        /// <param name="length">The length of the returned span.</param>
        /// <returns>The span over the <see cref="NonRefCollectionBuilder{T}"/> which can accommodate exactly specified number of elements.</returns>
        public Span<T> ReserveRange(int length)
        {
            if (Array.MaxLength - _count < length)
            {
                ThrowHelpers.ThrowArgumentException("The value is greater than the maximum number of elements that can be added to this collection.", nameof(length));
            }
            if (length == 0)
            {
                return [];
            }

            if (_allocatedCount == 0 || _countInCurrentSegment == _segments.CurrentSegmentCapacity)
            {
                Grow(Math.Max(length, _segments.DefaultNextSegmentLength));
                _count += length;
                _countInCurrentSegment += length;
                return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_segments[_allocatedCount - 1]), length);
            }

            Span<T> currentSegment = _segments.CurrentSegment;
            Span<T> dest;
            _segments.ExpandCurrentSegmentWithoutResizing();

            if (currentSegment.Length - _countInCurrentSegment >= length)
            {
                dest = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), _countInCurrentSegment),
                    length
                );
            }
            else if (_countInCurrentSegment + length < checked(_segments.DefaultNextSegmentLength + currentSegment.Length))
            {
                _segments.ExpandCurrentSegment(_countInCurrentSegment + length);
                dest = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(_segments[_allocatedCount - 1]), _countInCurrentSegment),
                    length
                );
            }
            else
            {
                _segments.ShrinkCurrentSegment(_countInCurrentSegment);
                Grow(_countInCurrentSegment); // it is ensured that `_countInCurrentSegment` is greater than `_segment.DefaultNextSegmentLength`
                dest = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_segments[_allocatedCount - 1]), length);
            }

            _count += length;
            _countInCurrentSegment += length;
            return dest;
        }

        /// <summary>
        /// Remove specified number of elements from the back of the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="length">The number of elements to remove.</param>
        public void RemoveRange(int length)
        {
            if ((uint)length > (uint)_countInCurrentSegment)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(length), "The value is greater than the number of elements that can be removed.");
            }
            
            _count -= length;
            _countInCurrentSegment -= length;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                var span = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(_segments[_allocatedCount - 1]), _countInCurrentSegment),
                    length
                );
                span.Clear();
            }
        }

        /// <summary>
        /// Creates an array from the <see cref="NonRefCollectionBuilder{T}"/> and returns it.
        /// </summary>
        /// <returns>An array which contains elements copied from the <see cref="NonRefCollectionBuilder{T}"/>.</returns>
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
