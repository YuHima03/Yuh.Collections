using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yuh.Collections
{
    public ref struct SpanSequence<T>()
#if NET9_0_OR_GREATER
        : IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
#endif
    {
        private readonly SegmentGetter? _segmentGetter = null;

        private readonly Span<int> _countBefore = [];

        public readonly int Length = 0;

        public ref T this[int index]
        {
            get
            {
                if (_segmentGetter is null || (uint)index >= Length)
                {
                    ThrowHelpers.ThrowIndexOutOfRangeException();
                }
                var (si, ii) = GetLocation(index);
                Debug.Assert((uint)ii < _segmentGetter.Invoke(si).Length, "The index `ii` is out of range.");
                return ref Unsafe.Add(ref MemoryMarshal.GetReference(_segmentGetter.Invoke(si)), ii);
            }
        }

        private readonly (int SegmentIndex, int ItemIndex) GetLocation(int index)
        {
            throw new NotImplementedException();
        }

        public delegate Span<T> SegmentGetter(int index);
    }
}
