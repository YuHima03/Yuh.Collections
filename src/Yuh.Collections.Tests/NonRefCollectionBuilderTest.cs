namespace Yuh.Collections.Tests
{
    public class NonRefCollectionBuilderTest
    {
        private const int CollectionLength = 256;
        private const int InsertionCount = 16;
        private const int TotalCount = CollectionLength * InsertionCount;

        private static readonly IEnumerable<int> TestEnumerable = Enumerable.Range(0, CollectionLength);

        [Fact]
        public void AddRangeICollectionTest()
        {
            var collection = TestEnumerable.ToArray() as ICollection<int>;

            using NonRefCollectionBuilder<int> builder = new();
            List<int> list = new(TotalCount);

            foreach (var _ in Enumerable.Range(0, InsertionCount))
            {
                builder.AddICollectionRange(collection);
                list.AddRange(collection);
            }

            Assert.Equal(builder.ToArray(), list);
        }

        [Fact]
        public void AddRangeIEnumerableTest()
        {
            using NonRefCollectionBuilder<int> builder = new();
            List<int> list = new(TotalCount);

            foreach (var _ in Enumerable.Range(0, InsertionCount))
            {
                builder.AddIEnumerableRange(TestEnumerable);
                list.AddRange(TestEnumerable);
            }

            Assert.Equal(builder.ToArray(), list);
        }

        [Fact]
        public void AddSpanRangeTest()
        {
            ReadOnlySpan<int> span = TestEnumerable.ToArray().AsSpan();

            using NonRefCollectionBuilder<int> builder = new();
            List<int> list = new(TotalCount);

            foreach (var _ in Enumerable.Range(0, InsertionCount))
            {
                builder.AddRange(span);
                list.AddRange(span);
            }

            Assert.Equal(builder.ToArray(), list);
        }

        [Fact]
        public void AddValueTest()
        {
            NonRefCollectionBuilder<char> builder = new();
            try
            {
                builder.AddFormatted(int.MaxValue, 12);
                builder.Add('\n');
                builder.AddFormatted(int.MinValue, 12);
                builder.Add('\n');
                builder.AddFormatted(long.MaxValue, 12);
                builder.Add('\n');
                builder.AddFormatted(long.MinValue, 12);

                Assert.Equal(
                    builder.ToArray(),
                    $"""
                    {int.MaxValue}
                    {int.MinValue}
                    {long.MaxValue}
                    {long.MinValue}
                    """.ReplaceLineEndings("\n")
                );
            }
            finally
            {
                builder.Dispose();
            }
        }
    }
}
