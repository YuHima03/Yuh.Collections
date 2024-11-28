using Yuh.Collections.Tests.DataProviders;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests
{
    public class CollectionBuilderTest()
    {
        [Theory]
        [ClassData(typeof(SegmentedIntArrayData))]
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
        [ClassData(typeof(SegmentedIntArrayData))]
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
        [ClassData(typeof(SegmentedIntArrayData))]
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
        [ClassData(typeof(SegmentedIntArrayData))]
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
        [ClassData(typeof(IntArrayData))]
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

#if NET7_0_OR_GREATER
        public static TheoryData<int[]> NonEmptyIntArrayData => new(IntArrayData.DataSource.Where(Enumerable.Any));

        [Theory]
        [MemberData(nameof(NonEmptyIntArrayData))]
        public void EnumeratorAfterCollectionChangedTest(int[] array)
        {
            using CollectionBuilder<int> builder = new();
            builder.AppendRange(array.AsSpan());

            using var enumerator = builder.GetEnumerator();
            try
            {
                builder.AppendRange(array.AsSpan());
                enumerator.MoveNext();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            Assert.Fail($"MoveNext method of {typeof(CollectionBuilder<>.Enumerator)} structure doesn't throw {nameof(InvalidOperationException)}.");
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void EnumeratorAfterDisposedTest(int[] array)
        {
            using CollectionBuilder<int> builder = new();
            builder.AppendRange(array.AsSpan());

            var exceptionThrown = false;
            var enumerator = builder.GetEnumerator();
            enumerator.Dispose();

            try
            {
                enumerator.MoveNext();
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }
            if (!exceptionThrown)
            {
                Assert.Fail($"MoveNext method of {typeof(CollectionBuilder<>.Enumerator)} structure doesn't throw an exception after disposed.");
            }

            exceptionThrown = false;
            enumerator = builder.GetEnumerator();
            enumerator.Dispose();
            try
            {
                enumerator.Reset();
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }
            if (!exceptionThrown)
            {
                Assert.Fail($"Reset method of {typeof(CollectionBuilder<>.Enumerator)} structure doesn't throw an exception after disposed.");
            }
        }
#endif

#if NET9_0_OR_GREATER
        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void IGenericEnumerableTest(int[] array)
        {
            CollectionBuilder<int> builder = new();
            builder.AppendRange(array.AsSpan());

            Test(array, builder);

            static void Test<T, U>(U[] expected, T actually) where T : IEnumerable<U>, allows ref struct
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
        [ClassData(typeof(IntArrayData))]
        public void IEnumerableTest(int[] array)
        {
            CollectionBuilder<int> builder = new();
            builder.AppendRange(array.AsSpan());

            Test(array, builder);

            static void Test<T, U>(U[] expected, T actually) where T : IEnumerable<U>, allows ref struct
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
