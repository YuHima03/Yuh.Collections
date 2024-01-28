using System.Collections;
using System.Runtime.CompilerServices;

namespace Yuh.Collections
{
    /// <summary>
    /// Represents a double-ended queue for which elements can be added to or removed from the front or back.
    /// </summary>
    /// <remarks>
    ///     This provides most of the functions <see cref="Deque{T}"/> has, and may require smaller memory-region than <see cref="Deque{T}"/>.
    ///     However, this performs slightly worse than <see cref="Deque{T}"/> in some respects.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the <see cref="DequeSlim{T}"/>.</typeparam>
    public class DequeSlim<T> : ICollection, ICollection<T>, IEnumerable, IEnumerable<T>, IList, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        private const int _defaultCapacity = 8;
#pragma warning disable IDE0301
        private static readonly T[] _s_emptyArray = Array.Empty<T>();
#pragma warning restore IDE0301

        private T[] _items;
        private int _capacity;
        private int _count;
        private int _head;
        private int _version;

        bool IList.IsFixedSize => false;
        bool ICollection<T>.IsReadOnly => false;
        bool IList.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

#pragma warning disable IDE1006
        private ref T _firstRef => ref _items[_head];
        private ref T _lastRef => ref _items[(_head + _count - 1) % _capacity];
#pragma warning restore IDE1006

        /// <summary>
        /// Gets of sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <value>The element at the specified index.</value>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="DequeSlim{T}"/>.</exception>
        public T this[int index]
        {
            get
            {
                if ((uint)index < (uint)_count)
                {
                    return _items[(_head + index) % _capacity];
                }
                else
                {
                    throw new IndexOutOfRangeException(ThrowHelpers.M_IndexOutOfRange);
                }
            }

            set
            {
                if ((uint)index < (uint)_count)
                {
                    _items[(_head + index) % _capacity] = value;
                    _version++;
                }
                else
                {
                    throw new IndexOutOfRangeException(ThrowHelpers.M_IndexOutOfRange);
                }
            }
        }

