﻿using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yuh.Collections.Debugging;

namespace Yuh.Collections
{
    /// <summary>
    /// Provides static methods to create a new instance of the <see cref="Deque{T}"/> class.
    /// </summary>
    public static class Deque
    {
        /// <summary>
        /// Creates a new instance of the <see cref="Deque{T}"/> class that contains elements copied from the specified <see cref="Deque{T}"/> and returns it.
        /// </summary>
        /// <typeparam name="T">The type of elements in the <see cref="Deque{T}"/>.</typeparam>
        /// <param name="deque">The deque whose elements are copied to the new one.</param>
        /// <returns>A new instance of the <see cref="Deque{T}"/> class that contains elements copied from <paramref name="deque"/> and has sufficient capacity to accommodate the number of elements copied.</returns>
        public static Deque<T> CreateClone<T>(Deque<T> deque)
        {
            return new(deque);
        }
    }

    /// <summary>
    /// Represents a double-ended queue for which elements can be added to or removed from the front or back.
    /// </summary>
    /// <remarks>
    ///     This provides most of the functions <see cref="DoubleEndedList{T}"/> has, and may require smaller memory-region than <see cref="DoubleEndedList{T}"/>.
    ///     However, this performs slightly worse than <see cref="DoubleEndedList{T}"/> in some respects.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the <see cref="Deque{T}"/>.</typeparam>
    [DebuggerDisplay("Count = {_count}, Capacity = {_capacity}")]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    public class Deque<T> : ICollection, ICollection<T>, IEnumerable, IEnumerable<T>, IList, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        private const int _defaultCapacity = 8;

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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="Deque{T}"/>.</exception>
        public T this[int index]
        {
            get
            {
                if ((uint)index < (uint)_count)
                {
                    int i = _head + index;
                    return (i >= _capacity) ? _items[i - _capacity] : _items[i]; // _items[i % _capacity]
                }
                else
                {
                    ThrowHelpers.ThrowIndexOutOfRangeException(ThrowHelpers.M_IndexOutOfRange);
                    return default;
                }
            }

            set
            {
                if ((uint)index < (uint)_count)
                {
                    int i = _head + index;
                    if (i >= _capacity)
                    {
                        _items[i - _capacity] = value;
                    }
                    else
                    {
                        _items[i] = value;
                    }
                    // _items[i % _capacity] = value;
                    _version++;
                }
                else
                {
                    ThrowHelpers.ThrowIndexOutOfRangeException(ThrowHelpers.M_IndexOutOfRange);
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
                    ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_TypeOfValueNotSupported, nameof(value));
                }
            }
        }

        /// <summary>
        /// Gets the number of the elements contained in the <see cref="Deque{T}"/>.
        /// </summary>
        /// <returns>The number of the elements contained in the <see cref="Deque{T}"/>.</returns>
        public int Count => _count;

        /// <summary>
        /// Gets the number of total elements the <see cref="Deque{T}"/> can hold without resizing.
        /// </summary>
        /// <remarks>
        /// To sets the capacity, please use <see cref="Resize(int)"/>.
        /// </remarks>
        /// <returns>The number of elements that the <see cref="Deque{T}"/> can contain before resizing is required.</returns>
        public int Capacity
        {
            get
            {
                return _items.Length;
            }
        }

        /// <summary>
        /// Gets the value that indicates whether the <see cref="Deque{T}"/> is empty.
        /// </summary>
        /// <returns><see langword="true"/> if the <see cref="Deque{T}"/> is empty; <see langword="false"/> if not.</returns>
        public bool IsEmpty => _count == 0;

        internal T First => _items[_head];

        internal T Last
        {
            get
            {
                var tail = _head + _count - 1;
                return (tail >= _capacity) ? _items[tail - _capacity] : _items[tail];
            }
        }

        /// <summary>
        /// Initializes an new instance of the <see cref="Deque{T}"/> class.
        /// </summary>
        public Deque() : this(_defaultCapacity) { }

        /// <summary>
        /// Initializes an new instance of the <see cref="Deque{T}"/> class that contains elements copied from the specified <see cref="Deque{T}"/> and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="deque">The deque whose elements are copied to the new one.</param>
        internal Deque(Deque<T> deque)
        {
            _capacity = deque.Capacity;
            _count = deque._count;
            _head = deque._head;
            _items = new T[deque._capacity];

            deque._items.AsSpan().CopyTo(_items.AsSpan());
        }

        /// <summary>
        /// Initializes an new instance of the <see cref="Deque{T}"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new <see cref="Deque{T}"/> can initially store.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> must be positive or zero, and less than or equal to the maximum length of an array.</exception>
        public Deque(int capacity)
        {
            if (capacity == 0)
            {
                _items = [];
            }
            else
            {
                ThrowHelpers.ThrowIfArgumentIsNegative(capacity);
                ThrowHelpers.ThrowIfArgumentIsGreaterThanMaxArrayLength(capacity);
                _items = new T[capacity];
            }

            _capacity = capacity;
            _head = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Deque{T}"/> class that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied. 
        /// </summary>
        /// <param name="enumerable">The collection whose elements are copied to the new deque.</param>
        /// <exception cref="ArgumentNullException"><paramref name="enumerable"/> is <see langword="null"/>.</exception>
        public Deque(IEnumerable<T> enumerable) : this(_defaultCapacity)
        {
            ArgumentNullException.ThrowIfNull(enumerable);

            if (enumerable is ICollection<T> collection)
            {
                int capacity = Math.Min(collection.Count << 1, Array.MaxLength);

                if (capacity > _defaultCapacity)
                {
                    _items = new T[capacity];
                    _capacity = capacity;
                }

                collection.CopyTo(_items, 0);
                _count = collection.Count;
                _head = 0;
            }
            else
            {
                foreach (T item in enumerable)
                {
                    PushBack(item);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Deque{T}"/> class that contains elements copied from the specified span and has sufficient capacity to accommodate the number of the elements copied.
        /// </summary>
        /// <param name="span">The span whose elements are copied to the new deque.</param>
        public Deque(ReadOnlySpan<T> span) : this(_defaultCapacity)
        {
            int capacity = Math.Min(span.Length << 1, Array.MaxLength);

            if (capacity > _defaultCapacity)
            {
                _items = new T[capacity];
            }

            span.CopyTo(_items.AsSpan());
            _count = span.Length;
            _head = 0;
        }

        internal Deque(T[] innerArray, int head, int count)
        {
            _capacity = innerArray.Length;
            _count = count;
            _head = head;
            _items = innerArray;
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
        /// Removes all of the elements from the <see cref="Deque{T}"/>.
        /// </summary>
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                if (_head + _count > _capacity)
                {
                    ref var itemsRef = ref MemoryMarshal.GetArrayDataReference(_items);
                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), _capacity - _head).Clear(); // _items.AsSpan()[_head..]
                    MemoryMarshal.CreateSpan(ref itemsRef, _head + _count - _capacity).Clear(); // _items.AsSpan()[..(_head + _count - _capacity)]
                }
                else
                {
                    MemoryMarshal.CreateSpan(ref _items[_head], _count).Clear(); // _items.AsSpan().Slice(_head, _count)
                }
            }
            _count = 0;
            _head = _capacity >> 1;
            _version++;
        }

        /// <summary>
        /// Determines whether the <see cref="Deque{T}"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="Deque{T}"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> is found in the <see cref="Deque{T}"/>; otherwise, <see langword="false"/>.</returns>
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
        /// Copies the elements of the <see cref="Deque{T}"/> to a span.
        /// </summary>
        /// <param name="span">The span that is the destination of the elements copied from the <see cref="Deque{T}"/>.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="span"/> is less than the number of elements contained in the <see cref="Deque{T}"/>.</exception>
        public void CopyTo(Span<T> span)
        {
            if (_count == 0)
            {
                return;
            }
            if (span.Length < _count)
            {
                ThrowHelpers.ThrowArgumentException(nameof(span), "The length of the span is less than the number of elements contained in the collection.");
            }
            CopyToInternal(span);
        }

        /// <summary>
        /// Copies the elements of the <see cref="Deque{T}"/> to an <typeparamref name="T"/>[], starting at a particular index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="Deque{T}"/>.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The number of the elements in the source <see cref="Deque{T}"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (_count == 0)
            {
                return;
            }
            if (array.Length - arrayIndex < _count)
            {
                ThrowHelpers.ThrowArgumentException("The number of the elements in the source collection is greater than the available space from the specified index to the end of the destination array.", nameof(arrayIndex));
            }
            CopyToInternal(MemoryMarshal.CreateSpan(ref array[0], _count));
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (_count == 0)
            {
                return;
            }
            if (array.Length - index < _count)
            {
                ThrowHelpers.ThrowArgumentException("The number of the elements in the source collection is greater than the available space from the specified index to the end of the destination array.", nameof(index));
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

        /// <remarks>
        /// This method does NOT check if the <see cref="Deque{T}"/> is empty and if the <paramref name="span"/> has sufficient capacity to accomodate elements copied from the <see cref="Deque{T}"/>.
        /// </remarks>
        /// <param name="span"></param>
        private void CopyToInternal(Span<T> span)
        {
            ref var itemsRef = ref MemoryMarshal.GetArrayDataReference(_items);

            if (_head + _count > _capacity)
            {
                var count1 = _capacity - _head;
                var count2 = _head + _count - _capacity;
                MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), count1) // _items.AsSpan().Slice(_head, count1)
                    .CopyTo(span);
                MemoryMarshal.CreateSpan(ref itemsRef, count2) // _items.AsSpan()[..count2]
                    .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), count1), count2)); // span[count1..]
            }
            else
            {
                MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), _count).CopyTo(span); // _items.AsSpan().Slice(_head, _count)
            }
        }

        /// <summary>
        ///     Ensures that the capacity of this <see cref="Deque{T}"/> is at least the specified one.
        ///     If the current capacity is less than the specified one, resizes the internal array so that the <see cref="Deque{T}"/> can accommodate the specified number of elements without resizing.
        /// </summary>
        /// <param name="capacity">The number of elements that the <see cref="Deque{T}"/> can hold without resizing.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative or greater than <see cref="Array.MaxLength"/>.</exception>
        public void EnsureCapacity(int capacity)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(capacity);
            ThrowHelpers.ThrowIfArgumentIsGreaterThanMaxArrayLength(capacity);
            EnsureCapacityInternal(capacity);
        }

        private void EnsureCapacityInternal(int capacity)
        {
            if (capacity <= _items.Length)
            {
                return;
            }

            int newCapacity = Math.Min(Math.Max((_items.Length << 1), capacity), Array.MaxLength);
            ResizeInternal(newCapacity);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Deque{T}"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="Deque{T}"/>.</returns>
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
        /// Searches for the specified item and returns the zero-based index of the first occurrence within the entire <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="Deque{T}"/>.
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
        /// Insert an item to the <see cref="Deque{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">An object to insert. The value can be <see langword="null"/> for reference types.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="Deque{T}"/>.</exception>
        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Searches for the specified item and returns the zero-based index of the last occurrence within the entire <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="Deque{T}"/>.
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
        /// Returns the object at the beginning of the <see cref="Deque{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="Deque{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
        public T PeekFirst()
        {
            ThrowIfCollectionIsEmpty();
            return First;
        }

        /// <summary>
        /// Returns the object at the end of the <see cref="Deque{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the end of the <see cref="Deque{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
        public T PeekLast()
        {
            ThrowIfCollectionIsEmpty();
            return Last;
        }

        /// <summary>
        /// Removes and returns the object at the end of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <returns>The object at the end of the <see cref="Deque{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
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
        /// Removes and returns the specified number of objects at the end of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="count">The number of elements to remove at the end of the <see cref="Deque{T}"/>.</param>
        /// <returns>An array that contains the objects removed at the end of the <see cref="Deque{T}"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is invalid.</exception>
        public T[] PopBackRange(int count)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(count);
            if (count > _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(count), ThrowHelpers.M_ValueIsGreaterThanCount);
            }

            var destinationArray = new T[count];
            PopBackRangeInternal(destinationArray.AsSpan());
            return destinationArray;
        }

        /// <summary>
        /// Removes the certain number of objects at the end of the <see cref="Deque{T}"/> and copies them to the specified span to fill up it.
        /// </summary>
        /// <param name="destination">The span to copy the removed objects to.</param>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="destination"/> is greater than the number of elements contained in the collection.</exception>
        public void PopBackRange(Span<T> destination)
        {
            if (destination.Length > _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(destination.Length), ThrowHelpers.M_ValueIsGreaterThanCount);
            }

            PopBackRangeInternal(destination);
        }

        private void PopBackRangeInternal(Span<T> destination)
        {
            int count = destination.Length;
            int lastIdx = _head + _count - 1;
            if (lastIdx >= _capacity)
            {
                lastIdx -= _capacity; // lastIdx = (_head + _count - 1) % _capacity
            }
            int rangeBeginsAt = lastIdx - count + 1;
            ref var itemsRef = ref MemoryMarshal.GetArrayDataReference(_items);

            if (rangeBeginsAt >= 0)
            {
                var source = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, rangeBeginsAt), count); // _items.AsSpan().Slice(rangeBeginsAt, count);
                source.CopyTo(destination);

                CollectionHelpers.ClearIfReferenceOrContainsReferences(source);
            }
            else
            {
                var source_1 = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _capacity + rangeBeginsAt), -rangeBeginsAt); // _items.AsSpan()[^(-rangeBeginsAt)..]
                var source_2 = MemoryMarshal.CreateSpan(ref itemsRef, lastIdx + 1); // _items.AsSpan()[..(lastIdx + 1)]

                source_1.CopyTo(destination);
                source_2.CopyTo(
                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), -rangeBeginsAt), destination.Length + rangeBeginsAt) // destination[(-rangeBeginsAt)..]
                );

                CollectionHelpers.ClearIfReferenceOrContainsReferences(source_1, source_2);
            }

            _count -= count;
            _version++;
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="Deque{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
        public T PopFront()
        {
            ThrowIfCollectionIsEmpty();
            return PopFrontInternal();
        }

        private T PopFrontInternal()
        {
            var res = First;
            CollectionHelpers.SetDefaultValueIfReferenceOrContainsReferences(ref _firstRef);

            _head++;
            if (_head == _capacity)
            {
                _head = 0; // _head = (_head + 1) % _capacity
            }
            _count--;
            _version++;

            return res;
        }

        /// <summary>
        /// Removes and returns the specified number of objects at the front of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="count">The number of elements to remove at the front of the <see cref="Deque{T}"/>.</param>
        /// <returns>An array that contains the objects removed at the front of the <see cref="Deque{T}"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is invalid.</exception>
        public T[] PopFrontRange(int count)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(count);
            if (count > _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(count), ThrowHelpers.M_ValueIsGreaterThanCount);
            }

            var destinationArray = new T[count];
            PopFrontRangeInternal(destinationArray.AsSpan());
            return destinationArray;
        }

        /// <summary>
        /// Removes the certain number of objects at the front of the <see cref="Deque{T}"/> and copies them to the specified span to fill up it.
        /// </summary>
        /// <param name="destination">The span to copy the removed objects to.</param>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="destination"/> is greater than the number of elements contained in the collection.</exception>
        public void PopFrontRange(Span<T> destination)
        {
            if (destination.Length > _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(destination.Length), ThrowHelpers.M_ValueIsGreaterThanCount);
            }

            PopFrontRangeInternal(destination);
        }

        private void PopFrontRangeInternal(Span<T> destination)
        {
            int count = destination.Length;
            int rangeEndsAt = _head + count - 1;
            ref var itemsRef = ref MemoryMarshal.GetArrayDataReference(_items);

            if (rangeEndsAt < _capacity)
            {
                var source = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), count); // _items.AsSpan().Slice(_head, count)
                source.CopyTo(destination);
                CollectionHelpers.ClearIfReferenceOrContainsReferences(source);
            }
            else
            {
                int len_1 = _capacity - _head;
                var source_1 = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), len_1); // _items.AsSpan()[_head..]
                var source_2 = MemoryMarshal.CreateSpan(ref itemsRef, rangeEndsAt - _capacity + 1); // _items.AsSpan()[..((rangeEndsAt - _capacity) + 1)]
                //                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~ <- (rangeEndsAt % _capacity) = (rangeEndsAt - _capacity) 

                source_1.CopyTo(destination);
                source_2.CopyTo(
                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), len_1), destination.Length - len_1) // destination[(_capacity - _head)..]
                );

                CollectionHelpers.ClearIfReferenceOrContainsReferences(source_1, source_2);
            }

            _head = rangeEndsAt + 1;
            if (_head >= _capacity)
            {
                _head -= _capacity; // _head = (rangeEndsAt + 1) - _capacity
            }
            _count -= count;
            _version++;
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the end of the <see cref="Deque{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushBack(T item)
        {
            if (_count == _capacity)
            {
                Grow();
            }

            int index = _head + _count;
            if (index >= _capacity)
            {
                index -= _capacity; // _end = (_head + _count) % _capacity
            }
            _items[index] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <remarks>
        /// Please note that, if the specified collection has many objects, this method may take a long time or temporarily occupy large memory region to copy the objects.
        /// To avoid this, please set a <see cref="ReadOnlySpan{T}"/> to the <paramref name="items"/> parameter instead.
        /// </remarks>
        /// <param name="items">The collection whose elements should be added to the end of the <see cref="Deque{T}"/>.</param>
        public void PushBackRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items is ICollection<T> collection)
            {
                var src = new T[collection.Count];
                collection.CopyTo(src, 0);
                PushBackRange(src.AsSpan());
            }
            else
            {
                foreach (var v in items)
                {
                    PushBack(v);
                }
            }
        }

        /// <summary>
        /// Adds the elements of the specified memory region to the end of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="items">The read-only span whose elements should be added to the end of the <see cref="Deque{T}"/>.</param>
        public void PushBackRange(ReadOnlySpan<T> items)
        {
            int count = items.Length;
            if (count == 0)
            {
                return;
            }

            int requiredCapacity = _count + count;
            if (requiredCapacity > Array.MaxLength)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            EnsureCapacityInternal(requiredCapacity);

            int rangeBeginsAt = _head + _count;
            if (rangeBeginsAt >= _capacity)
            {
                rangeBeginsAt -= _capacity; // rangeBeginsAt = (_head + _count) % _capacity
            }

            ref var destRef = ref MemoryMarshal.GetArrayDataReference(_items);
            int sliceAt = _capacity - rangeBeginsAt;

            if (sliceAt >= count)
            {
                items.CopyTo(
                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destRef, rangeBeginsAt), items.Length) // _items.AsSpan().Slice(rangeBeginsAt, items.Length)
                );
            }
            else
            {
                ref var itemsRef = ref MemoryMarshal.GetReference(items);

                MemoryMarshal.CreateSpan(ref itemsRef, sliceAt) // items[..sliceAt]
                    .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destRef, rangeBeginsAt), sliceAt)); // _items.AsSpan().Slice(rangeBeginsAt, sliceAt)
                MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, sliceAt), items.Length - sliceAt) // items[sliceAt..]
                    .CopyTo(_items.AsSpan());
            }

            _count += count;
            _version++;
        }

        /// <summary>
        /// Adds an object to the beginning of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the beginning of the <see cref="Deque{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushFront(T item)
        {
            if (_count == _capacity)
            {
                Grow();
            }

            _head = (_head == 0) ? (_capacity - 1) : (_head - 1); // ((_head - 1) + _capacity) % _capacity
            _items[_head] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Adds the elements of the specified collection to the front of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <remarks>
        /// Please note that, this method may take a long time or temporarily occupy large memory region to copy the objects.
        /// To avoid this, please set a <see cref="ReadOnlySpan{T}"/> to the <paramref name="items"/> parameter instead.
        /// </remarks>
        /// <param name="items">The collection whose elements should be added to the front of the <see cref="Deque{T}"/>.</param>
        public void PushFrontRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items is ICollection<T> collection)
            {
                var src = new T[collection.Count];
                collection.CopyTo(src, 0);
                PushFrontRange(src.AsSpan());
            }
            else
            {
                foreach (var v in items.Reverse())
                {
                    PushFront(v);
                }
            }
        }

        /// <summary>
        /// Adds the elements of the specified memory region to the front of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="items">The read-only span whose elements should be added to the front of the <see cref="Deque{T}"/>.</param>
        public void PushFrontRange(ReadOnlySpan<T> items)
        {
            int count = items.Length;
            if (count == 0)
            {
                return;
            }

            int requiredCapacity = _count + count;
            if (requiredCapacity > Array.MaxLength)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            EnsureCapacityInternal(requiredCapacity);

            ref var destRef = ref MemoryMarshal.GetArrayDataReference(_items);
            int rangeBeginsAt = _head - count;

            if (rangeBeginsAt >= 0)
            {
                items.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destRef, rangeBeginsAt), items.Length)); // _items.AsSpan().Slice(rangeBeginsAt, items.Length)
                _head = rangeBeginsAt;
            }
            else
            {
                rangeBeginsAt = -rangeBeginsAt; // rangeBeginsAt = Math.Abs(rangeBeginsAt)
                ref var itemsRef = ref MemoryMarshal.GetReference(items);

                MemoryMarshal.CreateSpan(ref itemsRef, rangeBeginsAt) // items[..rangeBeginsAt]
                    .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destRef, _capacity - rangeBeginsAt), rangeBeginsAt)); // _items.AsSpan()[^rangeBeginsAt..]
                MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, rangeBeginsAt), items.Length - rangeBeginsAt) // items[rangeBeginsAt..]
                    .CopyTo(MemoryMarshal.CreateSpan(ref destRef, _capacity)); // _items.AsSpan()

                _head = _capacity - rangeBeginsAt;
            }

            _count += count;
            _version++;
        }

        /// <summary>
        /// Removes the first occurrence of the specified object from the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="Deque{T}"/>.</param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            int idx = IndexOf(item);

            if (idx < 0)
            {
                return false;
            }
            else
            {
                RemoveAt(idx);
                return true;
            }
        }

        void IList.Remove(object? value)
        {
            if (value is T tValue)
            {
                Remove(tValue);
            }
        }

        /// <summary>
        /// Removes the element at the specified index in the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index of the <see cref="Deque{T}"/>.</exception>
        public void RemoveAt(int index)
        {
            if ((uint)index >= _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(index), ThrowHelpers.M_IndexOutOfRange);
            }

            RemoveRangeInternal(index, index + 1);
        }

        /// <summary>
        /// Remove a range of elements from the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is negative.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the <see cref="Deque{T}"/>.</exception>
        public void RemoveRange(int index, int count)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(index);
            ThrowHelpers.ThrowIfArgumentIsNegative(count);

            int endIndex = checked(index + count);
            if (endIndex > _count)
            {
                ThrowHelpers.ThrowArgumentException("The number of elements to remove is greater than the available space from the specified index to the end of the deque.", "[index, count]");
            }

            RemoveRangeInternal(index, endIndex);
        }

        private void RemoveRangeInternal(int beginIndex, int endIndex)
        {
            var count = endIndex - beginIndex;

            if (count == 0)
            {
                return;
            }

            if (beginIndex == 0)
            {
                //
                // The following process is almost the same as that of the PopFrontRange method.
                //

                if (endIndex == _count) // #00
                {
                    Clear();
                    return;
                }
                else
                {
                    int newHead = checked(_head + count);
                    ref var itemsRef = ref MemoryMarshal.GetArrayDataReference(_items);

                    if (newHead < _capacity) // #01
                    {
                        CollectionHelpers.ClearIfReferenceOrContainsReferences(
                            MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), count) // items.Slice(_head, count)
                        );
                    }
                    else // #02
                    {
                        newHead -= _capacity; // newHead %= _capacity
                        CollectionHelpers.ClearIfReferenceOrContainsReferences(
                            MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), _capacity - _head), // items[_head..]
                            MemoryMarshal.CreateSpan(ref itemsRef, newHead) // items[..newHead]
                        );
                    }

                    _head = newHead;
                }
            }
            else if (endIndex == _count)
            {
                //
                // The following process is almost the same as that of the PopBackRange method.
                //

                int _end = checked(_head + _count);
                if (_end > _capacity)
                {
                    _end -= _capacity;
                }

                int newEnd = _end - count;
                ref var itemsRef = ref MemoryMarshal.GetArrayDataReference(_items);

                if (newEnd >= 0) // #03
                {
                    CollectionHelpers.ClearIfReferenceOrContainsReferences(
                        MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, newEnd), count) // _items.AsSpan().Slice(newEnd, count)
                    );
                }
                else // #04
                {
                    CollectionHelpers.ClearIfReferenceOrContainsReferences(
                        MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _capacity + newEnd), -newEnd), // _items.AsSpan()[(^newEnd)..]
                        MemoryMarshal.CreateSpan(ref itemsRef, _end) // _items.AsSpan()[.._end]
                    );
                }
            }
            else
            {
                if (beginIndex < _count - endIndex)
                {
                    var items = _items.AsSpan();

                    int iFrom = _head + beginIndex - 1;
                    if (iFrom >= _capacity)
                    {
                        iFrom -= _capacity; // iFrom = (_head + beginIndex - 1) % _capacity
                    }

                    int iTo = iFrom + count;
                    if (iTo >= _capacity)
                    {
                        iTo -= _capacity; // iTo = (iFrom + count) % _capacity
                    }

                    for (int i = 0; i < beginIndex; i++)
                    {
                        items[iTo--] = items[iFrom--];
                        if (iFrom == -1)
                        {
                            iFrom += _capacity;
                        }
                        if (iTo == -1)
                        {
                            iTo += _capacity;
                        }
                    }

                    int newHead = checked(_head + count);
                    ref var itemsRef = ref MemoryMarshal.GetReference(items);

                    if (newHead < _capacity) // #05
                    {
                        CollectionHelpers.ClearIfReferenceOrContainsReferences(
                            MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), count) // items.Slice(_head, count)
                        );
                    }
                    else // #06
                    {
                        newHead -= _capacity;
                        CollectionHelpers.ClearIfReferenceOrContainsReferences(
                            MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), _capacity - _head), // items[_head..]
                            MemoryMarshal.CreateSpan(ref itemsRef, newHead) // items[..newHead]
                        );
                    }

                    _head = newHead;
                }
                else
                {
                    int _end = _head + _count;
                    var items = _items.AsSpan();

                    int iFrom = _head + endIndex;
                    if (iFrom >= _capacity)
                    {
                        iFrom -= _capacity; // iFrom = (_head + endIndex) % _capacity
                    }

                    int iTo = iFrom - count;
                    if (iTo < 0)
                    {
                        iTo += _capacity; // iTo = (iFrom - count) % _capacity
                    }

                    for (int i = 0; i < _count - endIndex; i++)
                    {
                        if (iFrom == _capacity)
                        {
                            iFrom = 0;
                        }
                        if (iTo == _capacity)
                        {
                            iTo = 0;
                        }
                        items[iTo++] = items[iFrom++];
                    }

                    if (_end >= _capacity)
                    {
                        _end -= _capacity; // _end %= _capacity
                    }

                    int newEnd = _end - count;
                    ref var itemsRef = ref MemoryMarshal.GetReference(items);

                    if (newEnd > 0) // #07
                    {
                        CollectionHelpers.ClearIfReferenceOrContainsReferences(
                            MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, newEnd), count) // items.Slice(newEnd, count)
                        );
                    }
                    else // #08
                    {
                        newEnd += _capacity;
                        CollectionHelpers.ClearIfReferenceOrContainsReferences(
                            MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, newEnd), _capacity - newEnd), // items[newEnd..]
                            MemoryMarshal.CreateSpan(ref itemsRef, _end) // items[.._end]
                        );
                    }
                }
            }

            _count -= count;
            _version++;
        }

        /// <summary>
        /// Resizes the internal array to the specified size.
        /// </summary>
        /// <param name="capacity"></param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than the number of elements contained in the <see cref="Deque{T}"/> or greater than <see cref="Array.MaxLength"/>.</exception>
        public void Resize(int capacity)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(capacity);
            ThrowHelpers.ThrowIfArgumentIsGreaterThanMaxArrayLength(capacity);
            if (capacity < _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(capacity), "The capacity is less than the number of the elements contained in the collection.");
            }

            ResizeInternal(capacity);
        }

        private void ResizeInternal(int capacity)
        {
            if (capacity == 0)
            {
                _items = [];
            }
            else
            {
                T[] items = new T[capacity];
                CopyToInternal(items.AsSpan());
                _items = items;
            }
            _capacity = capacity;
            _head = 0;
            _version++;
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="Deque{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter.
        /// The object is not removed from the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the beginning of the <see cref="Deque{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the beginning of the <see cref="Deque{T}"/>; <see langword="false"/> if the <see cref="Deque{T}"/> is empty.</returns>
        public bool TryPeekFirst([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                item = First;
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the end of the <see cref="Deque{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter.
        /// The object is not removed from the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the end of the <see cref="Deque{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the end of the <see cref="Deque{T}"/>; <see langword="false"/> if the <see cref="Deque{T}"/> is empty.</returns>
        public bool TryPeekLast([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                item = Last;
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the end of the <see cref="Deque{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter and removes it from the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the end of the <see cref="Deque{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the end of the <see cref="Deque{T}"/>; <see langword="false"/> if the <see cref="Deque{T}"/> is empty.</returns>
        public bool TryPopBack([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                item = PopBackInternal();
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="Deque{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter and removes it from the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the beginning of the <see cref="Deque{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the beginning of the <see cref="Deque{T}"/>; <see langword="false"/> if the <see cref="Deque{T}"/> is empty.</returns>
        public bool TryPopFront([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                item = PopFrontInternal();
                return true;
            }
        }

        /// <summary>
        /// Enlarge the internal array to twice its size.
        /// </summary>
        /// <exception cref="Exception">The number of elements contained in the <see cref="Deque{T}"/> has reached its upper limit.</exception>
        private void Grow()
        {
            int capacity = Math.Min(
                Math.Max(_capacity << 1, _defaultCapacity),
                Array.MaxLength
                );

            if (capacity < _count + 2)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            ResizeInternal(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfCollectionIsEmpty()
        {
            if (_count == 0)
            {
                ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_CollectionIsEmpty);
            }
        }

        /// <summary>
        /// Enumerates the elements of a <see cref="Deque{T}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator, IEnumerator<T>
        {
            private readonly Deque<T> _deque;
            private int _index = -1;
            private readonly int _version;

            /// <summary>Gets the element in the <see cref="Deque{T}"/> at the current position of the enumerator.</summary>
            /// <returns>The element in the <see cref="Deque{T}"/> at the current position of the enumerator.</returns>
            public readonly T Current => _deque._items[(_deque._head + _index) % _deque._capacity];

            readonly object? IEnumerator.Current => Current;

            internal Enumerator(Deque<T> deque)
            {
                _deque = deque;
                _version = deque._version;
            }

            /// <inheritdoc/>
            public readonly void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="Deque{T}"/>.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the <see cref="Deque{T}"/>.</returns>
            /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                ThrowIfCollectionModified();
                return ++_index < _deque._count;
            }

            /// <summary>Set the enumerator to its initial position, which is the before the first element in the <see cref="Deque{T}"/>.</summary>
            /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> was modified after the enumerator was created.</exception>
            public void Reset()
            {
                ThrowIfCollectionModified();
                _index = -1;
            }

            /// <summary>
            /// Throw an exception if the <see cref="Deque{T}"/> was modified after the enumerator was created.
            /// </summary>
            /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> was modified after the enumerator was created.</exception>
            private readonly void ThrowIfCollectionModified()
            {
                if (_version != _deque._version)
                {
                    ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_CollectionModifiedAfterEnumeratorCreated);
                }
            }
        }
    }
}
