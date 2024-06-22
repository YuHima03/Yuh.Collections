using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Yuh.Collections
{
    /// <summary>
    /// Provides extension methods for <see cref="CollectionBuilder{T}"/> structure.
    /// </summary>
    public static class CollectionBuilderExtensions
    {
        /// <summary>
        /// Creates a new instance of the <see cref="Deque{T}"/> class that contains elements copied from the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements of the collection.</typeparam>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements are copied to the new <see cref="Deque{T}"/>.</param>
        /// <returns>A new <see cref="Deque{T}"/> whose elements are copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public static Deque<T> ToDeque<T>(in this CollectionBuilder<T> builder)
        {
            T[] values = new T[builder.GetAllocatedCapacity()];
            builder.CopyTo(values.AsSpan());
            return new(values, 0, builder.Count);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DoubleEndedList{T}"/> class that contains elements copied from the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements of the collection.</typeparam>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements are copied to the new <see cref="DoubleEndedList{T}"/>.</param>
        /// <returns>A new <see cref="DoubleEndedList{T}"/> whose elements are copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public static DoubleEndedList<T> ToDoubleEndedList<T>(in this CollectionBuilder<T> builder)
        {
            int capacity = builder.GetAllocatedCapacity();
            int length = builder.Count;
            int head = (capacity - length) >> 1;
            T[] values = new T[capacity];

            builder.CopyTo(
                MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(values), head)
            );
            return new(values, head, length);
        }

        /// <summary>
        /// Creates a new <see cref="string"/> from the <see cref="CollectionBuilder{T}"/> the type of which is <see cref="char"/>.
        /// </summary>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements (characters) are copied to the new <see cref="string"/>.</param>
        /// <returns>A new <see cref="string"/> whose characters are copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public static string ToBasicString(in this CollectionBuilder<char> builder)
        {
            int length = builder.Count;
            if (length <= 1024)
            {
                Span<char> chars = stackalloc char[length];
                builder.CopyTo(chars);
                return new(chars);
            }
            else
            {
                var charsArray = ArrayPool<char>.Shared.Rent(length);
                var chars = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(charsArray), length);
                builder.CopyTo(chars);
                string s = new(chars);

                ArrayPool<char>.Shared.Return(charsArray);
                return s;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="StringBuilder"/> class that contains characters copied from the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements (characters) are copied to the new <see cref="StringBuilder"/>.</param>
        /// <returns>A new <see cref="StringBuilder"/> whose characters are copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public static StringBuilder ToStringBuilder(in this CollectionBuilder<char> builder)
        {
            int length = builder.Count;
            StringBuilder sb = new(length);
            if (length <= 1024)
            {
                Span<char> chars = stackalloc char[length];
                builder.CopyTo(chars);
                return sb.Append(chars);
            }
            else
            {
                var charsArray = ArrayPool<char>.Shared.Rent(length);
                var chars = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(charsArray), length);
                builder.CopyTo(chars);
                sb.Append(chars);

                ArrayPool<char>.Shared.Return(charsArray);
                return sb;
            }
        }
    }
}
