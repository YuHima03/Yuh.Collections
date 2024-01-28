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
        internal static void ThrowIfNotPowerOfTwo(int num, [CallerArgumentExpression(nameof(num))] string? paramName = null)
        {
            if (num <= 0 || (num & (num - 1)) != 0)
            {
                throw new ArgumentException($"`{paramName}` must be power of 2.", paramName);
            }
        }
    }
}
