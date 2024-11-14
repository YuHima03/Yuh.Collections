using System.Diagnostics;
using System.Text;

namespace Yuh.Collections.Tests
{
    public class CollectionBuilderTest()
    {
        private const int TestEnumerableCount = 128;
        private const int InsertionCount = 512;
        private const int TotalItemsCount = TestEnumerableCount * InsertionCount;

        private static IEnumerable<int> TestEnumerable => Enumerable.Range(0, TestEnumerableCount);

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

        [Fact]
        public void AddSpanRangeTest()
        {
            using CollectionBuilder<int> builder = new();
            List<int> list = new(TotalItemsCount);

            var testArray = TestEnumerable.ToArray();
            var testSpan = TestEnumerable.ToArray().AsSpan();

            foreach (var _ in Enumerable.Range(0, InsertionCount))
            {
                builder.AppendRange(testSpan);
#if NET8_0_OR_GREATER
                list.AddRange(testSpan);
#else
                list.AddRange(testArray);
#endif
            }

            Assert.Equal(list, builder.ToArray());
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
