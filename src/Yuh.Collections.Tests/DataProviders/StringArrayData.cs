using System.Collections;

namespace Yuh.Collections.Tests.DataProviders
{
    public sealed class StringArrayData() : IEnumerable<object[]>
    {
        public readonly static string[][] DataSource = [
            [],
            ["foo", "bar", "baz", "qax"],
            [.. Enumerable.Repeat("The quick brown fox jumps over the lazy dog\r\n", 1024)]
        ];

        public IEnumerator<object[]> GetEnumerator() => DataSource.Select<string[], object[]>(x => [x]).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
