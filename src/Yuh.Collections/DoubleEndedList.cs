using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yuh.Collections.Debugging;

namespace Yuh.Collections
{
    /// <summary>
    /// Represents a list for which elements can be added to or removed from the front or back.
    /// </summary>
    /// <remarks>
    /// <para>This supports random-access to the elements contained in the collection and addition of elements to the front or back, or removal from the front or back.</para>
    /// </remarks>
    /// <typeparam name="T">The type of elements in the <see cref="DoubleEndedList{T}"/>.</typeparam>
    [DebuggerDisplay("Count = {_count}, Capacity = {_items.Length}")]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    public class DoubleEndedList<T> : ICollection<T>, IDeque<T>, IList, IList<T>, IReadOnlyDeque<T>, IReadOnlyList<T>
    {
        private const int _defaultCapacity = 8;
#pragma warning disable IDE0301
        private static readonly T[] _s_emptyArray = Array.Empty<T>();
#pragma warning restore IDE0301

        private T[] _items;
        private int _count = 0;
        private int _head = 0;
        private int _version = 0;

        bool IList.IsFixedSize => false;
        bool ICollection<T>.IsReadOnly => false;
        bool IList.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <value>The element at the specified index.</value>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="DoubleEndedList{T}"/>.</exception>
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                {
                    ThrowHelpers.ThrowIndexOutOfRangeException(ThrowHelpers.M_IndexOutOfRange);
                }
                return _items[_head + index];
            }

