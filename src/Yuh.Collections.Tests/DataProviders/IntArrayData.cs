using System.Collections;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests.DataProviders
{
    public class IntArrayData : IEnumerable<object[]>
    {
        public readonly static int[][] DataSource = [.. SegmentedIntArrayData.DataSource.Select(arrays => arrays.Flatten().ToArray())];

        public IEnumerator<object[]> GetEnumerator() => DataSource.Select<int[], object[]>(x => [x]).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
