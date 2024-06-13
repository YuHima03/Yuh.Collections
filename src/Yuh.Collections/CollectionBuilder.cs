using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    /// <summary>
    /// Repreents a temporary collection that is used to build new collections.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public ref struct CollectionBuilder<T>
    {

        private readonly Span<T[]> _containers; // { T[1], T[2], T[4], ..., T[2^30] }
        private int _count = 0;
        private int _allocatedCount = 0; // in the range [0, 31]

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
        /// <param name="containers">A span used to keep elements added to this collection.</param>
        public CollectionBuilder(Span<T[]> containers)
        {
            if (containers.Length <= 31)
            {
                _containers = containers;
                Capacity = (int)((1U << containers.Length) - 1);
            }
            else
            {
                _containers = containers[..31];
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

            int currentCapacity = (int)((1U << _allocatedCount) - 1);

            if (_count == currentCapacity)
            {
                var container = new T[currentCapacity + 1];
                container[0] = item;
                _containers[_allocatedCount] = container;
                _allocatedCount++;
            }
            else
            {
                int destIndex = _count - (currentCapacity - (1 << (_allocatedCount - 1)));
                _containers[_allocatedCount - 1][destIndex] = item;
            }

            _count++;
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
                _containers[0] = new T[1];
                _allocatedCount = 1;
            }

            int newCount = _count + items.Length;
            int currentCapacity = (int)((1U << _allocatedCount) - 1);
            int startIndex = _count - (currentCapacity - (1 << (_allocatedCount - 1))); // the first elements are copied to `_containers[_allocatedCount - 1][startIndex]`.

            if (newCount < currentCapacity) // if this condition is true, `currentCapacity` is not 0 and it is ensured that `this._allocatedCount` is positive.
            {
                items.CopyTo(MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_containers[_allocatedCount - 1]), startIndex),
                    items.Length
                ));
            }
            else
            {
                int containerLength = 1 << (_allocatedCount - 1);
                var src = items;

                int cpyLength = containerLength - startIndex;
                MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(src), cpyLength)
                    .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_containers[_allocatedCount - 1]), startIndex), cpyLength));
                containerLength <<= 1;
                _containers[_allocatedCount] = new T[containerLength];
                _allocatedCount++;

                int remainsCount = src.Length - cpyLength;
                src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(src), cpyLength), remainsCount);
                
                while (true)
                {
                    cpyLength = containerLength;

                    if (remainsCount <= cpyLength)
                    {
                        src.CopyTo(_containers[_allocatedCount - 1].AsSpan());
                        break;
                    }
                    else
                    {
                        MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(src), cpyLength)
                            .CopyTo(_containers[_allocatedCount - 1].AsSpan());

                        containerLength <<= 1;
                        _containers[_allocatedCount] = new T[containerLength];
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

            int containerLength = 1;
            int remainsCount = _count;
            for (int i = 0; ; i++)
            {
                if (remainsCount <= containerLength)
                {
                    MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(_containers[i]), remainsCount)
                        .CopyTo(destination);
                    break;
                }

                int nextContainerLength = containerLength << 1;

                _containers[i].AsSpan().CopyTo(destination);
                destination = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), containerLength),
                    nextContainerLength
                );

                remainsCount -= containerLength;
                containerLength = nextContainerLength;
            }
        }
    }
}