        object? IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                if (value is T tValue)
                {
                    this[index] = tValue;
                }
                else
                {
                    throw new ArgumentException(ThrowHelpers.M_TypeOfValueNotSupported, nameof(value));
                }
            }
        }

        /// <summary>
        /// Gets the number of the elements contained in the <see cref="DequeSlim{T}"/>.
        /// </summary>
        /// <returns>The number of the elements contained in the <see cref="DequeSlim{T}"/>.</returns>
        public int Count => _count;

        /// <summary>
        /// Gets the number of total elements the <see cref="DequeSlim{T}"/> can hold without resizing.
        /// </summary>
        /// <remarks>
        /// To sets the capacity, please use <see cref="Resize(int)"/>.
        /// </remarks>
        /// <returns>The number of elements that the <see cref="DequeSlim{T}"/> can contain before resizing is required.</returns>
        public int Capacity
        {
            get
            {
                return _items.Length;
            }
        }

        internal T First => _items[_head];

        internal T Last => _items[(_head + _count - 1) % _capacity];

        /// <summary>
        /// Initializes an new instance of the <see cref="DequeSlim{T}"/> class.
        /// </summary>
        public DequeSlim() : this(_defaultCapacity) { }

        /// <summary>
        /// Initializes an new instance of the <see cref="DequeSlim{T}"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new <see cref="DequeSlim{T}"/> can initially store.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> must be positive or zero, and less than or equal to the maximum length of an array.</exception>
        public DequeSlim(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            ThrowHelpers.ThrowIfArgumentIsGreaterThanMaxArrayLength(capacity);

            _capacity = capacity;
            _items = new T[capacity];
            _head = capacity >> 1;
        }

        void ICollection<T>.Add(T item)
        {
            PushBack(item);
        }

        int IList.Add(object? value)
        {
            if (value is T tValue)
            {
                PushBack(tValue);
                return _count - 1;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Removes all of the elements from the <see cref="DequeSlim{T}"/>.
        /// </summary>
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                if (_head + _count > _capacity)
                {
                    Array.Clear(_items, _head, _capacity - _head);
                    Array.Clear(_items, 0, _head + _count - _capacity);
                }
                else
                {
                    Array.Clear(_items, _head, _count);
                }
            }
            _count = 0;
            _head = _capacity >> 1;
            _version++;
        }

        /// <summary>
        /// Determines whether the <see cref="DequeSlim{T}"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="DequeSlim{T}"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> is found in the <see cref="DequeSlim{T}"/>; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T item)
        {
            if (_head + _count > _capacity)
            {
                return (Array.IndexOf(_items, item, _head, _capacity - _head) != -1) || (Array.IndexOf(_items, item, 0, _head + _count - _capacity) != -1);
            }
            else
            {
                return Array.IndexOf(_items, item, _head, _count) != -1;
            }
        }

        bool IList.Contains(object? value)
        {
            return (value is T tValue) && Contains(tValue);
        }

        /// <summary>
        /// Copies the elements of the <see cref="DequeSlim{T}"/> to an <typeparamref name="T"/>[], starting at a particular index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="DequeSlim{T}"/>.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The number of the elements in the source <see cref="DequeSlim{T}"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array.Length - arrayIndex < _count)
            {
                throw new ArgumentException("The number of the elements in the source collection is greater than the available space from the specified index to the end of the destination array.", nameof(arrayIndex));
            }
            CopyToInternal(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array.Length - index < _count)
            {
                throw new ArgumentException("The number of the elements in the source collection is greater than the available space from the specified index to the end of the destination array.", nameof(index));
            }

            if (_head + _count > _capacity)
            {
                Array.Copy(_items, _head, array, index, _capacity - _head);
                Array.Copy(_items, 0, array, index + _capacity - _head, _head + _count - _capacity);
            }
            else
            {
                Array.Copy(_items, _head, array, index, _count);
            }
        }

        private void CopyToInternal(T[] array, int arrayIndex)
        {
            if (_head + _count > _capacity)
            {
                Array.Copy(_items, _head, array, arrayIndex, _capacity - _head);
                Array.Copy(_items, 0, array, arrayIndex + _capacity - _head, _head + _count - _capacity);
            }
            else
            {
                Array.Copy(_items, _head, array, arrayIndex, _count);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="DequeSlim{T}"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="DequeSlim{T}"/>.</returns>
        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Searches for the specified item and returns the zero-based index of the first occurrence within the entire <see cref="DequeSlim{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="DequeSlim{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        /// <returns>The zero-based index of the first occurrence of <paramref name="item"/>, if found; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            if (_head + _count > _capacity)
            {
                int idx = Array.IndexOf(_items, item, _head, _capacity - _head);

                if (idx >= 0)
                {
                    return idx - _head;
                }
                else
                {
                    idx = Array.IndexOf(_items, item, 0, _head + _count - _capacity);
                    return (idx >= 0) ? (_capacity - _head + idx) : -1;
                }
            }
            else
            {
                int idx = Array.IndexOf(_items, item, _head, _count);
                return (idx >= 0) ? (idx - _head) : -1;
            }
        }

        int IList.IndexOf(object? value)
        {
            return (value is T tValue) ? IndexOf(tValue) : -1;
        }

        /// <summary>
        /// Insert an item to the <see cref="DequeSlim{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">An object to insert. The value can be <see langword="null"/> for reference types.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="DequeSlim{T}"/>.</exception>
        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Searches for the specified item and returns the zero-based index of the last occurrence within the entire <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="DequeSlim{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        /// <returns>The zero-based index of the last occurrence of <paramref name="item"/>, if found; otherwise, <c>-1</c>.</returns>
        public int LastIndexOf(T item)
        {
            if (_head + _count > _capacity)
            {
                int idx = Array.LastIndexOf(_items, item, 0, _head + _count - _capacity);

                if (idx >= 0)
                {
                    return idx - _head;
                }
                else
                {
                    idx = Array.LastIndexOf(_items, item, _head, _capacity - _head);
                    return (idx >= 0) ? (_capacity - _head + idx) : -1;
                }
            }
            else
            {
                int idx = Array.LastIndexOf(_items, item, _head, _count);
                return (idx >= 0) ? (idx - _head) : -1;
            }
        }
        
        void IList.Insert(int index, object? value)
        {
            if (value is T tValue)
            {
                Insert(index, tValue);
            }
        }

        /// <summary>
        /// Returns the object at the beginning of the <see cref="DequeSlim{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="DequeSlim{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="DequeSlim{T}"/> is empty.</exception>
        public T PeekFirst()
        {
            ThrowIfCollectionIsEmpty();
            return First;
        }

        /// <summary>
        /// Returns the object at the end of the <see cref="DequeSlim{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the end of the <see cref="DequeSlim{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="DequeSlim{T}"/> is empty.</exception>
        public T PeekLast()
        {
            ThrowIfCollectionIsEmpty();
            return Last;
        }

        /// <summary>
        /// Removes and returns the object at the end of the <see cref="DequeSlim{T}"/>.
        /// </summary>
        /// <returns>The object at the end of the <see cref="DequeSlim{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="DequeSlim{T}"/> is empty.</exception>
        public T PopBack()
        {
            ThrowIfCollectionIsEmpty();
            return PopBackInternal();
        }

        private T PopBackInternal()
        {
            var res = Last;
            CollectionHelpers.SetDefaultValueIfReferenceOrContainsReferences(ref _lastRef);

            _count--;
            _version++;

            return res;
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="DequeSlim{T}"/>.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="DequeSlim{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="DequeSlim{T}"/> is empty.</exception>
        public T PopFront()
        {
            ThrowIfCollectionIsEmpty();
            return PopFrontInternal();
        }

        private T PopFrontInternal()
        {
            var res = First;
            CollectionHelpers.SetDefaultValueIfReferenceOrContainsReferences(ref _firstRef);

            _head = (_head + 1) % _capacity;
            _count--;
            _version++;

            return res;
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="DequeSlim{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the end of the <see cref="DequeSlim{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushBack(T item)
        {
            if (_count == _capacity)
            {
                Grow();
            }

            _items[(_head + _count) % _capacity] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Adds an object to the beginning of the <see cref="DequeSlim{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the beginning of the <see cref="DequeSlim{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushFront(T item)
        {
            if (_count == _capacity)
            {
                Grow();
            }

            _head = (_head - 1 + _capacity) % _capacity;
            _items[_head] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Removes the first occurrence of the specified object from the <see cref="DequeSlim{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="DequeSlim{T}"/>.</param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        void IList.Remove(object? value)
        {
            if (value is T tValue)
            {
                Remove(tValue);
            }
        }

        /// <summary>
        /// Removes the element at the specified index in the <see cref="DequeSlim{T}"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index of the <see cref="DequeSlim{T}"/>.</exception>
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Resizes the internal array to the specified size.
        /// </summary>
        /// <param name="capacity"></param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than the number of elements contained in the <see cref="DequeSlim{T}"/> or greater than <see cref="Array.MaxLength"/>.</exception>
        public void Resize(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            ThrowHelpers.ThrowIfArgumentIsGreaterThanMaxArrayLength(capacity);
            if (capacity < _count)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity is less than the number of the elements contained in the collection.");
            }

            ResizeInternal(capacity);
        }

        private void ResizeInternal(int capacity)
        {
            if (capacity == 0)
            {
                _items = _s_emptyArray;
                _capacity = 0;
                _head = 0;
            }
            else
            {
                int head = (capacity - _count) >> 1;
                T[] items = new T[capacity];

                CopyToInternal(items, head);
                _capacity = capacity;
                _head = head;
                _items = items;
            }
            _version++;
        }

        /// <summary>
        /// Enlarge the internal array to twice its size.
        /// </summary>
        /// <exception cref="Exception">The number of elements contained in the <see cref="DequeSlim{T}"/> has reached its upper limit.</exception>
        private void Grow()
        {
            int capacity = int.Min(
                int.Max(_capacity << 1, _defaultCapacity),
                Array.MaxLength
                );

            if (capacity < _count + 2)
            {
                throw new Exception(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            ResizeInternal(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfCollectionIsEmpty()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException(ThrowHelpers.M_CollectionIsEmpty);
            }
        }

        /// <summary>
        /// Enumerates the elements of a <see cref="DequeSlim{T}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator, IEnumerator<T>
        {
            private readonly DequeSlim<T> _deque;
            private int _index = -1;
            private readonly int _version;

            /// <summary>Gets the element in the <see cref="DequeSlim{T}"/> at the current position of the enumerator.</summary>
            /// <returns>The element in the <see cref="DequeSlim{T}"/> at the current position of the enumerator.</returns>
            public readonly T Current => _deque._items[(_deque._head + _index) % _deque._capacity];

            readonly object? IEnumerator.Current => Current;

            internal Enumerator(DequeSlim<T> deque)
            {
                _deque = deque;
                _version = deque._version;
            }

            /// <inheritdoc/>
            public readonly void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="DequeSlim{T}"/>.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the <see cref="DequeSlim{T}"/>.</returns>
            /// <exception cref="InvalidOperationException">The <see cref="DequeSlim{T}"/> was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                ThrowIfCollectionModified();
                return ++_index < _deque._count;
            }

            /// <summary>Set the enumerator to its initial position, which is the before the first element in the <see cref="DequeSlim{T}"/>.</summary>
            /// <exception cref="InvalidOperationException">The <see cref="DequeSlim{T}"/> was modified after the enumerator was created.</exception>
            public void Reset()
            {
                ThrowIfCollectionModified();
                _index = -1;
            }

            /// <summary>
            /// Throw an exception if the <see cref="DequeSlim{T}"/> was modified after the enumerator was created.
            /// </summary>
            /// <exception cref="InvalidOperationException">The <see cref="DequeSlim{T}"/> was modified after the enumerator was created.</exception>
            private readonly void ThrowIfCollectionModified()
            {
                if (_version != _deque._version)
                {
                    throw new InvalidOperationException(ThrowHelpers.M_CollectionModifiedAfterEnumeratorCreated);
                }
            }
        }
    }
}
