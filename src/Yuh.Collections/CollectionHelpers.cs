using System.Runtime.CompilerServices;

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
        internal static void ClearIfReferenceOrContainsReferences<T>(Span<T> span1,  Span<T> span2)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                span1.Clear();
                span2.Clear();
            }
        }
    }
}
