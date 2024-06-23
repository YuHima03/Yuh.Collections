namespace Yuh.Collections
{
    /// <summary>
    /// Provides sets of methods to access the internal data structure of collections.
    /// </summary>
    public static class CollectionsMarshal
    {
        /// <summary>
        /// Gets a new span over the <see cref="DoubleEndedList{T}"/>.
        /// </summary>
        /// <remarks>
        /// Items should not be added or removed from the <see cref="DoubleEndedList{T}"/> while the returned <see cref="Span{T}"/> is in use.
        /// </remarks>
        /// <param name="deque"><see cref="DoubleEndedList{T}"/> from which to create a <see cref="Span{T}"/>.</param>
        /// <typeparam name="T">The type of items in the <see cref="DoubleEndedList{T}"/>.</typeparam>
        /// <returns>The span representation of the <see cref="DoubleEndedList{T}"/>.</returns>
        public static Span<T> AsSpan<T>(DoubleEndedList<T> deque)
        {
            return deque.AsSpan();
        }
    }
}
