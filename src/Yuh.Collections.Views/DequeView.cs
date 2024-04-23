using System.Runtime.CompilerServices;

namespace Yuh.Collections.Views
{
    public class DequeView<T>
    {
        // Do NOT change the order of the following fields.
        public T[] Items = [];
        public int Capacity;
        public int Count;
        public int Head;
        public int Version;

        public Deque<T> Encapsulate()
        {
            return Unsafe.As<Deque<T>>(this);
        }
    }
}
