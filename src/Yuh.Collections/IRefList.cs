using System.Diagnostics.CodeAnalysis;

namespace Yuh.Collections
{
    public interface IRefList<T>
    {
        public int Count { get; }

        public Span<T> this[int index] { get; }
    }
}
