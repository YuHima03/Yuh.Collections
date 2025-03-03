using System.Collections;

namespace Yuh.Collections.Tests.DataProviders
{
    public sealed class StringArrayData() : IEnumerable<object[]>
    {
        public readonly static string[][] DataSource = [
            [],
            ["foo", "bar", "baz", "qax"],
            [.. Enumerable.Repeat(string.Intern("The quick brown fox jumps over the lazy dog\r\n"), 1024)]
        ];

        public readonly static IEnumerable<object[]> ObjectDataSource = DataSource.Select<string[], object[]>(x => [x]);

        public IEnumerator<object[]> GetEnumerator() => ObjectDataSource.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
