﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    /// <summary>
    /// Provides methods to create a new instance of the <see cref="CircularBuffer{T}"/> class or resize the collection.
    /// </summary>
    public static class CircularBuffer
    {
        /// <summary>
        /// Create a new <see cref="CircularBuffer{T}"/> that has specified capacity and has elements copied from the <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="source">The <see cref="CircularBuffer{T}"/> whose elements are copied to a new one.s</param>
        /// <param name="capacity">
        ///     The capacity of the new collection.
        ///     The value must be power of 2.
        /// </param>
        /// <returns>A new instance of the <see cref="CircularBuffer{T}"/> class that has specified capacity and has elements copied from the <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="capacity"/> is not power of 2 or greater than the maximum length of an array.</exception>
        public static CircularBuffer<T> Resize<T>(CircularBuffer<T> source, int capacity)
        {
            ArgumentNullException.ThrowIfNull(source);
            ThrowHelpers.ThrowIfArgumentIsNotPowerOfTwo(capacity);
            ThrowHelpers.ThrowIfArgumentIsGreaterThanMaxArrayLength(capacity);

            var buffer = new T[capacity];
            source.CopyTo(buffer.AsSpan());
            return new(buffer, 0, source.Count);
        }

        /// <summary>
        /// Creates a new <see cref="CircularBuffer{T}"/> that has doubled capacity of the <paramref name="source"/> and has elements copied from it.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="source">The <see cref="CircularBuffer{T}"/> whose elements are copied to a new one.</param>
        /// <returns>A new instance of the <see cref="CircularBuffer{T}"/> class that has doubled capacity of the <paramref name="source"/> and has elements copied from it.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="Exception">The capacity of the new collection is greater than the maximum length of an array.</exception>
        public static CircularBuffer<T> ResizeDouble<T>(CircularBuffer<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            int capacity = checked(source.Capacity << 1);
            if (capacity > Array.MaxLength)
            {
                ThrowHelpers.ThrowException(ThrowHelpers.M_CapacityReachedUpperLimit);
            }

            var buffer = new T[capacity];
            source.CopyTo(buffer.AsSpan());
            return new(buffer, 0, source.Count);
        }
    }

    /// <summary>
    /// Represents a buffer that supports addition of elements to the front or back, or removal from the front or back.
    /// </summary>
    /// <remarks>
    /// This can be substituted for <see cref="DoubleEndedList{T}"/> or <see cref="Deque{T}"/>, but this may perform better than these.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public class CircularBuffer<T> : ICollection, ICollection<T>, IEnumerable, IEnumerable<T>, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        private const int _defaultCapacity = 8;

        private readonly T[] _buffer;
        private readonly int _capacity;
        private int _count = 0;
        private int _head = 0; // 0 <= _head < _capacity
        private readonly int _mask = 0;
        private int _version = 0;

        /// <summary>
        /// Gets of sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <value>The element at the specified index.</value>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularBuffer{T}"/>.</exception>
        public T this[int index]
        {
            get
            {
                if ((uint)index < (uint)_count)
                {
                    return _buffer[(_head + index) & _mask];
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
                    _buffer[(_head + index) & _mask] = value;
                    _version++;
                }
                else
                {
                    ThrowHelpers.ThrowIndexOutOfRangeException(ThrowHelpers.M_IndexOutOfRange);
                }
            }
        }

        /// <summary>
        /// Gets the number of elements the internal data structure can hold without resizing.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Gets the number of the elements contained in the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <returns>The number of the elements contained in the <see cref="CircularBuffer{T}"/>.</returns>
        public int Count => _count;

        /// <summary>
        /// Gets the value that indicates whether the <see cref="CircularBuffer{T}"/> is empty.
        /// </summary>
        /// <returns><see langword="true"/> if the <see cref="CircularBuffer{T}"/> is empty; <see langword="false"/> if not.</returns>
        public bool IsEmpty => _count == 0;

        private ref T FirstRef => ref _buffer[_head];

        private ref T LastRef => ref _buffer[(_head + _count - 1) & _mask];

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
        /// </summary>
        public CircularBuffer() : this(new T[_defaultCapacity]) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class that is empty and has the specified capacity.
        /// </summary>
        /// <param name="capacity">
        ///     The number of elements that may be contained in the <see cref="CircularBuffer{T}"/> without resizing.
        ///     This value must be power of 2.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> must be greater than zero, and less than <see cref="Array.MaxLength"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="capacity"/> must be power or 2.</exception>
        public CircularBuffer(int capacity)
        {
            if (capacity > Array.MaxLength)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(capacity), "`capacity` must be less than the maximum number of elements that may be contained in an array.");
            }
            ThrowHelpers.ThrowIfArgumentIsNotPowerOfTwo(capacity);

            _buffer = new T[capacity];
            _capacity = capacity;
            _mask = capacity - 1;
        }

        internal CircularBuffer(T[] buffer, int head = 0, int count = 0)
        {
            _buffer = buffer;
            _capacity = buffer.Length;
            _count = count;
            _head = head;
            _mask = buffer.Length - 1;
        }

        void ICollection<T>.Add(T item)
        {
            PushBack(item);
        }

        /// <summary>
        /// Removes all of the elements from the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                if (_head + _count > _capacity)
                {
                    ref var bufferRef = ref MemoryMarshal.GetReference(_buffer.AsSpan());
                    MemoryMarshal.CreateSpan(ref Unsafe.Add(ref bufferRef, _head), _capacity - _head).Clear();
                    MemoryMarshal.CreateSpan(ref bufferRef, (_head + _count) & _mask).Clear();
                }
                else
                {
                    MemoryMarshal.CreateSpan(ref _buffer[_head], _count).Clear();
                }
            }
            _head = 0;
            _count = 0;
            _version++;
        }

        /// <summary>
        /// Determines whether the <see cref="CircularBuffer{T}"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="CircularBuffer{T}"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> is found in the <see cref="CircularBuffer{T}"/>; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T item)
        {
            if (_head + _count > _capacity)
            {
                return (Array.IndexOf(_buffer, item, _head, _capacity - _head) != -1) || (Array.IndexOf(_buffer, item, 0, (_head + _count) & _mask) != -1);
            }
            else
            {
                return Array.IndexOf(_buffer, item, 0, _count) != -1;
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="CircularBuffer{T}"/> to a span.
        /// </summary>
        /// <param name="destination">The span that the elements are copied from the <see cref="CircularBuffer{T}"/> to.</param>
        public void CopyTo(Span<T> destination)
        {
            if (_count == 0)
            {
                return;
            }

            if (destination.Length < _count)
            {
                ThrowHelpers.ThrowArgumentException(nameof(destination), "The length of the specified span is less than the number of elements contained in the collection.");
            }

            CopyToInternal(destination);
        }

        /// <summary>
        /// Copies the elements of the <see cref="CircularBuffer{T}"/> to an <typeparamref name="T"/>[], starting at a particular index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="CircularBuffer{T}"/>.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The number of the elements in the source <see cref="CircularBuffer{T}"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_count == 0)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(array);
            ThrowHelpers.ThrowIfArgumentIsNegative(arrayIndex);
            if (array.Length - arrayIndex < _count)
            {
                ThrowHelpers.ThrowArgumentException("The number of the elements in the source buffer is greater than the available space from the specified index to the end of the destination array.");
            }

            var arraySpan = MemoryMarshal.CreateSpan(ref array[0], array.Length); // It is ensured that array.Length is greater than 0.
            CopyToInternal(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(arraySpan), (nint)(uint)arrayIndex), arraySpan.Length - arrayIndex));
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ThrowHelpers.ThrowIfArgumentIsNegative(index);
            if (array.Length - index < _count)
            {
                ThrowHelpers.ThrowArgumentException("The number of the elements in the source buffer is greater than the available space from the specified index to the end of the destination array.");
            }

            if (_head + _count > _capacity)
            {
                Array.Copy(_buffer, _head, array, index, _capacity - _head);
                Array.Copy(_buffer, 0, array, index + _capacity - _head, (_head + _count) & _mask);
            }
            else
            {
                Array.Copy(_buffer, _head, array, index, _count);
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="CircularBuffer{T}"/> to a span.
        /// </summary>
        /// <remarks>
        /// It is not checked that the number of elements contained in the <see cref="CircularBuffer{T}"/> is greater than 0.
        /// </remarks>
        /// <param name="destination"></param>
        private void CopyToInternal(Span<T> destination)
        {
            var bufferSpan = MemoryMarshal.CreateSpan(ref _buffer[0], _capacity);
            var _end = checked(_head + _count);

            if (_end > _capacity)
            {
                var len1 = _capacity - _head;

                // same as:
                // bufferSpan[_head..].CopyTo(destination);
                MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(bufferSpan), _head), len1)
                    .CopyTo(destination);

                // same as:
                // bufferSpan[..(_end - _capacity)].CopyTo(destination[(_capacity - _head)..])
                MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(bufferSpan), _end - _capacity)
                    .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(bufferSpan), len1), bufferSpan.Length - len1));
            }
            else
            {
                // same as:
                // bufferSpan.Slice(_head, _count).CopyTo(destination);
                MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(bufferSpan), (nint)(uint)_head), _count)
                    .CopyTo(destination);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="CircularBuffer{T}"/>.</returns>
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

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the entire <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="CircularBuffer{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        /// <returns>The zero-base index of the first occurrence of <paramref name="item"/> within the entire <see cref="CircularBuffer{T}"/>, if found; otherwise -1.</returns>
        public int IndexOf(T item)
        {
            if (_head + _count > _capacity)
            {
                int idx = Array.IndexOf(_buffer, item, _head, _capacity - _head);

                if (idx >= 0)
                {
                    return idx - _head;
                }
                else
                {
                    idx = Array.IndexOf(_buffer, item, 0, (_head + _capacity) & _mask);
                    return (idx >= 0) ? (_capacity - _head + idx) : -1;
                }
            }
            else
            {
                return Array.IndexOf(_buffer, item, _head, _count) - _head;
            }
        }

        void IList<T>.Insert(int index, T item)
        {
            if (index == 0)
            {
                PushFront(item);
            }
            else if (index == _count)
            {
                PushBack(item);
            }
            else if (index < 0 || _count < index)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(index), ThrowHelpers.M_IndexOutOfRange);
            }
            else
            {
                ThrowHelpers.ThrowNotSupportedException("The insertion of an object is not supported except on the beginning or end.");
            }
        }

        /// <summary>
        /// Returns the object at the beginning of the <see cref="CircularBuffer{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="CircularBuffer{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CircularBuffer{T}"/> is empty.</exception>
        public T PeekFirst()
        {
            if (TryPeekFirst(out var item))
            {
                return item;
            }
            else
            {
                ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_CollectionIsEmpty);
                return default;
            }
        }

        /// <summary>
        /// Returns the object at the end of the <see cref="CircularBuffer{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the end of the <see cref="CircularBuffer{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CircularBuffer{T}"/> is empty.</exception>
        public T PeekLast()
        {
            if (TryPeekLast(out var item))
            {
                return item;
            }
            else
            {
                ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_CollectionIsEmpty);
                return default;
            }
        }

        /// <summary>
        /// Removes and returns the object at the end of the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <returns>The object at the end of the <see cref="CircularBuffer{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CircularBuffer{T}"/> is empty.</exception>
        public T PopBack()
        {
            if (TryPopBack(out var item))
            {
                return item;
            }
            else
            {
                ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_CollectionIsEmpty);
                return default;
            }
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="CircularBuffer{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CircularBuffer{T}"/> is empty.</exception>
        public T PopFront()
        {
            if (TryPopFront(out var item))
            {
                return item;
            }
            else
            {
                ThrowHelpers.ThrowInvalidOperationException(ThrowHelpers.M_CollectionIsEmpty);
                return default;
            }
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the end of the <see cref="CircularBuffer{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushBack(T item)
        {
            if (_count == _capacity)
            {
                _buffer[_head] = item;
                _head = (_head + 1) & _mask;
            }
            else
            {
                _buffer[(_head + _count) & _mask] = item;
                _count++;
            }

            _version++;
        }

        /// <summary>
        /// Adds an object to the beginning of the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the beginning of the <see cref="CircularBuffer{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushFront(T item)
        {
            if (_count != _capacity)
            {
                _count++;
            }

            _head = (_head + _capacity - 1) & _mask;
            _buffer[_head] = item;
            _version++;
        }

        bool ICollection<T>.Remove(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            if (_count == 0)
            {
                return false;
            }
            else if (comparer.Equals(item, PeekFirst()))
            {
                PopFront();
                return true;
            }
            else if (comparer.Equals(item, PeekLast()))
            {
                PopBack();
                return true;
            }
            else
            {
                return false;
            }
        }

        void IList<T>.RemoveAt(int index)
        {
            if (index == 0)
            {
                PopFront();
            }
            else if (index == _count - 1)
            {
                PopBack();
            }
            else if (index < 0 || _count <= index)
            {
                ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(index), ThrowHelpers.M_IndexOutOfRange);
            }
            else
            {
                ThrowHelpers.ThrowNotSupportedException("The removal of an element at any position other than the front of back is not supported.");
            }
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="CircularBuffer{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter.
        /// The object is not removed from the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the beginning of the <see cref="CircularBuffer{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the beginning of the <see cref="CircularBuffer{T}"/>; <see langword="false"/> if the <see cref="CircularBuffer{T}"/> is empty.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeekFirst([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                item = _buffer[(_head + _count - 1) & _mask];
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the end of the <see cref="CircularBuffer{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter.
        /// The object is not removed from the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the end of the <see cref="CircularBuffer{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the end of the <see cref="CircularBuffer{T}"/>; <see langword="false"/> if the <see cref="CircularBuffer{T}"/> is empty.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeekLast([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                item = _buffer[_head];
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the end of the <see cref="CircularBuffer{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter and removes it from the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the end of the <see cref="CircularBuffer{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the end of the <see cref="CircularBuffer{T}"/>; <see langword="false"/> if the <see cref="CircularBuffer{T}"/> is empty.</returns>
        public bool TryPopBack([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                ref var itemRef = ref LastRef;

                item = itemRef;
                SetDefaultValueIfNeeded(ref itemRef);

                _count--;
                _version++;
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="CircularBuffer{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter and removes it from the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the beginning of the <see cref="CircularBuffer{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the beginning of the <see cref="CircularBuffer{T}"/>; <see langword="false"/> if the <see cref="CircularBuffer{T}"/> is empty.</returns>
        public bool TryPopFront([MaybeNullWhen(false)] out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }
            else
            {
                ref var itemRef = ref FirstRef;

                item = itemRef;
                SetDefaultValueIfNeeded(ref itemRef);

                _head = (_head + 1) & _mask;
                _count--;
                _version++;
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether an object can be added to the <see cref="CircularBuffer{T}"/> without overwriting the element at the beginning, and if it can, add the object to the end.
        /// </summary>
        /// <param name="item">The object to add to the end of the <see cref="CircularBuffer{T}"/>.</param>
        /// <returns><see langword="true"/> if the object is successfully added; <see langword="false"/> if the <see cref="CircularBuffer{T}"/> is full.</returns>
        public bool TryPushBackWithoutOverwriting(T item)
        {
            if (_count == _capacity)
            {
                return false;
            }
            else
            {
                _buffer[(_head + _count) & _mask] = item;
                _count++;
                _version++;
                return true;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether an object can be added to the <see cref="CircularBuffer{T}"/> without overwriting the element at the end, and if it can, add the object to the beginning.
        /// </summary>
        /// <param name="item">The object to add to the beginning of the <see cref="CircularBuffer{T}"/>.</param>
        /// <returns><see langword="true"/> if the object is successfully added; <see langword="false"/> if the <see cref="CircularBuffer{T}"/> is full.</returns>
        public bool TryPushFrontWithoutOverwriting(T item)
        {
            if (_count == _capacity)
            {
                return false;
            }
            else
            {
                _head = (_head + _capacity - 1) & _mask;
                _buffer[_head] = item;
                _count++;
                _version++;
                return true;
            }
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
        /// Represents an enumerator that iterates through the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator, IEnumerator<T>
        {
            private readonly CircularBuffer<T> _collection;
            private int _index = -1;
            private readonly int _version;

            /// <summary>Gets the element in the <see cref="CircularBuffer{T}"/> at the current position of the enumerator.</summary>
            /// <returns>The element in the <see cref="CircularBuffer{T}"/> at the current position of the enumerator.</returns>
            public readonly T Current => _collection._buffer[(_collection._head + _index) & _collection._mask];

            readonly object? IEnumerator.Current => Current;

            internal Enumerator(CircularBuffer<T> buffer)
            {
                _collection = buffer;
                _version = buffer._version;
            }

            /// <inheritdoc/>
            public readonly void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="CircularBuffer{T}"/>.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the <see cref="CircularBuffer{T}"/>.</returns>
            /// <exception cref="InvalidOperationException">The <see cref="CircularBuffer{T}"/> was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                ThrowIfCollectionModified();
                return ++_index < _collection._count;
            }

            /// <summary>Set the enumerator to its initial position, which is the before the first element in the <see cref="CircularBuffer{T}"/>.</summary>
            /// <exception cref="InvalidOperationException">The <see cref="CircularBuffer{T}"/> was modified after the enumerator was created.</exception>
            public void Reset()
            {
                ThrowIfCollectionModified();
                _index = -1;
            }

            /// <summary>
            /// Throw an exception if the <see cref="DoubleEndedList{T}"/> was modified after the enumerator was created.
            /// </summary>
            /// <exception cref="InvalidOperationException">The <see cref="CircularBuffer{T}"/> was modified after the enumerator was created.</exception>
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
