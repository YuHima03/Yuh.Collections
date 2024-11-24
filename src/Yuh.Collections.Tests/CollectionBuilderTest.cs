using System.Diagnostics;
using System.Text;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests
{
    public class CollectionBuilderTest()
    {
        private const int TestEnumerableCount = 128;
        private const int InsertionCount = 512;
        private const int TotalItemsCount = TestEnumerableCount * InsertionCount;

        private static IEnumerable<int> TestEnumerable => Enumerable.Range(0, TestEnumerableCount);

        public static TheoryData<int[][]> TestIntEnumerable => [
            [.. Enumerable.Repeat<int[]>([], 256)],
            [.. Enumerable.Range(0, 1024).Select<int, int[]>(i => [i])],
            [.. Enumerable.Repeat(Enumerable.Range(0, 128).ToArray(), 512)]
        ];

        [Fact]
        public void AddTest()
        {
            using CollectionBuilder<int> builder = new();
            List<int> list = new(TotalItemsCount);

            var enumerable = TestEnumerable;

            foreach (var _ in Enumerable.Range(0, InsertionCount))
            {
                foreach (var num in TestEnumerable)
                {
                    builder.Append(num);
                    list.Add(num);
                }
            }

            Assert.Equal(list, builder.ToArray());
        }

        [Fact]
        public void AddIEnumerableRangeTest()
        {
            using CollectionBuilder<int> builder = new();
            List<int> list = new(TotalItemsCount);

            var enumerable = TestEnumerable;

            foreach (var _ in Enumerable.Range(0, InsertionCount))
            {
                builder.AppendIEnumerableRange(enumerable);
                list.AddRange(enumerable);
            }

            Assert.Equal(list, builder.ToArray());
        }

        [Fact]
        public void AddICollectionRangeTest()
        {
            using CollectionBuilder<int> builder = new();
            List<int> list = new(TotalItemsCount);

            var collection = (ICollection<int>)TestEnumerable.ToArray();

            foreach (var _ in Enumerable.Range(0, InsertionCount))
            {
                builder.AppendICollectionRange(collection);
                list.AddRange(collection);
            }

            Assert.Equal(list, builder.ToArray());
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

        [Fact]
        public void AppendFormattableTest()
        {
            CollectionBuilder<char> builder = new();
            StringBuilder sb = new();

            var endl = Environment.NewLine;
            foreach (var _ in Enumerable.Range(0, InsertionCount))
            {
                var now = DateTime.Now;
                builder.AppendFormatted(now);
                builder.AppendLiteral(endl);
                sb.Append(now).Append(endl);
            }

            Debug.WriteLine(builder.GetAllocatedCapacity());
            Assert.Equal(sb.ToString(), builder.ToBasicString());
            builder.Dispose();
        }
    }
}
