using System.Buffers;
using System.Runtime.CompilerServices;

namespace Yuh.Collections
{
    /// <summary>
    /// Provides extension methods for <see cref="NonRefCollectionBuilder{T}"/> structure.
    /// </summary>
    public static class NonRefCollectionBuilderExtensions
    {
        private const int MaxStackAllocLength = 1024;
        private const int MaxArrayLengthFromArrayPool = 1024 * 1024;
        private const int MinInitialReserveLength = 16;

        /// <summary>
        /// Adds <see cref="char"/>s in a string that represents <typeparamref name="T"/> value to the back of the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">The collection builder to add chars to.</param>
        /// <param name="value"></param>
        /// <param name="estimatedStringLength"></param>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns>A <see cref="string"/> which contains characters are copied from the <see cref="NonRefCollectionBuilder{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddFormatted<T>(ref this NonRefCollectionBuilder<char> builder, T value, int estimatedStringLength = MinInitialReserveLength, ReadOnlySpan<char> format = default, IFormatProvider? formatProvider = null)
            where T : IFormattable, ISpanFormattable
        {
            int charsWritten;
            int reserveLength = Math.Max(estimatedStringLength, MinInitialReserveLength);

            while (!value.TryFormat(builder.ReserveRange(reserveLength), out charsWritten, format, formatProvider))
            {
                builder.RemoveRange(reserveLength);
                reserveLength <<= 1;
            }

            builder.RemoveRange(reserveLength - charsWritten);
        }

        /// <summary>
        /// Adds <see cref="char"/>s in a string to the back of the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">The collection builder to add chars to.</param>
        /// <param name="s">A string to add to <paramref name="builder"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is null.</exception>
        public static void AddString(in this NonRefCollectionBuilder<char> builder, string s)
        {
            ArgumentNullException.ThrowIfNull(s);
            builder.AddRange(s.AsSpan());
        }

        /// <summary>
        /// Creates a <see cref="string"/> from the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">A <see cref="NonRefCollectionBuilder{T}"/> whose elements (characters) are copied to the new <see cref="string"/>.</param>
        /// <returns>A <see cref="string"/> which contains characters are copied from the <see cref="NonRefCollectionBuilder{T}"/>.</returns>
        public static string ToBasicString(in this NonRefCollectionBuilder<char> builder)
        {
            int length = builder.Count;
            switch (length)
            {
                case <= MaxStackAllocLength:
                {
                    Span<char> chars = stackalloc char[length];
                    builder.CopyTo(chars);
                    return chars.ToString();
                }
                case <= MaxArrayLengthFromArrayPool:
                {
                    var charsArray = ArrayPool<char>.Shared.Rent(length);
                    var chars = charsArray.AsSpan()[..length];

                    builder.CopyTo(chars);
                    var s = chars.ToString();

                    ArrayPool<char>.Shared.Return(charsArray);
                    return s;
                }
                default:
                {
                    var charsArray = GC.AllocateUninitializedArray<char>(length);
                    var chars = charsArray.AsSpan();
                    builder.CopyTo(chars);
                    return chars.ToString();
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> from the <see cref="NonRefCollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">A <see cref="NonRefCollectionBuilder{T}"/> whose elements are copied to the new <see cref="List{T}"/>.</param>
        /// <returns>A <see cref="List{T}"/> which contains elements copied from the <see cref="NonRefCollectionBuilder{T}"/>.</returns>
        public static List<T> ToList<T>(in this NonRefCollectionBuilder<T> builder)
        {
            int length = builder.Count;
            List<T> list = new(builder.GetAllocatedCapacity());

#if NET8_0_OR_GREATER
            System.Runtime.InteropServices.CollectionsMarshal.SetCount(list, length);
            builder.CopyTo(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list));
#else
            if (length <= MaxArrayLengthFromArrayPool)
            {
                var array = ArrayPool<T>.Shared.Rent(length);
                builder.CopyTo(array);
                list.AddRange(array);
                ArrayPool<T>.Shared.Return(array);
            }
            else
            {
                list.AddRange(builder.ToArray());
            }
#endif
            return list;
        }
    }
}
