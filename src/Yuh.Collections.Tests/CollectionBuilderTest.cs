using System.Diagnostics;
using System.Text;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests
{
    public class CollectionBuilderTest()
    {
        public static TheoryData<int[][]> TestIntEnumerable => [
            [.. Enumerable.Repeat<int[]>([], 256)],
            [.. Enumerable.Range(0, 1024).Select<int, int[]>(i => [i])],
            [.. Enumerable.Repeat(Enumerable.Range(0, 128).ToArray(), 512)]
        ];

        [Theory]
        [MemberData(nameof(TestIntEnumerable))]
        public void AppendTest(int[][] items)
        {
            using CollectionBuilder<int> builder = new();
            int[] expected = [.. items.Flatten()];

            foreach (var array in items)
            {
                foreach (var v in array)
                {
                    builder.Append(v);
                }
            }

            Assert.Equal(expected, builder.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestIntEnumerable))]
        public void AppendIEnumerableRangeTest(int[][] items)
        {
            using CollectionBuilder<int> builder = new();
            int[] expected = [.. items.Flatten()];

            foreach (var array in items)
            {
                builder.AppendIEnumerableRange(array);
            }

            Assert.Equal(expected, builder.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestIntEnumerable))]
        public void AppendICollectionRangeTest(int[][] items)
        {
            using CollectionBuilder<int> builder = new();
            int[] expected = [.. items.Flatten()];

            foreach (var array in items)
            {
                builder.AppendICollectionRange(array);
            }

            Assert.Equal(expected, builder.ToArray());
        }

        [Theory]
        [MemberData(nameof(TestIntEnumerable))]
        public void AppendSpanRangeTest(int[][] items)
        {
            using CollectionBuilder<int> builder = new();
            int[] expected = [.. items.Flatten()];

            foreach (var array in items)
            {
                builder.AppendRange(array.AsSpan());
            }

            Assert.Equal(expected, builder.ToArray());
        }
    }
}
