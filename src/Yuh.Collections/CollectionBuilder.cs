using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        private ReadOnlySpan<T[]> _allocatedSegments = [];
        
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
        private unsafe fixed int _segmentLength[CollectionBuilderConstants.MaxSegmentCount];

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
        public readonly int Count => throw new NotImplementedException();

        /// <summary>
        /// Gets the number of elements that can be added without resizing the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        public readonly int RemainingCapacity => throw new NotImplementedException();

        /// <summary>
        /// Initializes a collection builder whose fields are set to default value.
        /// </summary>
        public CollectionBuilder()
        {

        }

        /// <summary>
        /// Initializes a collection builder whose first segment can contain exactly specified number of elements.
        /// </summary>
        /// <param name="firstSegmentLength">The number of elements that can be contained in the first segment.</param>
        public CollectionBuilder(int firstSegmentLength) : this()
        {
            
        }

        /// <summary>
        /// Adds a element to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="item">An object to add.</param>
        /// <exception cref="Exception">The <see cref="CollectionBuilder{T}"/> is already full.</exception>
        public void Append(T item)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds elements in the span to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="items">A span over elements to add.</param>
        /// <exception cref="ArgumentException">The <see cref="CollectionBuilder{T}"/> doesn't have enough space to accommodate elements contained in <paramref name="items"/>.</exception>
        public void AppendRange(scoped ReadOnlySpan<T> items)
        {
            throw new NotImplementedException();
        }

        private static T[] AllocateNewArray(int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies elements in the <see cref="CollectionBuilder{T}"/> to the specified span.
        /// </summary>
        /// <param name="destination"></param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> doesn't have enough space to accommodate elements copied.</exception>
        public readonly void CopyTo(Span<T> destination)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public readonly void Dispose()
        {
            
        }

        /// <summary>
        /// Returns the number of elements that can be contained in the <see cref="CollectionBuilder{T}"/> without allocate new internal array.
        /// </summary>
        /// <returns>The number of elements that can be contained in the <see cref="CollectionBuilder{T}"/> without allocating new internal array.</returns>
        public readonly int GetAllocatedCapacity()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Allocates new buffer than can accommodate at least <see cref="_nextSegmentLength"/> elements.
        /// </summary>
        private void Grow()
            => throw new NotImplementedException();

        /// <summary>
        /// Allocates new buffer than can accommodate at least specified number of elements.
        /// </summary>
        /// <remarks>
        /// If <paramref name="neededLength"/> is greater than <see cref="_nextSegmentLength"/>, this method allocate new buffer that can accommodate <paramref name="neededLength"/> elements; otherwise, <see cref="_nextSegmentLength"/> elements.
        /// </remarks>
        /// <param name="neededLength"></param>
        private void Grow(int neededLength)
            => throw new NotImplementedException();

        /// <summary>
        /// Allocates new buffer that can accommodate at least specified number of elements.
        /// </summary>
        /// <param name="length"></param>
        private void GrowExact(int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes specified number of elements from the end of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="length">The number of elements to remove from the <see cref="CollectionBuilder{T}"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative or greater than <see cref="Count"/>.</exception>
        public void RemoveRange(int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reserves the specified length of memory region from the back of the <see cref="CollectionBuilder{T}"/> and returns the span over the region.
        /// </summary>
        /// <param name="length">The number of elements that the reserved memory region can exactly accommodate.</param>
        /// <returns>The span over the reserved memory region.</returns>
        public Span<T> ReserveRange(int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an array from the <see cref="CollectionBuilder{T}"/> and returns it.
        /// </summary>
        /// <returns>An array which contains elements copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public readonly T[] ToArray()
        {
            throw new NotImplementedException();
        }
    }
}
