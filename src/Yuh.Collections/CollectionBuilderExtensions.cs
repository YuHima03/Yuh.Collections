using System.Buffers;
using System.Runtime.CompilerServices;
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
        private const int DefaultReserveLength = 32;

        /// <summary>
        /// Appends a string to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">The collection builder to add <paramref name="s"/> to.</param>
        /// <param name="s">
        ///     A string to add.
        ///     The value can be null or empty string.
        /// </param>
        public static void AppendLiteral(ref this CollectionBuilder<char> builder, string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return;
            }
            if (s.Length == 1)
            {
                builder.Append(s[0]);
            }
            else
            {
                builder.AppendRange(s.AsSpan());
            }
        }

        /// <summary>
        /// Encodes a string into a UTF-8 string and appends the encoded string to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">A collection builder to add an encoded string to.</param>
        /// <param name="s">
        /// A string to add.
        /// The value can be null or empty string.
        /// </param>
#if NET9_0_OR_GREATER
        [OverloadResolutionPriority(-1)]
#endif
        public static void AppendLiteral(ref this CollectionBuilder<byte> builder, string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return;
            }
            AppendLiteral(ref builder, s.AsSpan());
        }

        /// <summary>
        /// Encodes a sequence of characters into a UTF-8 string and appends the encoded string to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">A collection builder to add an encoded string to.</param>
        /// <param name="s">A sequence of characters to add.</param>
        public static void AppendLiteral(ref this CollectionBuilder<byte> builder, scoped ReadOnlySpan<char> s)
        {
            if (s.IsEmpty)
            {
                return;
            }

            var maxByteLength = (long)s.Length * 3;
            switch (maxByteLength)
            {
                case <= 1024:
                {
                    Span<byte> destination = stackalloc byte[(int)maxByteLength];
                    var bytesWritten = Encoding.UTF8.GetBytes(s, destination);
                    builder.AppendRange(destination[..bytesWritten]);
                    break;
                }
                case <= (1 << 26):
                {
                    byte[] destination = ArrayPool<byte>.Shared.Rent((int)maxByteLength);
                    try
                    {
                        var bytesWritten = Encoding.UTF8.GetBytes(s, destination.AsSpan());
                        builder.AppendRange(destination.AsSpan()[..bytesWritten]);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(destination);
                    }
                    break;
                }
                default:
                {
                    var bytes = new byte[Encoding.UTF8.GetByteCount(s)];
                    var bytesWritten = Encoding.UTF8.GetBytes(s, bytes.AsSpan());
                    builder.AppendRange(bytes.AsSpan()[..bytesWritten]);
                    break;
                }
            }
        }

        /// <summary>
        /// Appends the string expression of the value to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="builder">The collection builder to add a string to.</param>
        /// <param name="value">The value the string expression of which is added to <paramref name="builder"/>.</param>
        /// <param name="estimatedStringLength">The estimated length of a string to add.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for the destination collection.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for the destination collection.</param>
        public static void AppendFormatted<T>(ref this CollectionBuilder<char> builder, T value, int estimatedStringLength = DefaultReserveLength, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            if (value is ISpanFormattable valueSpanFormattable)
            {
                int charsWritten;
                int destLength = (estimatedStringLength <= 0) ? 1 : estimatedStringLength;

                if (destLength <= 512)
                {
                    int totalAllocatedCharLength = 0;
                    do
                    {
#pragma warning disable CA2014
                        Span<char> destination = stackalloc char[destLength];
#pragma warning restore CA2014
                        if (valueSpanFormattable.TryFormat(destination, out charsWritten, format, provider))
                        {
                            builder.AppendRange(destination[..charsWritten]);
                            return;
                        }
                        totalAllocatedCharLength += destLength;
                        destLength = checked(destLength << 1);
                    }
                    while (destLength <= 512 - totalAllocatedCharLength);
                }

                var reserved = builder.ReserveRange(destLength);
                while (!valueSpanFormattable.TryFormat(reserved, out charsWritten, format, provider))
                {
                    builder.RemoveRange(destLength);
                    destLength = checked(destLength << 1);
                    reserved = builder.ReserveRange(destLength);
                }
                builder.RemoveRange(destLength - charsWritten);
                return;
            }
            else if (value is IFormattable valueFormattable)
            {
                builder.AppendLiteral(valueFormattable.ToString(format.ToString(), provider));
                return;
            }

            builder.AppendLiteral(value?.ToString());
            return;
        }

        /// <summary>
        /// Appends UTF-8 string expression of a value to the back of the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="builder">A collection builder to add a string to.</param>
        /// <param name="value">A value to write.</param>
        /// <param name="estimatedStringLength">An estimated length of a string to add.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for the destination collection.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for the destination collection.</param>
        public static void AppendUtf8Formatted<T>(ref this CollectionBuilder<byte> builder, T value, int estimatedStringLength = DefaultReserveLength, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
#if NET9_0_OR_GREATER
            if (value is IUtf8SpanFormattable u8SpanFormattable)
            {
                int charsWritten;
                int destLength = Math.Max(estimatedStringLength, 1);

                if (destLength <= 1024)
                {
                    int totalAllocatedByteLength = 0;
                    do
                    {
#pragma warning disable CA2014
                        Span<byte> destination = stackalloc byte[destLength];
#pragma warning restore CA2014
                        if (u8SpanFormattable.TryFormat(destination, out charsWritten, format, provider))
                        {
                            builder.AppendRange(destination[..charsWritten]);
                            return;
                        }
                        totalAllocatedByteLength += destLength;
                        destLength <<= 1;
                    }
                    while (destLength <= 512 - totalAllocatedByteLength);
                }

                var reserved = builder.ReserveRange(destLength);
                while (!u8SpanFormattable.TryFormat(reserved, out charsWritten, format, provider))
                {
                    builder.RemoveRange(destLength);
                    destLength = checked(destLength << 1);
                    reserved = builder.ReserveRange(destLength);
                }
                builder.RemoveRange(destLength - charsWritten);
                return;
            }
#endif
            if (value is IFormattable formattable)
            {
                if (formattable is ISpanFormattable spanFormattable)
                {
                    int destLength = Math.Max(estimatedStringLength, 1);

                    if (destLength <= 512)
                    {
                        int totalAllocatedByteLength = 0;
                        do
                        {
#pragma warning disable CA2014
                            Span<char> destination = stackalloc char[destLength];
#pragma warning restore CA2014
                            if (spanFormattable.TryFormat(destination, out var charsWritten, format, provider))
                            {
                                builder.AppendLiteral(destination[..charsWritten]);
                                return;
                            }
                            totalAllocatedByteLength += destLength;
                            destLength <<= 1;
                        }
                        while (destLength <= 512 - totalAllocatedByteLength);
                    }
                }
                builder.AppendLiteral(formattable.ToString(format.ToString(), provider));
                return;
            }

            builder.AppendLiteral(value?.ToString());
            return;
        }

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

            builder.CopyTo(values.AsSpan()[head..]);
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
            if (Unsafe.SizeOf<T>() * length <= (1 << 26))
            {
                T[] values = ArrayPool<T>.Shared.Rent(capacity);
                builder.CopyTo(values.AsSpan());
                list.AddRange(new ArraySegment<T>(values, 0, length));
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
        /// Creates a <see cref="string"/> from a collection builder that represents a UTF-8 encoded string.
        /// </summary>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> that represents a UTF-8 encoded string to create a <see cref="string"/> from.</param>
        /// <returns>A <see cref="string"/> converted from the UTF-8 encoded string that the collection builder represents.</returns>
        public static string ToSystemString(in this CollectionBuilder<byte> builder)
        {
            int length = builder.Count;
            switch (length)
            {
                case <= 1024:
                {
                    Span<byte> bytes = stackalloc byte[length];
                    builder.CopyTo(bytes);
                    return Encoding.UTF8.GetString(bytes);
                }
                case <= (1 << 26):
                {
                    var bytes = ArrayPool<byte>.Shared.Rent(length);
                    try
                    {
                        builder.CopyTo(bytes.AsSpan());
                        return Encoding.UTF8.GetString(bytes[..builder.Count]);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(bytes);
                    }
                }
                default:
                {
                    var decoder = Encoding.UTF8.GetDecoder();

                    using CollectionBuilderConstants.Segments<(char[], int)> _decoded = new();
                    Span<(char[], int)> decoded = _decoded.AsSpan();
                    int decodedSegmentCount = 0;
                    int strLen = 0;

                    using var en = builder.GetSegmentEnumerator();
                    while (en.MoveNext())
                    {
                        int estCnt = decoder.GetCharCount(en.CurrentSpan, false);
                        char[] chars = ArrayPool<char>.Shared.Rent(estCnt);
                        int charsWritten = decoder.GetChars(en.CurrentSpan, chars.AsSpan(), false);

                        strLen = checked(strLen + charsWritten);
                        decoded[decodedSegmentCount] = (chars, charsWritten);
                        decodedSegmentCount++;
                    }
                    decoder.Reset();

#if NET9_0_OR_GREATER
                    return string.Create<ReadOnlySpan<(char[], int)>>(
                        strLen,
                        decoded[..decodedSegmentCount],
                        static (dest, src) => {
                            int copiedCnt = 0;

                            for (int i = 0; i < src.Length; i++)
                            {
                                var (chars, len) = src[i];
                                chars.AsSpan()[..len].CopyTo(dest.Slice(copiedCnt, len));

                                copiedCnt += len;
                                ArrayPool<char>.Shared.Return(chars);
                            }
                        }
                    );
#else
                    return string.Create(
                        strLen,
                        (_decoded, decodedSegmentCount),
                        static (dest, stat) => {
                            ReadOnlySpan<(char[], int)> src = stat._decoded.AsSpan()[..stat.decodedSegmentCount];
                            int copiedCnt = 0;

                            for (int i = 0; i < src.Length; i++)
                            {
                                var (chars, len) = src[i];
                                chars.AsSpan()[..len].CopyTo(dest.Slice(copiedCnt, len));

                                copiedCnt += len;
                                ArrayPool<char>.Shared.Return(chars);
                            }
                        }
                    );
#endif
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="string"/> from the <see cref="CollectionBuilder{T}"/>.
        /// </summary>
        /// <param name="builder">A <see cref="CollectionBuilder{T}"/> whose elements (characters) are copied to the new <see cref="string"/>.</param>
        /// <returns>A <see cref="string"/> which contains characters are copied from the <see cref="CollectionBuilder{T}"/>.</returns>
        public static string ToSystemString(in this CollectionBuilder<char> builder)
        {
            if (builder.Count == 0)
            {
                return string.Empty;
            }

            using var en = builder.GetSegmentEnumerator();
#if NET9_0_OR_GREATER
            return string.Create(
                builder.Count,
                en,
                static (dest, enumerator) => {
                    int copiedCnt = 0;
                    while (enumerator.MoveNext())
            {
                        var src = enumerator.CurrentSpan;
                        src.CopyTo(dest[copiedCnt..]);
                        copiedCnt += src.Length;
                    }
                }
            );
#else
            using CollectionBuilderConstants.Segments<ReadOnlyMemory<char>> _segments = new();
            Span<ReadOnlyMemory<char>> segments = _segments.AsSpan();
            int segmentCount = 0;

            while (en.MoveNext())
            {
                segments[segmentCount] = en.CurrentMemory;
                segmentCount++;
            }

            return string.Create(
                builder.Count,
                _segments,
                static (dest, stat) => {
                    int copiedCnt = 0;
                    foreach (var seg in stat.AsSpan())
                    {
                        seg.Span.CopyTo(dest[copiedCnt..]);
                        copiedCnt += seg.Length;
                    }
                }
            );
#endif
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
            if (length <= 512)
            {
                Span<char> chars = stackalloc char[length];
                builder.CopyTo(chars);
                return sb.Append(chars);
            }
            else if (length <= (1 << 25))
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
