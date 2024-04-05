using System.Diagnostics;

namespace Yuh.Collections.Debugging
{
    [DebuggerNonUserCode]
    internal class CollectionDebugView<T>
    {
        private readonly ICollection<T> _collection;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _collection.ToArray();

        public CollectionDebugView(ICollection<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);
            _collection = collection;
        }
    }
}
