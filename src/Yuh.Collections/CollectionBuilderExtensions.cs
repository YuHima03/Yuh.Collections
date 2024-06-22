﻿using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using SysCollectionsMarshal = System.Runtime.InteropServices.CollectionsMarshal;

namespace Yuh.Collections
{
    /// <summary>
    /// Provides extension methods for <see cref="CollectionBuilder{T}"/> structure.
    /// </summary>
    public static class CollectionBuilderExtensions
    {
        /// <summary>
        /// Creates a <see cref="Deque{T}"/> from the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements of the collection.</typeparam>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements are copied to the new <see cref="Deque{T}"/>.</param>
        /// <returns>A <see cref="Deque{T}"/> which contains elements are copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public static Deque<T> ToDeque<T>(in this CollectionBuilder<T> builder)
        {
            T[] values = new T[builder.GetAllocatedCapacity()];
            builder.CopyTo(values.AsSpan());
            return new(values, 0, builder.Count);
        }

        /// <summary>
        /// Creates <see cref="DoubleEndedList{T}"/> from the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements of the collection.</typeparam>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements are copied to the new <see cref="DoubleEndedList{T}"/>.</param>
        /// <returns>A <see cref="DoubleEndedList{T}"/> which contains elements are copied from the <see cref="CollectionBuilder{T}"/>.</returns>
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
        /// Creates a <see cref="List{T}"/> from the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements are copied to the new <see cref="List{T}"/>.</param>
        /// <returns>A <see cref="List{T}"/> which contains elements copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public static List<T> ToList<T>(in this CollectionBuilder<T> builder)
        {
            int capacity = builder.GetAllocatedCapacity();
            int length = builder.Count;

            List<T> list = new(capacity);
#if NET8_0_OR_GREATER
            SysCollectionsMarshal.SetCount(list, length);
            builder.CopyTo(SysCollectionsMarshal.AsSpan(list));
#else
            if (length <= 1024 * 1024)
            {
                T[] values = ArrayPool<T>.Shared.Rent(capacity);
                builder.CopyTo(values.AsSpan());
                list.AddRange(values);
                ArrayPool<T>.Shared.Return(values);
            }
            else
            {
                list.AddRange(builder.ToArray());
            }
#endif
            return list;
        }

        /// <summary>
        /// Creates a <see cref="string"/> from the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements (characters) are copied to the new <see cref="string"/>.</param>
        /// <returns>A <see cref="string"/> which contains characters are copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public static string ToBasicString(in this CollectionBuilder<char> builder)
        {
            int length = builder.Count;
            if (length <= 1024)
            {
                Span<char> chars = stackalloc char[length];
                builder.CopyTo(chars);
                return new(chars);
            }
            else if (length <= 1024 * 1024)
            {
                var charsArray = ArrayPool<char>.Shared.Rent(length);
                var chars = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(charsArray), length);
                builder.CopyTo(chars);
                string s = new(chars);

                ArrayPool<char>.Shared.Return(charsArray);
                return s;
            }
            else
            {
                return new(builder.ToArray());
            }
        }

        /// <summary>
        /// Creates <see cref="StringBuilder"/> from the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements (characters) are copied to the new <see cref="StringBuilder"/>.</param>
        /// <returns>A <see cref="StringBuilder"/> which contains characters are copied from the <see cref="CollectionBuilder{T}"/>.</returns>
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
            else if (length <= 1024 * 1024)
            {
                var charsArray = ArrayPool<char>.Shared.Rent(length);
                var chars = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(charsArray), length);
                builder.CopyTo(chars);
                sb.Append(chars);

                ArrayPool<char>.Shared.Return(charsArray);
                return sb;
            }
            else
            {
                return sb.Append(builder.ToArray());
            }
        }
    }
}
