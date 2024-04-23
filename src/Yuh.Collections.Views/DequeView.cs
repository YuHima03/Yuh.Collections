using System.Runtime.CompilerServices;

namespace Yuh.Collections.Views
{
    public class DoubleEndedListView<T>
    {
        public T[] Items = [];
        public int Count;
        public int Head;
        public int Version;

        public DoubleEndedList<T> Encapsulate()
        {
            return Unsafe.As<DoubleEndedList<T>>(this);
        }
    }
}
