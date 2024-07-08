using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    internal static class CollectionHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetDefaultValueIfReferenceOrContainsReferences<T>(ref T value)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                value = default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ClearIfReferenceOrContainsReferences<T>(Span<T> span)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                span.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ClearIfReferenceOrContainsReferences<T>(Span<T> span1, Span<T> span2)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                span1.Clear();
                span2.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetReadOnlySpan<T>(IEnumerable<T> items, out ReadOnlySpan<T> span)
        {
            var itemsType = items.GetType();
            var TType = typeof(T);

            if (itemsType == typeof(T[]))
            {
                span = Unsafe.As<T[]>(items).AsSpan();
                return true;
            }
            else if (itemsType == typeof(List<T>))
            {
                span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(Unsafe.As<List<T>>(items));
                return true;
            }
            else if (itemsType == typeof(DoubleEndedList<T>))
            {
                span = Unsafe.As<DoubleEndedList<T>>(items).AsSpan();
                return true;
            }
            else if (TType == typeof(char) && itemsType == typeof(string))
            {
                var TSpan = Unsafe.As<string>(items).AsSpan();
                span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<char, T>(ref MemoryMarshal.GetReference(TSpan)), TSpan.Length);
                return true;
            }

            span = default;
            return false;
        }
    }
}
