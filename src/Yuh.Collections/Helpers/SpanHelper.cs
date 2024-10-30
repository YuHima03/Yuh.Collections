using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections.Helpers
{
    internal static class SpanHelper
    {
        public static ref readonly T First<T>(ref this ReadOnlySpan<T> span)
        {
            if (span.IsEmpty)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionIsEmpty, nameof(span));
            }
            return ref MemoryMarshal.GetReference(span);
        }

        public static ref T First<T>(ref this Span<T> span)
        {
            if (span.IsEmpty)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionIsEmpty, nameof(span));
            }
            return ref MemoryMarshal.GetReference(span);
        }

        public static ref readonly T Last<T>(ref this ReadOnlySpan<T> span)
        {
            if (span.IsEmpty)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionIsEmpty, nameof(span));
            }
            return ref span.UnsafeAccess(span.Length - 1);
        }

        public static ref T Last<T>(ref this Span<T> span)
        {
            if (span.IsEmpty)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionIsEmpty, nameof(span));
            }
            return ref span.UnsafeAccess(span.Length - 1);
        }

        public static ref readonly T UnsafeAccess<T>(ref this ReadOnlySpan<T> span, int index)
        {
            Debug.Assert((uint)index < span.Length, ThrowHelpers.M_IndexOutOfRange);
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
        }

        public static ref T UnsafeAccess<T>(ref this Span<T> span, int index)
        {
            Debug.Assert((uint)index < span.Length, ThrowHelpers.M_IndexOutOfRange);
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
        }
    }
}
