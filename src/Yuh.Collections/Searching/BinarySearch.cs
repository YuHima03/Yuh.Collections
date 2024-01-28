using System.Numerics;
using System.Runtime.CompilerServices;

namespace Yuh.Collections.Searching
{
    /// <summary>
    /// Provides methods that search for an element in a collection, using binary-search. 
    /// </summary>
    public static class BinarySearch
    {
        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in ascending order.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstIndexOf<T>(ReadOnlySpan<T> span, T value) where T : IComparisonOperators<T, T, bool>, IEqualityOperators<T, T, bool>
        {
            return FirstIndexOf<T, T>(span, value);
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <typeparam name="U">The type of the value to compare the elements of the collection to.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in ascending order.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        public static int FirstIndexOf<T, U>(ReadOnlySpan<T> span, U value) where T : IComparisonOperators<T, U, bool>, IEqualityOperators<T, U, bool>
        {
            if (span.Length == 0)
            {
                return -1;
            }

            int begin = 0;
            int end = span.Length - 1;

            while (begin < end)
            {
                int idx = (begin + end) >> 1; // Math.Floor((begin + end) / 2);

                if (span[idx] < value)
                {
                    begin = idx + 1;
                }
                else
                {
                    end = idx;
                }
            }

            return span[begin] == value ? begin : -1;
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in the order defined by <paramref name="comparer"/>.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <param name="comparer">The comparer that compares two <typeparamref name="T"/> objects.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FirstIndexOf<T>(ReadOnlySpan<T> span, T value, IComparer<T> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);
            return FirstIndexOf<T, T>(span, value, comparer.Compare);
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <typeparam name="U">The type of the value to compare the elements of the collection to.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in the order defined by <paramref name="comparer"/>.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <param name="comparer">The comparer that compares <typeparamref name="T"/> and <typeparamref name="U"/> objects.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        public static int FirstIndexOf<T, U>(ReadOnlySpan<T> span, U value, Func<T, U, int> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);

            if (span.Length == 0)
            {
                return -1;
            }

            int begin = 0;
            int end = span.Length - 1;

            while (begin < end)
            {
                int idx = (begin + end) >> 1; // Math.Floor((begin + end) / 2);

                if (comparer(span[idx], value) < 0) // span[idx] < value
                {
                    begin = idx + 1;
                }
                else
                {
                    end = idx;
                }
            }

            return (comparer(span[begin], value) == 0) ? begin : -1;
        }

        /// <summary>
        /// Returns the zero-based index of the last element in the specified collection that equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in ascending order.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <returns>The zero-based index of the last element in <paramref name="span"/> that is equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(ReadOnlySpan<T> span, T value) where T : IComparisonOperators<T, T, bool>, IEqualityOperators<T, T, bool>
        {
            return LastIndexOf<T, T>(span, value);
        }

        /// <summary>
        /// Returns the zero-based index of the last element in the specified collection that equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <typeparam name="U">The type of the value to compare the elements of the collection to.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in ascending order.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <returns>The zero-based index of the last element in <paramref name="span"/> that is equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        public static int LastIndexOf<T, U>(ReadOnlySpan<T> span, U value) where T : IComparisonOperators<T, U, bool>, IEqualityOperators<T, U, bool>
        {
            if (span.Length == 0)
            {
                return -1;
            }

            int begin = 0;
            int end = span.Length - 1;

            while (begin < end)
            {
                int idx = (begin + end + 1) >> 1; // Math.Ceil((begin + end) / 2);

                if (span[idx] > value)
                {
                    end = idx - 1;
                }
                else
                {
                    begin = idx;
                }
            }

            return span[begin] == value ? begin : -1;
        }

        /// <summary>
        /// Returns the zero-based index of the last element in the specified collection that equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in the order defined by <paramref name="comparer"/>.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <param name="comparer">The comparer that compares two <typeparamref name="T"/> objects.</param>
        /// <returns>The zero-based index of the last element in <paramref name="span"/> that is equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(ReadOnlySpan<T> span, T value, IComparer<T> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);
            return LastIndexOf<T, T>(span, value, comparer.Compare);
        }

        /// <summary>
        /// Returns the zero-based index of the last element in the specified collection that equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <typeparam name="U">The type of the value to compare the elements of the collection to.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in the order defined by <paramref name="comparer"/>.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <param name="comparer">The comparer that compares <typeparamref name="T"/> and <typeparamref name="U"/> objects.</param>
        /// <returns>The zero-based index of the last element in <paramref name="span"/> that is equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        public static int LastIndexOf<T, U>(ReadOnlySpan<T> span, U value, Func<T, U, int> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);

            if (span.Length == 0)
            {
                return -1;
            }

            int begin = 0;
            int end = span.Length - 1;

            while (begin < end)
            {
                int idx = (begin + end + 1) >> 1; // Math.Ceil((begin + end) / 2);

                if (comparer(span[idx], value) > 0) // span[idx] > value
                {
                    end = idx - 1;
                }
                else
                {
                    begin = idx;
                }
            }

