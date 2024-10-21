namespace Yuh.Collections
{
    internal ref struct ChunkedSpan<T, TSpanProvider>
#if NET9_0_OR_GREATER
        where TSpanProvider : struct, IReadOnlyList<Span<T>>
#else
        where TSpanProvider : struct, IReadOnlySpanList<T>
#endif
    {
        private TSpanProvider _spans;
        private Span<int> _lengthSum;
    }
}
