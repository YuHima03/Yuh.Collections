using System.Buffers;
using System.Runtime.CompilerServices;

namespace Yuh.Collections.Tests.Helpers
{
    public class LessAllocArray<T> : IDisposable
    {
        private const int MaxArraySizeFromArrayPool = 1 << 27;

        private T[]? _array;

        private readonly bool _isFromArrayPool;

        public T[] Array
        {
            get
            {
#if NET7_0_OR_GREATER
                ObjectDisposedException.ThrowIf(_array is null, this);
#else
                if (_array is null)
                {
                    throw new ObjectDisposedException(nameof(LessAllocArray<T>));
                }
#endif
                return _array;
            }
        }

        public LessAllocArray(int length)
        {
            if (_isFromArrayPool = Unsafe.SizeOf<T>() * length <= MaxArraySizeFromArrayPool)
            {
                _array = ArrayPool<T>.Shared.Rent(length);
            }
            else
            {
                _array = new T[length];
            }
        }

        public void Dispose()
        {
            if (_array is not null)
            {
                if (_isFromArrayPool)
                {
                    ArrayPool<T>.Shared.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                    _array = null;
                }
                else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    System.Array.Clear(Array);
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
