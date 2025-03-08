using System.Collections;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests.DataProviders
{
    public class IntArrayData : IEnumerable<object[]>
    {
        public readonly static int[][] DataSource = [.. SegmentedIntArrayData.DataSource.Select(arrays => arrays.Flatten().ToArray())];

        public readonly static IEnumerable<object[]> ObjectDataSource = DataSource.Select<int[], object[]>(x => [x]);

        public IEnumerator<object[]> GetEnumerator() => ObjectDataSource.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
