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

        [Theory]
        [MemberData(nameof(TestArrays))]
        public void EnumeratorTest(int[] array)
        {
            CollectionBuilder<int> builder = new();
            builder.AppendRange(array.AsSpan());

            using var enumerator = builder.GetEnumerator();
            for (int i = 0; i < array.Length; i++)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(array[i], enumerator.Current);
            }
            Assert.False(enumerator.MoveNext());
        }

#if NET9_0_OR_GREATER
        [Theory]
        [MemberData(nameof(TestArrays))]
        public void IGenericEnumerableTest(int[] array)
        {
            CollectionBuilder<int> builder = new();
            builder.AppendRange(array.AsSpan());

            Test(array, builder);

            static void Test<T, U>(U[] expected, T actually) where T : IEnumerable<U>,allows ref struct
            {
                using var enumerator = actually.GetEnumerator();
                for (int i = 0; i < expected.Length; i++)
                {
                    Assert.True(enumerator.MoveNext());
                    Assert.Equal(expected[i], enumerator.Current);
                }
                Assert.False(enumerator.MoveNext());
            }
        }

        [Theory]
        [MemberData(nameof(TestArrays))]
        public void IEnumerableTest(int[] array)
        {
            CollectionBuilder<int> builder = new();
            builder.AppendRange(array.AsSpan());

            Test(array, builder);

            static void Test<T, U>(U[] expected, T actually) where T : IEnumerable, allows ref struct
            {
                var enumerator = actually.GetEnumerator();
                for (int i = 0; i < expected.Length; i++)
                {
                    Assert.True(enumerator.MoveNext());
                    Assert.Equal(expected[i], enumerator.Current);
                }
                Assert.False(enumerator.MoveNext());
            }
        }
#endif
    }
}
