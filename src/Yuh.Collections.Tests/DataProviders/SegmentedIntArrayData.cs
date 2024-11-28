using System.Collections;

namespace Yuh.Collections.Tests.DataProviders
{
    public sealed class SegmentedIntArrayData() : IEnumerable<object[]>
    {
        public static readonly int[][][] DataSource = [
            [.. Enumerable.Repeat<int[]>([], 256)],
            [.. Enumerable.Range(0, 1024).Select<int, int[]>(i => [i])],
            [.. Enumerable.Repeat(Enumerable.Range(0, 128).ToArray(), 512)]
        ];

        public IEnumerator<object[]> GetEnumerator() => DataSource.Select<int[][], object[]>(r => [r]).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