            return (comparer(span[begin], value) == 0) ? begin : -1;
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that is greater than or equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in ascending order.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is greater than or equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LowerBound<T>(ReadOnlySpan<T> span, T value) where T : IComparisonOperators<T, T, bool>, IEqualityOperators<T, T, bool>
        {
            return LowerBound<T, T>(span, value);
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that is greater than or equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <typeparam name="U">The type of the value to compare the elements of the collection to.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in ascending order.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is greater than or equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        public static int LowerBound<T, U>(ReadOnlySpan<T> span, U value) where T : IComparisonOperators<T, U, bool>, IEqualityOperators<T, U, bool>
        {
            if (span.Length == 0)
            {
                return -1;
            }

            int begin = 0;
            int end = span.Length - 1;

            while (begin < end)
            {
                int idx = begin + end >> 1; // Math.Floor((begin + end) / 2);

                if (span[idx] < value)
                {
                    begin = idx + 1;
                }
                else
                {
                    end = idx;
                }
            }

            return span[begin] >= value ? begin : -1;
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that is greater than or equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in the order defined by <paramref name="comparer"/>.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <param name="comparer">The comparer that compares two <typeparamref name="T"/> objects.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is greater than or equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LowerBound<T>(ReadOnlySpan<T> span, T value, IComparer<T> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);
            return LowerBound<T, T>(span, value, comparer.Compare);
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that is greater than or equal to the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <typeparam name="U">The type of the value to compare the elements of the collection to.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in the order defined by <paramref name="comparer"/>.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <param name="comparer">The comparer that compares <typeparamref name="T"/> and <typeparamref name="U"/> objects.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is greater than or equal to <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        public static int LowerBound<T, U>(ReadOnlySpan<T> span, U value, Func<T, U, int> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);

            if (span.Length == 0)
            {
                return -1;
            }

            int begin = 0;
            int end = span.Length - 1; // Math.Floor((begin + end) / 2);

            while (begin < end)
            {
                int idx = begin + end >> 1;
                int cmp = comparer(span[idx], value);

                if (cmp < 0) // span[idx] < value
                {
                    begin = idx + 1;
                }
                else // span[idx] >= value
                {
                    end = idx;
                }
            }

            return (comparer(span[begin], value) >= 0) ? begin : -1;
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that is greater than the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in ascending order.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is greater than <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UpperBound<T>(ReadOnlySpan<T> span, T value) where T : IComparisonOperators<T, T, bool>, IEqualityOperators<T, T, bool>
        {
            return UpperBound<T, T>(span, value);
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that is greater than the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <typeparam name="U">The type of the value to compare the elements of the collection to.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in ascending order.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is greater than <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        public static int UpperBound<T, U>(ReadOnlySpan<T> span, U value) where T : IComparisonOperators<T, U, bool>, IEqualityOperators<T, U, bool>
        {
            if (span.Length == 0)
            {
                return -1;
            }

            int begin = 0;
            int end = span.Length - 1;

            while (begin < end)
            {
                int idx = (begin + end) >> 1; // Math.Floor((begin + end) / 2);

                if (span[idx] > value)
                {
                    end = idx;
                }
                else
                {
                    begin = idx + 1;
                }
            }

            return span[begin] > value ? begin : -1;
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that is greater than the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in the order defined by <paramref name="comparer"/>.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <param name="comparer">The comparer that compares two <typeparamref name="T"/> objects.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is greater than <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UpperBound<T>(ReadOnlySpan<T> span, T value, IComparer<T> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);
            return UpperBound<T, T>(span, value, comparer.Compare);
        }

        /// <summary>
        /// Returns the zero-based index of the first element in the specified collection that is greater than the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <typeparam name="U">The type of the value to compare the elements of the collection to.</typeparam>
        /// <param name="span">The read-only span over the collection whose elements are sorted in the order defined by <paramref name="comparer"/>.</param>
        /// <param name="value">The value to compare the elements to.</param>
        /// <param name="comparer">The comparer that compares <typeparamref name="T"/> and <typeparamref name="U"/> objects.</param>
        /// <returns>The zero-based index of the first element in <paramref name="span"/> that is greater than <paramref name="value"/>, if found; otherwise, <c>-1</c>.</returns>
        public static int UpperBound<T, U>(ReadOnlySpan<T> span, U value, Func<T, U, int> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);

            if (span.Length == 0)
            {
                return -1;
            }

            int begin = 0;
            int end = span.Length - 1;

            while (begin < end)
            {
                int idx = (begin + end) >> 1; // Math.Floor((begin + end) / 2);

                if (comparer(span[idx], value) > 0) // span[idx] > value
                {
                    end = idx;
                }
                else
                {
                    begin = idx + 1;
                }
            }

            return (comparer(span[begin], value) > 0) ? begin : -1;
        }
    }
}
