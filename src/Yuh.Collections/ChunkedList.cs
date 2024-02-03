using System.Runtime.CompilerServices;

namespace Yuh.Collections
{
    /// <summary>
    /// Represents a list of object that can be accessed by index and provides methods to search and manipulate lists.
    /// </summary>
    /// <remarks>
    /// <para>This holds elements in separated chunks, but this performs almost as well as the regular <see cref="List{T}"/>.</para>
    /// </remarks>
    /// <typeparam name="T">The type of elements in the <see cref="ChunkedList{T}"/>.</typeparam>
    internal class ChunkedList<T>
    {
        private const int DefaultChunkSize = 64;
        private const int DefaultChunksListCapacity = 8;
        private const int MinChunkSize = 16;

        private readonly T[][] _chunks;
        private int _count;
        private readonly int _mask;
        private int _version;

        /// <summary>
        /// Square root of the size of the chunks.
        /// </summary>
        private readonly int _chunkSizeSqRoot;

        /// <summary>
        /// Square root of the length of the <see cref="_chunks"/> array.
        /// </summary>
        private int _chunksArrayLengthSqRoot;

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                {
                    throw new IndexOutOfRangeException(ThrowHelpers.M_IndexOutOfRange);
                }
                var (quot, rem) = DivRemIndex(index);
                return _chunks[quot][rem];
            }

            set
            {
                if ((uint)index >= (uint)_count)
                {
                    throw new IndexOutOfRangeException(ThrowHelpers.M_IndexOutOfRange);
                }
                var (quot, rem) = DivRemIndex(index);
                _chunks[quot][rem] = value;
                _version++;
            }
        }

        /// <summary>
        /// Initializes an new instance of the <see cref="ChunkedList{T}"/> class whose chunks can hold the specified number of elements.
        /// </summary>
        public ChunkedList(int chunkSize = DefaultChunkSize)
        {
            ThrowHelpers.ThrowIfArgumentIsLessThan(chunkSize, MinChunkSize);
            ThrowHelpers.ThrowIfArgumentIsNotPowerOfTwo(chunkSize);

            _chunkSizeSqRoot = (int)Math.Sqrt(chunkSize);
            _chunks = new T[DefaultChunksListCapacity][];
            _mask = chunkSize - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (int Quotient, int Remainder) DivRemIndex(int index)
        {
            return (
                index >> _chunkSizeSqRoot, // (index / _chunkSize)
                index & _mask // (index % _chunkSize)
                );
        }
    }
}