            set
            {
                if ((uint)index >= (uint)_count)
                {
                    ThrowHelpers.ThrowIndexOutOfRangeException(ThrowHelpers.M_IndexOutOfRange);
                }
                _items[_head + index] = value;
                _version++;
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
                    ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_TypeOfValueNotSupported);
                }
            }
        }

        /// <summary>
        /// Gets the number of total elements the <see cref="DoubleEndedList{T}"/> can hold without resizing.
        /// </summary>
        /// <remarks>
        /// To sets the capacity, please use <see cref="Resize(int)"/> or <see cref="Resize(int, int)"/>.
        /// </remarks>
        /// <returns>The number of elements that the <see cref="DoubleEndedList{T}"/> can contain before resizing is required.</returns>
        public int Capacity
        {
            get
            {
                return _items.Length;
            }
        }

        internal T First => _items[_head];

        internal T Last => _items[_head + _count - 1];

        /// <summary>
        /// Gets the number of elements that can be added at the beginning of the <see cref="DoubleEndedList{T}"/> without resizing the internal data structure.
        /// </summary>
        /// <returns>The number of elements that can be added at the beginning of the <see cref="DoubleEndedList{T}"/> without resizing the internal data structure.</returns>
        public int FrontMargin => _head;

        /// <summary>
        /// Gets the number of elements that can be added at the end of the <see cref="DoubleEndedList{T}"/> without resizing the internal data structure.
        /// </summary>
        /// <returns>The number of elements that can be added at the end of the <see cref="DoubleEndedList{T}"/> without resizing the internal data structure.</returns>
        public int BackMargin => _items.Length - _head - _count;

        /// <summary>
        /// Gets the number of the elements contained in the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <returns>The number of the elements contained in the <see cref="DoubleEndedList{T}"/>.</returns>
        public int Count => _count;

        /// <summary>
        /// Gets the value that indicates whether the <see cref="DoubleEndedList{T}"/> is empty.
        /// </summary>
        /// <returns><see langword="true"/> if the <see cref="DoubleEndedList{T}"/> is empty; <see langword="false"/> if not.</returns>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleEndedList{T}"/> class.
        /// </summary>
        public DoubleEndedList() : this(_defaultCapacity) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleEndedList{T}"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new <see cref="DoubleEndedList{T}"/> can initially store.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.</exception>
        public DoubleEndedList(int capacity)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(capacity);

            if (capacity == 0)
            {
                _items = _s_emptyArray;
            }
            else
            {
                capacity = Math.Min(capacity, Array.MaxLength);
                _items = new T[capacity];
                _head = _items.Length >> 1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleEndedList{T}"/> class that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied. 
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
#if NET9_0_OR_GREATER
        [OverloadResolutionPriority(-1)]
#endif
        public DoubleEndedList(IEnumerable<T> collection) : this(0)
        {
            ArgumentNullException.ThrowIfNull(collection);

            if (collection is ICollection<T> c)
            {
                var count = c.Count;
                if (count == 0)
                {
                    return;
                }

                Resize(0, count);
                c.CopyTo(_items, 0);
                _count = count;
            }
            else
            {
                ResizeInternal(_defaultCapacity);
                foreach (var v in collection)
                {
                    PushBack(v);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleEndedList{T}"/> class that contains elements copied from the specified span and has sufficient capacity to accommodate the number of the elements copied.
        /// </summary>
        /// <param name="span">The span whose elements are copied to the new list.</param>
        public DoubleEndedList(ReadOnlySpan<T> span) : this(span.Length)
        {
            if (span.IsEmpty)
            {
                return;
            }

            span.CopyTo(_items.AsSpan());
            _count = span.Length;
            _head = 0;
            _version++;
        }

        internal DoubleEndedList(T[] innerArray, int head, int count)
        {
            _count = count;
            _head = head;
            _items = innerArray;
        }

        int IList.Add(object? value)
        {
            if (value is T tValue)
            {
                PushBack(tValue);
                return _head + _count - 1;
            }
            else
            {
                return -1;
            }
        }

        void ICollection<T>.Add(T item)
        {
            PushBack(item);
        }

        /// <summary>
        /// Creates a new read-only span over the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <remarks>
        /// Items should not be added or removed from the <see cref="DoubleEndedList{T}"/> while the returned <see cref="ReadOnlySpan{T}"/> is in use.
        /// </remarks>
        /// <returns>The read-only span representation of the <see cref="DoubleEndedList{T}"/>.</returns>
        public ReadOnlySpan<T> AsReadOnlySpan() => AsSpan();

        /// <summary>
        /// Creates a new span over the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <returns>The span representation of the <see cref="DoubleEndedList{T}"/>.</returns>
        internal Span<T> AsSpan()
        {
            var count = _count;
            if (_count == 0)
            {
                return [];
            }
            return _items.AsSpan().Slice(_head, count);
        }

        /// <summary>
        /// Removes all of the elements from the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        public void Clear()
        {
            CollectionHelpers.ClearIfReferenceOrContainsReferences(AsSpan());
            _count = 0;
            _head = _items.Length >> 1;
            _version++;
        }

        bool IList.Contains(object? value)
        {
            if (value is T tValue)
            {
                return Contains(tValue);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether the <see cref="DoubleEndedList{T}"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="DoubleEndedList{T}"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> is found in the <see cref="DoubleEndedList{T}"/>; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        /// <summary>
        /// Copies the elements of the <see cref="DoubleEndedList{T}"/> to a span.
        /// </summary>
        /// <param name="span">The span that is the destination of the elements copied from the <see cref="DoubleEndedList{T}"/>.</param>
        /// <exception cref="ArgumentException">The number of the elements in the source <see cref="DoubleEndedList{T}"/> is greater than the length of <paramref name="span"/>.</exception>
        public void CopyTo(Span<T> span)
        {
            AsReadOnlySpan().CopyTo(span);
        }

        /// <summary>
        /// Copies the elements of the <see cref="DoubleEndedList{T}"/> to an <typeparamref name="T"/>[], starting at a particular index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="DoubleEndedList{T}"/>.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The number of the elements in the source <see cref="DoubleEndedList{T}"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ThrowHelpers.ThrowIfArgumentIsNegative(arrayIndex);
            if (arrayIndex >= array.Length)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(arrayIndex), "The value must be less than the number of elements contained in the collection.");
            }

            CopyTo(MemoryMarshal.CreateSpan(ref array[arrayIndex], array.Length - arrayIndex));
        }

        void ICollection.CopyTo(Array array, int index)
        {
            Array.Copy(_items, _head, array, index, _count);
        }

        /// <summary>
        ///     Ensures that the capacity of this <see cref="DoubleEndedList{T}"/> is at least the specified one.
        ///     If the current capacity is less than the specified one, resizes the internal array so that the <see cref="DoubleEndedList{T}"/> can accommodate the specified number of elements without resizing.
        /// </summary>
        /// <param name="capacity">The number of elements that the <see cref="DoubleEndedList{T}"/> can hold without resizing.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative or greater than <see cref="Array.MaxLength"/>.</exception>
        public void EnsureCapacity(int capacity)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(capacity);
            ThrowHelpers.ThrowIfArgumentIsGreaterThanMaxArrayLength(capacity);
            EnsureCapacityInternal(capacity);
        }

        /// <summary>
        /// Ensures that the margins at the beginning and back of the <see cref="DoubleEndedList{T}"/> are respectively at least those specified.
        /// </summary>
        /// <param name="frontMargin">The number of elements that can be added at the beginning of the <see cref="DoubleEndedList{T}"/> without resizing the internal data structure.</param>
        /// <param name="backMargin">The number of elements that can be added at the end of the <see cref="DoubleEndedList{T}"/> without resizing the internal data structure.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="frontMargin"/> or <paramref name="backMargin"/> is 0.</exception>
        public void EnsureCapacity(int frontMargin, int backMargin)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(frontMargin);
            ThrowHelpers.ThrowIfArgumentIsNegative(backMargin);

            if (checked(frontMargin + backMargin + _count) > Array.MaxLength)
            {
                ThrowHelpers.ThrowArgumentException("The total needed capacity is greater than `Array.MaxLength`.", "{ frontMargin, backMargin }");
            }

            EnsureCapacityInternal(frontMargin, backMargin);
        }

        /// <remarks>
        /// If <paramref name="capacity"/> is greater than <see cref="Array.MaxLength"/>, this does NOT throw any exceptions and treats <paramref name="capacity"/> as equal to <see cref="Array.MaxLength"/>.
        /// </remarks>
        private void EnsureCapacityInternal(int capacity)
        {
            if (capacity <= _items.Length)
            {
                return;
            }

            int newCapacity = Math.Min(Math.Max(_items.Length << 1, capacity), Array.MaxLength);

            Grow(newCapacity);
        }

        /// <remarks>
        /// This does NOT examine whether the total required capacity is valid (between 0 and <see cref="Array.MaxLength"/>.)
        /// </remarks>
        private void EnsureCapacityInternal(int frontMargin, int backMargin)
        {
            if (frontMargin <= this.FrontMargin && backMargin <= this.BackMargin)
            {
                return;
            }

            int neededCapacity = frontMargin + backMargin + _count;
            int doubledCapacity = Math.Min(_items.Length << 1, Array.MaxLength);

            if (neededCapacity >= doubledCapacity)
            {
                ResizeInternal(neededCapacity, frontMargin);
            }
            else
            {
                int capacityDiff = doubledCapacity - neededCapacity; // this is always positive.
                int marginDiff = Math.Clamp((backMargin - frontMargin), -capacityDiff, capacityDiff);
                ResizeInternal(doubledCapacity, frontMargin + ((capacityDiff + marginDiff) >> 1));
            }
        }

        /// <summary>
        /// Searches for the first element that matches the conditions defined by the specified <see cref="Predicate{T}"/>.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first element that matches the specified conditions, if found; otherwise, <c>-1</c>.</returns>
        public int Find(Predicate<T> match)
        {
            var span = AsReadOnlySpan();
            for (int i = 0; i < _count; i++)
            {
                if (match(span[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Searches for the last element that matches the conditions defined by the specified <see cref="Predicate{T}"/>.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the last element that matches the specified conditions, if found; otherwise, <c>-1</c>.</returns>
        public int FindLast(Predicate<T> match)
        {
            var span = AsReadOnlySpan();
            for (int i = _count - 1; i >= 0; i--)
            {
                if (match(span[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="DoubleEndedList{T}"/>.</returns>
        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int IList.IndexOf(object? value)
        {
            if (value is T tValue)
            {
                return IndexOf(tValue);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Searches for the specified item and returns the zero-based index of the first occurrence within the entire <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="DoubleEndedList{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        /// <returns>The zero-based index of the first occurrence of <paramref name="item"/>, if found; otherwise, <c>-1</c>.</returns>
        public int IndexOf(T item)
        {
            var count = _count;
            if (count == 0)
            {
                return -1;
            }

            var idx = Array.IndexOf(_items, item, _head, count);
            return (idx >= 0) ? (idx - _head) : -1;
        }

        void IList.Insert(int index, object? value)
        {
            if (value is T tValue)
            {
                Insert(index, tValue);
            }
            else
            {
                ThrowHelpers.ThrowArgumentException(ThrowHelpers.M_TypeOfValueNotSupported, nameof(value));
            }
        }

        /// <summary>
        /// Inserts an item to the <see cref="DoubleEndedList{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">An object to insert. The value can be <see langword="null"/> for reference types.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is invalid (less than 0 or greater than <see cref="Count"/>.)</exception>
        public void Insert(int index, T item)
        {
            InsertRange(index, [item]);
        }

        /// <summary>
        /// Inserts the elements of the specified collection into the <see cref="DoubleEndedList{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="items">The collection whose elements should be inserted into the <see cref="DoubleEndedList{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is invalid (less than 0 or greater than <see cref="Count"/>.)</exception>
#if NET9_0_OR_GREATER
        [OverloadResolutionPriority(-1)]
#endif
        public void InsertRange(int index, IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if ((uint)index > (uint)_count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(index), ThrowHelpers.M_IndexOutOfRange);
            }

            if (items is ICollection<T> collection)
            {
                var count = collection.Count;
                if (count == 0)
                {
                    return;
                }
                ReserveRangeForInsertInternal(index, collection.Count);
                collection.CopyTo(_items, _head + index);
            }
            else
            {
                using CollectionBuilder<T> builder = new();
                builder.AppendRange(items);

                var count = builder.Count;
                if (count == 0)
                {
                    return;
                }
                ReserveRangeForInsertInternal(index, builder.Count);
                builder.CopyTo(_items.AsSpan().Slice(_head + index, count));
            }
        }

        /// <summary>
        /// Inserts the elements of the specified span into the <see cref="DoubleEndedList{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="items">The span whose elements should be inserted into the <see cref="DoubleEndedList{T}"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is invalid (less than 0 or greater than <see cref="Count"/>.)</exception>
        public void InsertRange(int index, ReadOnlySpan<T> items)
        {
            if ((uint)index > (uint)_count)  // same as (index < 0 || _count < index)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(index), ThrowHelpers.M_IndexOutOfRange);
            }

            if (items.IsEmpty)
            {
                return;
            }
            ReserveRangeForInsertInternal(index, items.Length);
            items.CopyTo(_items.AsSpan().Slice(_head + index, items.Length));
        }

        /// <summary>
        /// Searches for the specified item and returns the zero-based index of the last occurrence within the entire <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="DoubleEndedList{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        /// <returns>The zero-based index of the last occurrence of <paramref name="item"/>, if found; otherwise, <c>-1</c>.</returns>
        public int LastIndexOf(T item)
        {
            var count = _count;
            if (count == 0)
            {
                return -1;
            }

            var idx = Array.LastIndexOf(_items, item, _head + count - 1, count);
            return (idx >= 0) ? (idx - _head) : -1;
        }

        /// <summary>
        /// Returns the object at the beginning of the <see cref="DoubleEndedList{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="DoubleEndedList{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="DoubleEndedList{T}"/> is empty.</exception>
        public T PeekFirst()
        {
            ThrowIfEmpty();
            return First;
        }

        /// <summary>
        /// Returns the object at the end of the <see cref="DoubleEndedList{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the end of the <see cref="DoubleEndedList{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="DoubleEndedList{T}"/> is empty.</exception>
        public T PeekLast()
        {
            ThrowIfEmpty();
            return Last;
        }

        /// <summary>
        /// Removes and returns the object at the end of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <returns>The object at the end of the <see cref="DoubleEndedList{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="DoubleEndedList{T}"/> is empty.</exception>
        public T PopBack()
        {
            ThrowIfEmpty();
            return PopBackInternal();
        }

        private T PopBackInternal()
        {
            var index = _head + _count - 1;
            var item = _items[index];
            CollectionHelpers.SetDefaultValueIfReferenceOrContainsReferences(ref _items[index]);

            _count--;
            _version++;
            return item;
        }

        /// <summary>
        /// Removes and returns the specified number of objects at the end of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="count">The number of elements to remove at the end of the <see cref="DoubleEndedList{T}"/>.</param>
        /// <returns>An array that contains the objects removed at the end of the <see cref="DoubleEndedList{T}"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is invalid.</exception>
        public T[] PopBackRange(int count)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(count);
            if (count > _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(count), ThrowHelpers.M_ValueIsGreaterThanCount);
            }

            var destinationArray = new T[count];
            PopBackRange(destinationArray.AsSpan());
            return destinationArray;
        }

        /// <summary>
        /// Removes the certain number of objects at the end of the <see cref="DoubleEndedList{T}"/> and copies them to the specified span to fill up it.
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
            var source = MemoryMarshal.CreateSpan(ref _items[_head + _count - count], count); // AsSpan()[(^count)..]
            source.CopyTo(destination);

            _count -= count;
            _version++;
            CollectionHelpers.ClearIfReferenceOrContainsReferences(source);
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="DoubleEndedList{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="DoubleEndedList{T}"/> is empty.</exception>
        public T PopFront()
        {
            ThrowIfEmpty();
            return PopFrontInternal();
        }

        private T PopFrontInternal()
        {
            var item = _items[_head];
            CollectionHelpers.SetDefaultValueIfReferenceOrContainsReferences(ref _items[_head]);

            _count--;
            _head++;
            _version++;
            return item;
        }

        /// <summary>
        /// Removes and returns the specified number of objects at the front of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="count">The number of elements to remove at the front of the <see cref="DoubleEndedList{T}"/>.</param>
        /// <returns>An array that contains the objects removed at the front of the <see cref="DoubleEndedList{T}"/>.</returns>
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
        /// Removes the certain number of objects at the front of the <see cref="DoubleEndedList{T}"/> and copies them to the specified span to fill up it.
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
            var source = MemoryMarshal.CreateSpan(ref _items[_head], count); // AsSpan()[..count]
            source.CopyTo(destination);

            _count -= count;
            _head += count;
            _version++;
            CollectionHelpers.ClearIfReferenceOrContainsReferences(source);
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the end of the <see cref="DoubleEndedList{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushBack(T item)
        {
            int index = _head + _count;

            if (index == _items.Length)
            {
                Grow();
            }

            index = _head + _count;
            _items[index] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="items">The collection whose elements should be added to the end of the <see cref="DoubleEndedList{T}"/>.</param>
#if NET9_0_OR_GREATER
        [OverloadResolutionPriority(-1)]
#endif
        public void PushBackRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items is ICollection<T> collection)
            {
                int count = collection.Count;
                if (count == 0)
                {
                    return;
                }

                int requiredCapacity = _count + count;

                if (requiredCapacity > Array.MaxLength)
                {
                    ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
                }
                else
                {
                    EnsureCapacityInternal(0, count);
                    collection.CopyTo(_items, _head + _count);
                    _count += count;
                    _version++;
                }
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
        /// Adds the elements of the specified memory region to the end of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="items">The read-only span whose elements should be added to the end of the <see cref="DoubleEndedList{T}"/>.</param>
        public void PushBackRange(ReadOnlySpan<T> items)
        {
            if (items.IsEmpty)
            {
                return;
            }

            int requiredCapacity = _count + items.Length;

            if (requiredCapacity > Array.MaxLength)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }
            else
            {
                EnsureCapacityInternal(0, items.Length);
                items.CopyTo(MemoryMarshal.CreateSpan(ref _items[_head + _count], items.Length)); // _items.AsSpan().Slice(_head + _count, items.Length)
                _count += items.Length;
                _version++;
            }
        }

        /// <summary>
        /// Adds an object to the beginning of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the beginning of the <see cref="DoubleEndedList{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushFront(T item)
        {
            if (_head == 0)
            {
                Grow();
            }

            _items[--_head] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Adds the elements of the specified collection to the front of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="items">The collection whose elements should be added to the front of the <see cref="DoubleEndedList{T}"/>.</param>
#if NET9_0_OR_GREATER
        [OverloadResolutionPriority(-1)]
#endif
        public void PushFrontRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items is ICollection<T> collection)
            {
                int count = collection.Count;
                if (count == 0)
                {
                    return;
                }

                int requiredCapacity = _count + count;

                if (requiredCapacity > Array.MaxLength)
                {
                    ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
                }
                else
                {
                    EnsureCapacityInternal(count, 0);
                    _head -= count;
                    collection.CopyTo(_items, _head);
                    _count += count;
                    _version++;
                }
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
        /// Adds the elements of the specified memory region to the front of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="items">The read-only span whose elements should be added to the front of the <see cref="DoubleEndedList{T}"/>.</param>
        public void PushFrontRange(ReadOnlySpan<T> items)
        {
            if (items.IsEmpty)
            {
                return;
            }

            int count = items.Length;

            if (_count + count > Array.MaxLength)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }
            else
            {
                EnsureCapacityInternal(count, 0);
                _head -= count;
                items.CopyTo(MemoryMarshal.CreateSpan(ref _items[_head], count)); // _items.AsSpan().Slice(_head, count);
                _count += count;
                _version++;
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
        /// Removes the first occurrence of the specified object from the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="DoubleEndedList{T}"/>.</param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            int index = IndexOf(item);

            if (index == -1)
            {
                return false;
            }
            else
            {
                _ = RemoveAtInternal(index);
                return true;
            }
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index of the <see cref="DoubleEndedList{T}"/>.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || _count <= index)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(index), ThrowHelpers.M_IndexOutOfRange);
            }
            RemoveAtInternal(index);
        }

        /// <summary>
        /// Removes a range of elements from the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is negative.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the <see cref="DoubleEndedList{T}"/>.</exception>
        public void RemoveRange(int index, int count)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(index);
            ThrowHelpers.ThrowIfArgumentIsNegative(count);

            if (checked(index + count) > _count)
            {
                ThrowHelpers.ThrowArgumentException("The number of elements to remove is greater than the available space from the specified index to the end of the list.", "[index, count]");
            }

            RemoveRangeInternal(index, index + count);
        }

        private void RemoveRangeInternal(int beginIndex, int endIndex)
        {
            var count = endIndex - beginIndex;
            ref var itemsRef = ref _items[0];

            if (beginIndex == 0)
            {
                if (endIndex == _count)
                {
                    Clear();
                    return;
                }
                else
                {
                    _head += count;
                    CollectionHelpers.ClearIfReferenceOrContainsReferences(
                        MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), count) // itemsSpan.Slice(_head, count);
                    );
                }
            }
            else if (endIndex == _count)
            {
                CollectionHelpers.ClearIfReferenceOrContainsReferences(
                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head + _count - count), count) // itemsSpan.Slice(_head + _count - count, count);
                );
            }
            else
            {
                if (beginIndex < _count - endIndex)
                {
                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), beginIndex) // itemsSpan.Slice(_head, beginIndex)
                        .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head + count), beginIndex)); // itemsSpan.Slice(_head + count, beginIndex)

                    CollectionHelpers.ClearIfReferenceOrContainsReferences(
                        MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head), count) // itemsSpan.Slice(_head, count);
                    );
                }
                else
                {
                    var movedItemsCount = _head + _count - endIndex;

                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, endIndex), movedItemsCount) // itemsSpan.Slice(endIndex, movedItemsCount)
                        .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, endIndex - count), movedItemsCount)); // itemsSpan.Slice(endIndex - count, movedItemsCount)

                    CollectionHelpers.ClearIfReferenceOrContainsReferences(
                        MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsRef, _head + _count - count), count) // itemsSpan.Slice(_head + _count - count, count);
                    );
                }
            }

            _count -= count;
            _version++;
        }

        private void ReserveRangeForInsertInternal(int index, int length)
        {
            Debug.Assert((uint)index < _count, $"Invalid parameter: {nameof(index)}");
            Debug.Assert(0 <= length, $"Invalid parameter: {nameof(length)}");

            if (length == 0)
            {
                return;
            }

            var count = _count;
            if (index == 0)
            {
                EnsureCapacity(length, 0);
                _head -= length;
            }
            else if (index == count)
            {
                EnsureCapacity(0, length);
                _count += length;
            }
            else if (index < (count >> 1))
            {
                EnsureCapacity(length, 0);

                var head = _head;
                var items = _items.AsSpan();
                var newHead = head - length;
                items.Slice(head, index).CopyTo(items.Slice(newHead, index));

                _head = newHead;
            }
            else
            {
                EnsureCapacity(0, length);

                var items = _items.AsSpan();
                var copyCnt = count - index;
                var copyStartIdx = _head + index;
                items.Slice(copyStartIdx, copyCnt).CopyTo(items.Slice(copyStartIdx + length, copyCnt));
            }

            _count = count + length;
        }

        /// <summary>
        /// Resizes the internal array to the specified size.
        /// </summary>
        /// <param name="capacity"></param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than the number of elements contained in the <see cref="DoubleEndedList{T}"/> or greater than <see cref="Array.MaxLength"/>.</exception>
        public void Resize(int capacity)
        {
            if (capacity == _items.Length)
            {
                return;
            }
            else if (capacity < _count)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(capacity), "The specified capacity is less than the number of the elements contained in the collection.");
            }
            ThrowHelpers.ThrowIfArgumentIsGreaterThanMaxArrayLength(capacity);
            ResizeInternal(capacity);
        }

        /// <summary>
        /// Resizes the internal array to have the specified margins at the beginning and end of the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <remarks>
        /// This ensures that the specified number of elements can be added at the beginning or the end of the <see cref="DoubleEndedList{T}"/> without resizing the internal data structure. 
        /// </remarks>
        /// <param name="frontMargin">The number of elements that can be added at the beginning of the <see cref="DoubleEndedList{T}"/> without resizing the internal data structure.</param>
        /// <param name="backMargin">The number of elements that can be added at the end of the <see cref="DoubleEndedList{T}"/> without resizing the internal data structure.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="frontMargin"/> or <paramref name="backMargin"/> is negative.</exception>
        /// <exception cref="ArgumentException">The total required capacity is greater than the maximum size of an array.</exception>
        internal void Resize(int frontMargin, int backMargin)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(frontMargin);
            ThrowHelpers.ThrowIfArgumentIsNegative(backMargin);

            var capacity = frontMargin + backMargin + _count;

            if (capacity == _items.Length)
            {
                Shift(frontMargin - _head);
                return;
            }

            if (capacity > Array.MaxLength)
            {
                ThrowHelpers.ThrowArgumentException("The total required capacity is greater than the maximum size of an array.");
            }

            ResizeInternal(capacity, frontMargin);
        }

        /// <summary>
        /// Shrink the internal data structure so that the <see cref="DoubleEndedList{T}"/> doesn't have any margin at the beginning or end of it.
        /// </summary>
        public void ShrinkToFit()
        {
            ResizeInternal(_count, 0);
        }

        /// <summary>
        /// Copies the elements of the <see cref="DoubleEndedList{T}"/> into a new array.
        /// </summary>
        /// <returns>An array containing copies of the elements of the <see cref="DoubleEndedList{T}"/>.</returns>
        public T[] ToArray()
        {
            return AsReadOnlySpan().ToArray();
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="DoubleEndedList{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter.
        /// The object is not removed from the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the beginning of the <see cref="DoubleEndedList{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the beginning of the <see cref="DoubleEndedList{T}"/>; <see langword="false"/> if the <see cref="DoubleEndedList{T}"/> is empty.</returns>
        public bool TryPeekFirst([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                item = _items[_head];
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the end of the <see cref="DoubleEndedList{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter.
        /// The object is not removed from the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the end of the <see cref="DoubleEndedList{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the end of the <see cref="DoubleEndedList{T}"/>; <see langword="false"/> if the <see cref="DoubleEndedList{T}"/> is empty.</returns>
        public bool TryPeekLast([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                item = _items[_head + _count - 1];
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the end of the <see cref="DoubleEndedList{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter and removes it from the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the end of the <see cref="DoubleEndedList{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the end of the <see cref="DoubleEndedList{T}"/>; <see langword="false"/> if the <see cref="DoubleEndedList{T}"/> is empty.</returns>
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
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="DoubleEndedList{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter and removes it from the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the beginning of the <see cref="DoubleEndedList{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the beginning of the <see cref="DoubleEndedList{T}"/>; <see langword="false"/> if the <see cref="DoubleEndedList{T}"/> is empty.</returns>
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
        /// <exception cref="Exception">The capacity of the <see cref="DoubleEndedList{T}"/> has reached its upper limit.</exception>
        private void Grow()
        {
            int newCapacity = Math.Clamp(_items.Length << 1, _defaultCapacity, Array.MaxLength);

            if (newCapacity < _count + 2)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            Grow(newCapacity);
        }

        /// <summary>
        /// Enlarge the internal array to the specified size.
        /// </summary>
        /// <remarks>
        /// This does NOT examine whether the <paramref name="capacity"/> is valid (between the number of elements and <see cref="Array.MaxLength"/>.)
        /// </remarks>
        private void Grow(int capacity)
        {
            int diff = BackMargin - FrontMargin;
            ResizeInternal(capacity, ((capacity - _count + diff) >> 1));
        }

        private T RemoveAtInternal(int index)
        {
            if (index == 0)
            {
                return PopFront();
            }
            else if (index == _count - 1)
            {
                return PopBack();
            }
            else
            {
                ref var headRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_items), _head);
                T result = Unsafe.Add(ref headRef, index);

                // make it costs lower to move the elements.
                if (index <= (_count >> 1))
                {
                    // shift the elements in [0,index) of the list.
                    MemoryMarshal.CreateReadOnlySpan(ref headRef, index)
                        .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref headRef, 1), index));

                    SetDefaultValueIfNeeded(ref headRef);
                    _head++;
                }
                else
                {
                    ref var beginRef = ref Unsafe.Add(ref headRef, index);
                    var rangeLength = _count - index - 1;

                    // shift the elements in (index, _count) of the list.
                    MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref beginRef, 1), rangeLength)
                        .CopyTo(MemoryMarshal.CreateSpan(ref beginRef, rangeLength));

                    SetDefaultValueIfNeeded(ref Unsafe.Add(ref headRef, _count - 1));
                }

                _count--;
                _version++;
                return result;
            }
        }

        /// <summary>
        /// Creates a new array as an internal array at the specified size, and copies to the array all the elements contained in the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <param name="capacity"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeInternal(int capacity)
        {
            int frontMargin = (capacity - _count) >> 1;
            ResizeInternal(capacity, frontMargin);
        }

        /// <summary>
        /// Creates a new array as an internal array at the specified size, and copies to the array starting at the specified index all the elements contained in the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <remarks>
        /// This method does NOT check if <paramref name="capacity"/> and <paramref name="frontMargin"/> are valid.
        /// </remarks>
        /// <param name="capacity"></param>
        /// <param name="frontMargin"></param>
        private void ResizeInternal(int capacity, int frontMargin)
        {
            if (capacity == 0)
            {
                _items = _s_emptyArray;
                _head = 0;
            }
            else
            {
                T[] newArray = new T[capacity];
                MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_items), _head), _count)
                    .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(newArray), frontMargin), _count));
                _items = newArray;
                _head = frontMargin;
            }

            _version++;
        }

        /// <summary>
        /// Shifts all the elements in the internal data structure at specified time(s).
        /// </summary>
        /// <remarks>
        /// This doesn't check whether <paramref name="diff"/> is valid or not.
        /// </remarks>
        /// <param name="diff"></param>
        private void Shift(int diff)
        {
            if (diff == 0)
            {
                return;
            }

            ref var itemsHeadRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_items), _head); // ref _items[_head]
            var src = MemoryMarshal.CreateSpan(ref itemsHeadRef, _count);
            var dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref itemsHeadRef, diff), _count);
            src.CopyTo(dst);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                if (diff > 0)
                {
                    MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(src), diff).Clear(); // src[..diff]
                }
                else
                {
                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(src), src.Length + diff), -diff).Clear(); // src[^(-diff)..]
                }
            }

            _head += diff;
            _version++;
        }

        /// <summary>
        /// If <typeparamref name="T"/> is reference or contains references, sets the default value of <typeparamref name="T"/> to the <paramref name="value"/> parameter; otherwise, do nothing.
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetDefaultValueIfNeeded(ref T value)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                value = default!;
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the <see cref="DoubleEndedList{T}"/> is empty.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="DoubleEndedList{T}"/> is empty.</exception>
        private void ThrowIfEmpty()
        {
            if (IsEmpty)
            {
                ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_CollectionIsEmpty);
            }
        }

        /// <summary>
        /// Enumerates the elements of a <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly DoubleEndedList<T> _collection;
            private int _index;
            private readonly int _tail;
            private readonly int _version;

            /// <summary>Gets the element in the <see cref="DoubleEndedList{T}"/> at the current position of the enumerator.</summary>
            /// <returns>The element in the <see cref="DoubleEndedList{T}"/> at the current position of the enumerator.</returns>
            public readonly T Current => _collection._items[_index];

            readonly object? IEnumerator.Current => Current;

            internal Enumerator(DoubleEndedList<T> collection)
            {
                _collection = collection;
                _index = collection._head - 1;
                _tail = collection._head + collection._count - 1;
                _version = collection._version;
            }

            /// <inheritdoc/>
            public readonly void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="DoubleEndedList{T}"/>.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the <see cref="DoubleEndedList{T}"/>.</returns>
            /// <exception cref="InvalidOperationException">The <see cref="DoubleEndedList{T}"/> was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                ThrowIfCollectionModified();
                return ++_index <= _tail;
            }

            /// <summary>Set the enumerator to its initial position, which is the before the first element in the <see cref="DoubleEndedList{T}"/>.</summary>
            /// <exception cref="InvalidOperationException">The <see cref="DoubleEndedList{T}"/> was modified after the enumerator was created.</exception>
            public void Reset()
            {
                ThrowIfCollectionModified();
                _index = _collection._head - 1;
            }

            /// <summary>
            /// Throw an exception if the <see cref="DoubleEndedList{T}"/> was modified after the enumerator was created.
            /// </summary>
            /// <exception cref="InvalidOperationException">The <see cref="DoubleEndedList{T}"/> was modified after the enumerator was created.</exception>
            private readonly void ThrowIfCollectionModified()
            {
                if (_version != _collection._version)
                {
                    ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_CollectionModifiedAfterEnumeratorCreated);
                }
            }
        }
    }
}
