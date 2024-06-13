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
        private int _allocatedCount = 0; // in the range [0, 31]
        private int _count = 0;
        private Span<T> _currentSegment = [];
        private int _countInCurrentSegment = 0;
        private readonly Span<T[]> _segments; // { T[1], T[2], T[4], ..., T[2^30] }


        /// <summary>
        /// The number of elements that can be contained in the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        public readonly int Capacity;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionBuilder{T}"/> type which has used-specified span that accomodate elements.
        /// </summary>
        /// <param name="segments">A span used to keep elements added to this collection.</param>
        public CollectionBuilder(Span<T[]> segments)
        {
            if (segments.Length <= 31)
            {
                _segments = segments;
                Capacity = (int)((1U << segments.Length) - 1);
            }
            else
            {
                _segments = segments[..31];
                Capacity = int.MaxValue;
            }
        }

        /// <summary>
        /// Adds a element to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="item">An object to add.</param>
        /// <exception cref="Exception">The <see cref="CollectionBuilder{T}"/> is already full.</exception>
        public void Add(T item)
        {
            if (_count == Capacity)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow()
        {
            int nextSegmentLength = (_allocatedCount == 0) ? 1 : (_currentSegment.Length << 1);
            var nextSegment = GC.AllocateUninitializedArray<T>(nextSegmentLength);
            _segments[_allocatedCount] = nextSegment;
            _currentSegment = nextSegment.AsSpan();
            _countInCurrentSegment = 0;
            _allocatedCount++;
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

            if (items.Length > Capacity - _count) // use this way to avoid overflow
            {
                ThrowHelpers.ThrowArgumentException("This collection doesn't have enough space to accomodate elements of the specified span.", nameof(items));
            }

            if (_allocatedCount == 0)
            {
                _segments[0] = new T[1];
                _allocatedCount = 1;
            }

            int newCount = _count + items.Length;
            int currentCapacity = (int)((1U << _allocatedCount) - 1);
            int startIndex = _count - (currentCapacity - (1 << (_allocatedCount - 1))); // the first elements are copied to `_containers[_allocatedCount - 1][startIndex]`.

            if (newCount < currentCapacity) // if this condition is true, `currentCapacity` is not 0 and it is ensured that `this._allocatedCount` is positive.
            {
                items.CopyTo(MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_segments[_allocatedCount - 1]), startIndex),
                    items.Length
                ));
            }
            else
            {
                int currentSegmentLength = 1 << (_allocatedCount - 1);
                var src = items;

                int cpyLength = currentSegmentLength - startIndex;
                MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(src), cpyLength)
                    .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_segments[_allocatedCount - 1]), startIndex), cpyLength));
                currentSegmentLength <<= 1;
                _segments[_allocatedCount] = new T[currentSegmentLength];
                _allocatedCount++;

                int remainsCount = src.Length - cpyLength;
                src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(src), cpyLength), remainsCount);

                while (true)
                {
                    cpyLength = currentSegmentLength;

                    if (remainsCount <= cpyLength)
                    {
                        src.CopyTo(_segments[_allocatedCount - 1].AsSpan());
                        break;
                    }
                    else
                    {
                        MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(src), cpyLength)
                            .CopyTo(_segments[_allocatedCount - 1].AsSpan());

                        currentSegmentLength <<= 1;
                        _segments[_allocatedCount] = new T[currentSegmentLength];
                        _allocatedCount++;
                        remainsCount -= cpyLength;
                        src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(src), cpyLength), remainsCount);
                    }
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

            int currentSegmentLength = 1;
            int remainsCount = _count;
            for (int i = 0; ; i++)
            {
                if (remainsCount <= currentSegmentLength)
                {
                    MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(_segments[i]), remainsCount)
                        .CopyTo(destination);
                    break;
                }

                int nextSegmentLength = currentSegmentLength << 1;

                _segments[i].AsSpan().CopyTo(destination);
                destination = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), currentSegmentLength),
                    nextSegmentLength
                );

                remainsCount -= currentSegmentLength;
                currentSegmentLength = nextSegmentLength;
            }
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
    }
}
