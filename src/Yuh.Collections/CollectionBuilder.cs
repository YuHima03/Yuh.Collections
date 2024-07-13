using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    internal static class CollectionBuilder
    {
        internal const int MaxArrayLengthFromArrayPool = 1024 * 1024;

#if NET8_0_OR_GREATER
        internal const int MinArrayLengthFromArrayPool = 2048;
#elif NET7_0_OR_GREATER
        internal const int MinArrayLengthFromArrayPool = 512;
#else
        internal const int MinArrayLengthFromArrayPool = 128;
#endif

        internal const int MinSegmentLength = 16;
        internal const int SegmentsContainerLength = 27;

#if NET8_0_OR_GREATER
        [InlineArray(SegmentsContainerLength)]
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
    public unsafe ref struct CollectionBuilder<T>// : IDisposable
    {
        private int _count = 0;
        private int _countInCurrentSegment = 0;
        private Span<T> _currentSegment = [];
        private bool _growIsNeeded = true;
        private int _nextSegmentLength = CollectionBuilder.MinSegmentLength;
        private int _segmentsCount = 0; // in the range [0, 27]
        private fixed int _segmentsLength[32]; // set the length 32 for SIMD operations

#if NET8_0_OR_GREATER
        private CollectionBuilder.Array27<T[]> _segments;
#else
        private readonly T[][] _segmentsArray = new T[CollectionBuilder.SegmentsContainerLength][];
        private readonly Span<T[]> _segments;
#endif

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
            int itemsLength = items.Count;
            if (itemsLength == 0)
            {
                return;
            }
            else if (itemsLength == 1)
            {
                Append(items.First());
                return;
            }

            var countInCurrentSegment = _countInCurrentSegment;

            if (_growIsNeeded)
            {
                Grow(itemsLength);
                items.CopyTo(_segments[_segmentsCount - 1], 0);
                countInCurrentSegment = itemsLength;
            }
            else if (itemsLength <= _currentSegment.Length - countInCurrentSegment)
            {
                items.CopyTo(_segments[_segmentsCount - 1], countInCurrentSegment);
                countInCurrentSegment += itemsLength;
            }
            else if (countInCurrentSegment < _nextSegmentLength && countInCurrentSegment + itemsLength < checked(_currentSegment.Length + _nextSegmentLength))
            {
                ExpandCurrentSegment(_countInCurrentSegment + itemsLength);
                items.CopyTo(_segments[_segmentsCount - 1], _countInCurrentSegment);
                countInCurrentSegment += itemsLength;
            }
            else
            {
                ShrinkCurrentSegmentToFit();
                Grow(itemsLength);
                items.CopyTo(_segments[_segmentsCount - 1], 0);
                countInCurrentSegment = itemsLength;
            }

            _count += itemsLength;
            _countInCurrentSegment = countInCurrentSegment;
            _growIsNeeded = (countInCurrentSegment == _currentSegment.Length);
            return;
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
            ref T destRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), countInCurrentSegment);

            foreach (var item in items)
            {
                if (countInCurrentSegment == currentSegment.Length)
                {
                    _count += countInCurrentSegment - _countInCurrentSegment;

                    Grow();
                    countInCurrentSegment = 0;
                    currentSegment = _currentSegment;
                    destRef = ref MemoryMarshal.GetReference(currentSegment);
                }

                destRef = item;
                destRef = ref Unsafe.Add(ref destRef, 1);
                countInCurrentSegment++;
            }

            _count += countInCurrentSegment - _countInCurrentSegment;
            _countInCurrentSegment = countInCurrentSegment;
            _growIsNeeded = (countInCurrentSegment == currentSegment.Length);
        }

        /// <summary>
        /// Adds elements in the span to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">A span over elements to add.</param>
        /// <exception cref="ArgumentException">The <see cref="CollectionBuilder{T}"/> doesn't have enough space to accommodate elements contained in <paramref name="items"/>.</exception>
        public void AppendRange(scoped ReadOnlySpan<T> items)
        {
            if (items.Length == 0)
            {
                return;
            }
            else if (items.Length == 1)
            {
                Append(items[0]);
                return;
            }
            if (items.Length > Array.MaxLength - _count) // use this way to avoid overflow
            {
                ThrowHelpers.ThrowArgumentException("This collection doesn't have enough space to accommodate elements of the specified span.", nameof(items));
            }

            var currentSegment = _currentSegment;

            if (_countInCurrentSegment + items.Length <= currentSegment.Length)
            {
                items.CopyTo(MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), _countInCurrentSegment),
                    items.Length
                ));
                _count += items.Length;
                _countInCurrentSegment += items.Length;
                _growIsNeeded = (_countInCurrentSegment == currentSegment.Length);
                return;
            }
            else
            {
                ref var itemsRef = ref MemoryMarshal.GetReference(items);

                var source1 = MemoryMarshal.CreateReadOnlySpan(ref itemsRef, currentSegment.Length - _countInCurrentSegment);
                source1.CopyTo(MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), _countInCurrentSegment),
                    source1.Length
                ));

                var source2 = MemoryMarshal.CreateReadOnlySpan(
                    ref Unsafe.Add(ref itemsRef, source1.Length),
                    items.Length - source1.Length
                );

                Grow(source2.Length);
                currentSegment = _currentSegment;

                source2.CopyTo(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(currentSegment), source2.Length));

                _count += items.Length;
                _countInCurrentSegment += source2.Length;
                _growIsNeeded = (_countInCurrentSegment == currentSegment.Length);
                return;
            }
        }

        private static T[] AllocateNewArray(int length)
        {
            if (length < CollectionBuilder.MinArrayLengthFromArrayPool)
            {
                return new T[length];
            }
            else if (length <= CollectionBuilder.MaxArrayLengthFromArrayPool)
            {
                return ArrayPool<T>.Shared.Rent(length);
            }
            else
            {
                return GC.AllocateUninitializedArray<T>(length);
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

            int remainsCount = _count;
            ref T destRef = ref MemoryMarshal.GetReference(destination);

            for (int i = 0; i < CollectionBuilder.SegmentsContainerLength; i++)
            {
                var segment = GetSegmentAt(i);

                if (remainsCount <= segment.Length)
                {
                    MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(segment), remainsCount)
                        .CopyTo(MemoryMarshal.CreateSpan(ref destRef, remainsCount));
                    break;
                }

                segment.CopyTo(MemoryMarshal.CreateSpan(ref destRef, segment.Length));
                destRef = ref Unsafe.Add(ref Unsafe.AsRef(in destRef), segment.Length);
                remainsCount -= segment.Length;
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public readonly void Dispose()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                for (int i = 0; i < _segmentsCount; i++)
                {
                    var segment = _segments[i];
                    if ((uint)(segment.Length - MinArrayLengthFromArrayPool) <= (MaxArrayLengthFromArrayPool - MinArrayLengthFromArrayPool)) // MinArrayLengthFromArrayPool <= segment.Length && segment.Length <= MaxArrayLengthFromArrayPool
                    {
                        GetSegmentAt(i).Clear();
                        ArrayPool<T>.Shared.Return(_segments[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _segmentsCount; i++)
                {
                    var segment = _segments[i];
                    if ((uint)(segment.Length - MinArrayLengthFromArrayPool) <= (MaxArrayLengthFromArrayPool - MinArrayLengthFromArrayPool)) // MinArrayLengthFromArrayPool <= segment.Length && segment.Length <= MaxArrayLengthFromArrayPool
                    {
                        ArrayPool<T>.Shared.Return(_segments[i]);
                    }
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
            if (MinArrayLengthFromArrayPool <= oldSegment.Length && oldSegment.Length <= MaxArrayLengthFromArrayPool)
            {
                ArrayPool<T>.Shared.Return(oldSegment);
            }

            _currentSegment = newSegmentSpan;
            _segmentsLength[_segmentsCount - 1] = newSegment.Length;
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

        /// <remarks>
        /// To get current segment, refer <see cref="_currentSegment"/> instead of using this method.
        /// </remarks>
        private readonly Span<T> GetSegmentAt(int index)
        {
            return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_segments[index]), _segmentsLength[index]);
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
            if (_segmentsCount == CollectionBuilder.SegmentsContainerLength)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            var newSegment = AllocateNewArray(length);
            _segments[_segmentsCount] = newSegment;
            _segmentsLength[_segmentsCount] = newSegment.Length;

            _countInCurrentSegment = 0;
            _currentSegment = newSegment.AsSpan();
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

        public void RemoveRange(int length)
        {
            if (length == 0)
            {
                return;
            }
            if ((uint)length > _countInCurrentSegment)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(length), "The value is greater than the number of elements in the current segment.");
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
                range = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_currentSegment), length);
            }
            else if (length <= currentSegment.Length - countInCurrentSegment)
            {
                range = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(currentSegment), countInCurrentSegment),
                    length
                );
            }
            else if (countInCurrentSegment < _nextSegmentLength && countInCurrentSegment + length < checked(currentSegment.Length + _nextSegmentLength))
            {
                ExpandCurrentSegment(countInCurrentSegment + length);
                range = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSegment), countInCurrentSegment),
                    length
                );
            }
            else
            {
                ShrinkCurrentSegmentToFit();
                Grow(length);
                range = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_currentSegment), length);
            }

            _count += length;
            _countInCurrentSegment += length;
            _growIsNeeded = (_countInCurrentSegment == _currentSegment.Length);
            return range;
        }

        private void ShrinkCurrentSegmentToFit()
        {
            _currentSegment = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_currentSegment), _countInCurrentSegment);
            _segmentsLength[_segmentsCount - 1] = _countInCurrentSegment;
        }

        /// <summary>
        /// Creates an array from the <see cref="CollectionBuilder{T}"/> and returns it.
        /// </summary>
        /// <returns>An array which contains elements copied from the <see cref="CollectionBuilder{T}"/>.</returns>
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
