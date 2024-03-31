using System.Runtime.CompilerServices;

namespace Yuh.Collections.Views
{
    public class DequeView<T>
    {
        public T[] Items = [];
        public int Count;
        public int Head;
        public int Version;

        public Deque<T> Encapsulate()
        {
            return Unsafe.As<Deque<T>>(this);
        }
    }
}
