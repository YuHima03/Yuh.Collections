using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Yuh.Collections
{
    /// <summary>
    /// Represents a read-only, double-ended queue, for which elements can be added to or removed from the front or back.
    /// </summary>
    /// <typeparam name="T">The type of elements in the <see cref="IDeque{T}"/>.</typeparam>
    public interface IReadOnlyDeque<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
    {
        /// <summary>
        /// Returns the object at the beginning of the <see cref="IDeque{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="IDeque{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="IDeque{T}"/> is empty.</exception>
        public T PeekFirst();

        /// <summary>
        /// Returns the object at the end of the <see cref="IDeque{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the end of the <see cref="IDeque{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="IDeque{T}"/> is empty.</exception>
        public T PeekLast();

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="IDeque{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter.
        /// The object is not removed from the <see cref="IDeque{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the beginning of the <see cref="IDeque{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the beginning of the <see cref="IDeque{T}"/>; <see langword="false"/> if the <see cref="IDeque{T}"/> is empty.</returns>
        public bool TryPeekFirst([MaybeNullWhen(false)] out T item);

        /// <summary>
        /// Returns a value that indicates whether there is an object at the end of the <see cref="IDeque{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter.
        /// The object is not removed from the <see cref="IDeque{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the end of the <see cref="IDeque{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the end of the <see cref="IDeque{T}"/>; <see langword="false"/> if the <see cref="IDeque{T}"/> is empty.</returns>
        public bool TryPeekLast([MaybeNullWhen(false)] out T item);
    }

    /// <summary>
    /// Represents a double-ended queue, for which elements can be added to or removed from the front or back.
    /// </summary>
    /// <typeparam name="T">The type of elements in the <see cref="IDeque{T}"/>.</typeparam>
    public interface IDeque<T> : ICollection, IReadOnlyDeque<T>
    {
        /// <summary>
        /// Removes and returns the object at the end of the <see cref="IDeque{T}"/>.
        /// </summary>
        /// <returns>The object at the end of the <see cref="IDeque{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="IDeque{T}"/> is empty.</exception>
        public T PopBack();

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="IDeque{T}"/>.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="IDeque{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="IDeque{T}"/> is empty.</exception>
        public T PopFront();

        /// <summary>
        /// Adds an object to the end of the <see cref="IDeque{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the end of the <see cref="IDeque{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushBack(T item);

        /// <summary>
        /// Adds an object to the beginning of the <see cref="IDeque{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to add to the beginning of the <see cref="IDeque{T}"/>.
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public void PushFront(T item);

        /// <summary>
        /// Returns a value that indicates whether there is an object at the end of the <see cref="IDeque{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter and removes it from the <see cref="IDeque{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the end of the <see cref="IDeque{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the end of the <see cref="IDeque{T}"/>; <see langword="false"/> if the <see cref="IDeque{T}"/> is empty.</returns>
        public bool TryPopBack([MaybeNullWhen(false)] out T item);

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="IDeque{T}"/>, and if one is present, copies it to the <paramref name="item"/> parameter and removes it from the <see cref="IDeque{T}"/>.
        /// </summary>
        /// <param name="item">If present, the object at the beginning of the <see cref="IDeque{T}"/>; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if there is an object at the beginning of the <see cref="IDeque{T}"/>; <see langword="false"/> if the <see cref="IDeque{T}"/> is empty.</returns>
        public bool TryPopFront([MaybeNullWhen(false)] out T item);
    }
}
