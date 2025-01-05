using System.Collections;

namespace Yuh.Collections.Tests.Helpers
{
    internal class DequeSlim<T>(int maxLength) : IDisposable, IReadOnlyCollection<T>
    {
        readonly LessAllocArray<T> _array = new(checked((maxLength << 1) + 1));
        int _head = maxLength;
        int _count = 0;

        public int Count => _count;

        public void Dispose()
        {
            _array?.Dispose();
            GC.SuppressFinalize(this);
        }

        public ReadOnlySpan<T> AsSpan() => _array.Array.AsSpan().Slice(_head, _count);

        public IEnumerator<T> GetEnumerator() => _array.Array.Skip(_head - 1).Take(_count).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T PopBack()
        {
            if (_count == 0)
            {
                throw new Exception("The collection is empty.");
            }
            var item = _array.Array[_head + _count - 1];
            _count--;
            return item;
        }

        public T PopFront()
        {
            if (_count == 0)
            {
                throw new Exception("The collection is empty.");
            }
            var item = _array.Array[_head];
            _head++;
            _count--;
            return item;
        }

        public void PushBack(T item)
        {
            _array.Array[_head + _count] = item;
            _count++;
        }

        public void PushFront(T item)
        {
            _array.Array[_head - 1] = item;
            _head--;
            _count++;
        }
    }
}
