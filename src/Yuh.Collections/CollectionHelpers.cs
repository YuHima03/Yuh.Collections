using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Yuh.Collections
{
    internal static class CollectionHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetDefaultValueIfReferenceOrContainsReferences<T>(ref T value)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                value = default!;
            }
        }
    }
}
