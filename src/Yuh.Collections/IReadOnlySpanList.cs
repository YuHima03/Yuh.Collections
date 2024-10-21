namespace Yuh.Collections
{
    internal interface IReadOnlySpanList<T>
    {
        public int Count { get; }

        public Span<T> this[int index] { get; }
    }
}
