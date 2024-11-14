using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    public static class SpanExtensions
    {
        public static ref T First<T>(ref this Span<T> span)
        {
            if (span.IsEmpty)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionIsEmpty, nameof(span));
            }
            return ref MemoryMarshal.GetReference(span);
        }

        public static ref readonly T First<T>(ref this ReadOnlySpan<T> span)
        {
            if (span.IsEmpty)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionIsEmpty, nameof(span));
            }
            return ref MemoryMarshal.GetReference(span);
        }

        public static ref T Last<T>(ref this Span<T> span)
        {
            if (span.IsEmpty)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionIsEmpty, nameof(span));
            }
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), span.Length - 1);
        }

        public static ref readonly T Last<T>(ref this ReadOnlySpan<T> span)
        {
            if (span.IsEmpty)
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_CollectionIsEmpty, nameof(span));
            }
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), span.Length - 1);
        }
    }
}
