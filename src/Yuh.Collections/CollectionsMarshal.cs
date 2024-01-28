using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yuh.Collections
{
    /// <summary>
    /// Provides sets of methods to access the internal data structure of collections.
    /// </summary>
    public static class CollectionsMarshal
    {
        /// <summary>
        /// Gets a new span over the <see cref="Deque{T}"/>.
        /// </summary>
        /// <remarks>
        /// Items should not be added or removed from the <see cref="Deque{T}"/> while the returned <see cref="Span{T}"/> is in use.
        /// </remarks>
        /// <param name="deque"><see cref="Deque{T}"/> from which to create a <see cref="Span{T}"/>.</param>
        /// <typeparam name="T">The type of items in the <see cref="Deque{T}"/>.</typeparam>
        /// <returns>The span representation of the <see cref="Deque{T}"/>.</returns>
        public static Span<T> AsSpan<T>(Deque<T> deque)
        {
            return deque.AsSpan();
        }
    }
}
