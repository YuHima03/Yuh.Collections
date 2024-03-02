using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Yuh.Collections
{
    internal static class ThrowHelpers
    {
        internal const string M_CapacityReachedUpperLimit = "The capacity of the collection has reached its upper limit.";

        internal const string M_CollectionIsEmpty = "The collection is empty.";

        internal const string M_CollectionModifiedAfterEnumeratorCreated = "The collection was modified after the enumerator was created.";

        internal const string M_IndexOutOfRange = "The index must be positive or zero, and less than the number of the elements contained in the collection.";

        internal const string M_TypeOfValueNotSupported = "The type of the value is not supported.";

        internal const string M_ValueIsGreaterThanCount = "The value is greater than the number of elements contained in the collection.";

        internal const string M_ValueIsNegative = "The value is negative.";

        /// <exception cref="Exception"></exception>
        [DoesNotReturn]
        internal static void ThrowException(string? message = null, Exception? innerException = null)
        {
            throw new Exception(message, innerException);
        }

        /// <exception cref="ArgumentException"></exception>
        [DoesNotReturn]
        internal static void ThrowArgumentException(string? message = null, string? argName = null, Exception? innerException = null)
        {
            throw new ArgumentException(message, argName, innerException);
        }

        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(string? argName = null, string? message = null)
        {
            throw new ArgumentOutOfRangeException(argName, message);
        }

        /// <inheritdoc cref="ThrowArgumentOutOfRangeException(string?, string?)"/>
        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(string? argName = null, object? actualValue = null, string? message = null)
        {
            throw new ArgumentOutOfRangeException(argName, actualValue, message);
        }

        /// <exception cref="IndexOutOfRangeException"></exception>
        [DoesNotReturn]
        internal static void ThrowIndexOutOfRangeException(string? message = null, Exception? innerException = null)
        {
            throw new IndexOutOfRangeException(message, innerException);
        }

        /// <exception cref="InvalidOperationException"></exception>
        [DoesNotReturn]
        internal static void ThrowInvalidOperationException(string? message = null, Exception? innerException = null)
        {
            throw new InvalidOperationException(message, innerException);
        }

        /// <exception cref="NotSupportedException"></exception>
        [DoesNotReturn]
        internal static void ThrowNotSupportedException(string? message = null, Exception? innerException = null)
        {
            throw new NotSupportedException(message, innerException);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfArgumentIsGreaterThanMaxArrayLength(int num, [CallerArgumentExpression(nameof(num))] string? argName = null)
        {
            if (num > Array.MaxLength)
            {
                throw new ArgumentOutOfRangeException(argName, "The value is greater than the maximum number of elements that may be contained in an array.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfArgumentIsNotPowerOfTwo(int num, [CallerArgumentExpression(nameof(num))] string? paramName = null)
        {
            if (num <= 0 || (num & (num - 1)) != 0)
            {
                throw new ArgumentException("The number must be power of 2.", paramName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfArgumentIsNegative(int num, [CallerArgumentExpression(nameof(num))] string? paramName = null)
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(num, paramName);
#else
            if (num < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, M_ValueIsNegative);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfArgumentIsLessThan(int num, int other, [CallerArgumentExpression(nameof(num))] string? paramName = null)
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfLessThan(num, other, paramName);
#else
            if (num < other)
            {
                throw new ArgumentOutOfRangeException(paramName, $"The value is less than {other}");
            }
#endif
        }
    }
}
