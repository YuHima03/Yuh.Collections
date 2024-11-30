using Yuh.Collections.Tests.DataProviders;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests
{
    public class DoubleEndedListTest
    {
        public static TheoryData<int[]> NonEmptyAndNonSingleIntArrayData => [.. IntArrayData.DataSource.Where(x => x.Length >= 2)];

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void AsReadOnlySpanTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.AsSpan());
            Assert.Equal(data.AsSpan(), list.AsReadOnlySpan());
        }

        [Theory]
        [ClassData(typeof(SegmentedIntArrayData))]
        public void ClearTest(int[][] data)
        {
            DoubleEndedList<int> list = new();

            foreach (var array in data)
            {
                list.PushBackRange(array.AsSpan());
                Assert.Equal(array.AsSpan(), list.AsReadOnlySpan());

                list.Clear();
                Assert.Equal([], list.AsReadOnlySpan());
            }
        }

        [Theory]
        [MemberData(nameof(NonEmptyAndNonSingleIntArrayData))]
        public void ContainsTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.Where((_, i) => i % 2 == 0));

#pragma warning disable xUnit2017 // This method tests DoubleEndedList<T>.Contains(T) method, so ignore the warning.
            for (int i = 0; i < data.Length; i += 2)
            {
                Assert.True(list.Contains(data[i]));
            }
            for (int i = 1; i < data.Length; i += 2)
            {
                Assert.False(list.Contains(data[i]));
            }
#pragma warning restore xUnit2017
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void CopyToSpanTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.AsSpan());

            var len = data.Length;
            using LessAllocArray<int> array = new(len);
            var destination = array.Array.AsSpan()[..len];
            list.CopyTo(destination);

            Assert.Equal(data.AsSpan(), destination);
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void CopyToArrayTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.AsSpan());

            var len = data.Length;
            using LessAllocArray<int> array = new(len * 2);
            var destination = array.Array;

            for (int i = 0; i < len; i += Math.Max(1, len / 128))
            {
                list.CopyTo(destination, i);
                Assert.Equal(data.AsSpan(), destination.AsSpan().Slice(i, len));
            }
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void EnsureCapacityTest(int[] data)
        {
            DoubleEndedList<int> list = new(0);
            list.EnsureCapacity(data.Length);

            int capacity = list.Capacity;

            var sourceFront = data.AsSpan()[..list.FrontMargin];
            var sourceBack = data.AsSpan().Slice(list.FrontMargin, list.BackMargin);
            list.PushFrontRange(sourceFront);
            list.PushBackRange(sourceBack);

            Assert.Equal(capacity, list.Capacity);
        }

        [Theory]
        [ClassData(typeof(SegmentedIntArrayData))]
        public void EnsureMarginTest(int[][] data)
        {
            DoubleEndedList<int> list = new(0);

            for (int i = 0; i < data.Length; i++)
            {
                var array = data[i];
                int capacity;
                if (i % 2 == 0)
                {
                    list.EnsureCapacity(0, array.Length);
                    capacity = list.Capacity;
                    list.PushBackRange(array.AsSpan());
                }
                else
                {
                    list.EnsureCapacity(array.Length, 0);
                    capacity = list.Capacity;
                    list.PushFrontRange(array.AsSpan());
                }

                Assert.Equal(capacity, list.Capacity);
            }
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void InitializeTest(int[] items)
        {
            DoubleEndedList<int> list = new(items as IEnumerable<int>);
            Assert.Equal(items.AsSpan(), list.AsReadOnlySpan());
        }
    }
}
