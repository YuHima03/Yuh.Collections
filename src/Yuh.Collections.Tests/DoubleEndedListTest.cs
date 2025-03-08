using Yuh.Collections.Tests.DataProviders;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests
{
    public class DoubleEndedListTest
    {
        public static TheoryData<int[]> NonEmptyAndNonSingleIntArrayData => [.. IntArrayData.DataSource.Where(x => x.Length >= 2)];

        public static TheoryData<int[]> ShuffledIntArrayData => [.. IntArrayData.DataSource.Select(x => x.Shuffle(123456).ToArray())];

        public static TheoryData<ValueTuple<int[], int[]>> ShuffledIntArrayPairData => [
            .. Enumerable.Zip(
                IntArrayData.DataSource.Select(x => x.Shuffle(345678).ToArray()),
                IntArrayData.DataSource.Select(x => x.Shuffle(123890).ToArray())
            )
        ];

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
        [MemberData(nameof(ShuffledIntArrayData))]
        public void FindTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.Length / 2);

            if (data.Length == 0)
            {
                foreach (var n in (NonEmptyAndNonSingleIntArrayData as IEnumerable<int[]>).First())
                {
                    Assert.Equal(-1, list.Find(x => x == n));
                }
                return;
            }

            Dictionary<int, int> firstIdx = [];
            for (int i = 0; i < data.Length; i += 2)
            {
                var x = data[i];
                _ = firstIdx.TryAdd(x, list.Count);
                list.PushBack(x);
            }

            foreach (var n in data)
            {
                if (firstIdx.TryGetValue(n, out var i))
                {
                    Assert.Equal(i, list.Find(x => x == n));
                }
                else
                {
                    Assert.Equal(-1, list.Find(x => x == n));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ShuffledIntArrayData))]
        public void FindLastTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.Length / 2);

            if (data.Length == 0)
            {
                foreach (var n in (NonEmptyAndNonSingleIntArrayData as IEnumerable<int[]>).First())
                {
                    Assert.Equal(-1, list.FindLast(x => x == n));
                }
            }

            Dictionary<int, Index> lastIdx = [];
            for (int i = 0; i < data.Length; i += 2)
            {
                var x = data[^(i + 1)];
                _ = lastIdx.TryAdd(x, ^(list.Count + 1));
                list.PushFront(x);
            }

            foreach (var n in data)
            {
                if (lastIdx.TryGetValue(n, out var i))
                {
                    Assert.Equal(i.GetOffset(list.Count), list.FindLast(x => x == n));
                }
                else
                {
                    Assert.Equal(-1, list.FindLast(x => x == n));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ShuffledIntArrayData))]
        public void IndexOfTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.Length / 2);

            if (data.Length == 0)
            {
                foreach (var n in (NonEmptyAndNonSingleIntArrayData as IEnumerable<int[]>).First())
                {
                    Assert.Equal(-1, list.IndexOf(n));
                }
            }

            Dictionary<int, int> firstIdx = [];
            for (int i = 0; i < data.Length; i += 2)
            {
                var x = data[i];
                _ = firstIdx.TryAdd(x, list.Count);
                list.PushBack(x);
            }

            foreach (var n in data)
            {
                if (firstIdx.TryGetValue(n, out var i))
                {
                    Assert.Equal(i, list.IndexOf(n));
                }
                else
                {
                    Assert.Equal(-1, list.IndexOf(n));
                }
            }
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void InitializeTest(int[] items)
        {
            DoubleEndedList<int> list = new(items as IEnumerable<int>);
            Assert.Equal(items.AsSpan(), list.AsReadOnlySpan());
        }

        [Theory]
        [MemberData(nameof(NonEmptyAndNonSingleIntArrayData))]
        public void InsertTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.AsSpan());
            List<int> expected = new(data);

            for (int i = 0; i < data.Length; i++)
            {
                list.Insert(i * 2, data[^(i + 1)]);
                expected.Insert(i * 2, data[^(i + 1)]);
            }

            Assert.Equal(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(expected), list.AsReadOnlySpan());
        }

        [Theory]
        [MemberData(nameof(NonEmptyAndNonSingleIntArrayData))]
        public void InsertIEnumerableRangeTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.AsSpan());
            List<int> expected = new(data);

            int step = Math.Max(data.Length / 64, 1);
            for (int i = 0; i < data.Length; i += step)
            {
                list.InsertRange(i, data as IEnumerable<int>);
                expected.InsertRange(i, data);
            }

            Assert.Equal(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(expected), list.AsReadOnlySpan());
        }

        [Theory]
        [MemberData(nameof(NonEmptyAndNonSingleIntArrayData))]
        public void InsertSpanRangeTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.AsSpan());
            List<int> expected = new(data);

            Span<int> dataSpan = data.AsSpan();
            int step = Math.Max(data.Length / 64, 1);
            for (int i = 0; i < data.Length; i += step)
            {
                list.InsertRange(i, dataSpan);
                expected.InsertRange(i, data);
            }

            Assert.Equal(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(expected), list.AsReadOnlySpan());
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void ItemsTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.AsSpan());

            Assert.All(data, (v, i) => Assert.Equal(v, list[i]));
            Assert.All(Enumerable.Range(-5, 5), i => Assert.Throws<IndexOutOfRangeException>(() => list[i]));
            Assert.All(Enumerable.Range(list.Count, 5), i => Assert.Throws<IndexOutOfRangeException>(() => list[i]));
        }

        [Theory]
        [MemberData(nameof(ShuffledIntArrayData))]
        public void LastIndexOfTest(int[] data)
        {
            DoubleEndedList<int> list = new(data.Length / 2);

            if (data.Length == 0)
            {
                foreach (var n in (NonEmptyAndNonSingleIntArrayData as IEnumerable<int[]>).First())
                {
                    Assert.Equal(-1, list.LastIndexOf(n));
                }
            }

            Dictionary<int, Index> lastIdx = [];
            for (int i = 0; i < data.Length; i += 2)
            {
                var x = data[^(i + 1)];
                _ = lastIdx.TryAdd(x, ^(list.Count + 1));
                list.PushFront(x);
            }

            foreach (var n in data)
            {
                if (lastIdx.TryGetValue(n, out var i))
                {
                    Assert.Equal(i.GetOffset(list.Count), list.LastIndexOf(n));
                }
                else
                {
                    Assert.Equal(-1, list.LastIndexOf(n));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ShuffledIntArrayData))]
        public void PeekFirstTest(int[] data)
        {
            var (fi, bi) = (0, data.Length - 1);

            DoubleEndedList<int> list = [];

            if (data.Length == 0)
            {
                Assert.Throws<InvalidOperationException>(() => list.PeekFirst());
                return;
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 2 == 0)
                {
                    list.PushFront(data[fi]);
                    fi++;
                }
                else
                {
                    list.PushBack(data[bi]);
                    bi--;
                }

                Assert.Equal(data[fi - 1], list.PeekFirst());
            }
        }

        [Theory]
        [MemberData(nameof(ShuffledIntArrayData))]
        public void PeekLastTest(int[] data)
        {
            var (fi, bi) = (0, data.Length - 1);

            DoubleEndedList<int> list = [];

            if (data.Length == 0)
            {
                Assert.Throws<InvalidOperationException>(() => list.PeekLast());
                return;
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 2 == 0)
                {
                    list.PushBack(data[bi]);
                    bi--;
                }
                else
                {
                    list.PushFront(data[fi]);
                    fi++;
                }

                Assert.Equal(data[bi + 1], list.PeekLast());
            }
        }

        [Theory]
        [MemberData(nameof(ShuffledIntArrayData))]
        public void PopBackTest(int[] data)
        {
            DoubleEndedList<int> list = [];
            using DequeSlim<int> expected = new(data.Length);

            if (data.Length == 0)
            {
                Assert.Throws<InvalidOperationException>(() => list.PopBack());
                return;
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 2 == 0)
                {
                    list.PushBack(data[i]);
                    expected.PushBack(data[i]);
                }
                else
                {
                    list.PushFront(data[i]);
                    expected.PushFront(data[i]);
                }

                int back;
                Assert.Equal(back = expected.PopBack(), list.PopBack());
                Assert.Equal(expected.AsSpan(), list.AsReadOnlySpan());

                list.PushBack(back);
                expected.PushBack(back);
            }
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void PopBackRangeTest(int[] data)
        {
            Span<int> dataSpan = data.AsSpan();
            DoubleEndedList<int> list = new(dataSpan);

            if (data.Length == 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = list.PopBackRange(1));
                return;
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = list.PopBackRange(list.Count + 1));
            }

            using LessAllocArray<int> _buffer = new(data.Length);
            Span<int> buffer = _buffer.Array.AsSpan();

            for (int i = 1; i <= data.Length; i++)
            {
                var destination = buffer[..i];
                list.PopBackRange(destination);
                Assert.Equal(dataSpan[^i..], destination);
                list.PushBackRange(destination);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = list.PopBackRange(list.Count + 1));
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void PopFrontTest(int[] data)
        {
            DoubleEndedList<int> list = [];
            using DequeSlim<int> expected = new(data.Length);

            if (data.Length == 0)
            {
                Assert.Throws<InvalidOperationException>(() => list.PopFront());
                return;
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 2 == 0)
                {
                    list.PushBack(data[i]);
                    expected.PushBack(data[i]);
                }
                else
                {
                    list.PushFront(data[i]);
                    expected.PushFront(data[i]);
                }

                int front;
                Assert.Equal(front = expected.PopFront(), list.PopFront());
                Assert.Equal(expected.AsSpan(), list.AsReadOnlySpan());

                list.PushFront(front);
                expected.PushFront(front);
            }
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void PopFrontRangeTest(int[] data)
        {
            Span<int> dataSpan = data.AsSpan();
            DoubleEndedList<int> list = new(dataSpan);

            if (data.Length == 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = list.PopFrontRange(1));
                return;
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = list.PopFrontRange(list.Count + 1));
            }

            using LessAllocArray<int> _buffer = new(data.Length);
            Span<int> buffer = _buffer.Array.AsSpan();

            for (int i = 1; i <= data.Length; i++)
            {
                var destination = buffer[..i];
                list.PopFrontRange(destination);
                Assert.Equal(dataSpan[..i], destination);
                list.PushFrontRange(destination);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = list.PopFrontRange(list.Count + 1));
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void PushBackTest(int[] data)
        {
            Span<int> dataSpan = data.AsSpan();
            DoubleEndedList<int> list = [];

            for (int i = 0; i < data.Length; i++)
            {
                list.PushBack(data[i]);
                Assert.Equal(dataSpan[..(i + 1)], list.AsReadOnlySpan());
            }
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void PushBacRangeTest(int[] data)
        {
            Span<int> dataSpan = data.AsSpan();
            DoubleEndedList<int> list = new(dataSpan);

            using LessAllocArray<int> _buffer = new(data.Length);
            Span<int> buffer = _buffer.Array.AsSpan();

            using LessAllocArray<int> _expected = new(data.Length * 2);
            Span<int> expected = _expected.Array.AsSpan();
            dataSpan.CopyTo(expected);
            dataSpan.CopyTo(expected[data.Length..]);

            for (int i = 1; i <= data.Length; i++)
            {
                list.PushBackRange(dataSpan[..i]);
                Assert.Equal(expected[..list.Count], list.AsReadOnlySpan());
                list.PopBackRange(buffer[..i]);
            }
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void PushFrontTest(int[] data)
        {
            Span<int> dataSpan = data.AsSpan();
            DoubleEndedList<int> list = [];

            for (int i = 0; i < data.Length; i++)
            {
                list.PushFront(data[^(i + 1)]);
                Assert.Equal(dataSpan[^(i + 1)..], list.AsReadOnlySpan());
            }
        }

        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void PushFrontRangeTest(int[] data)
        {
            Span<int> dataSpan = data.AsSpan();
            DoubleEndedList<int> list = new(dataSpan);

            using LessAllocArray<int> _buffer = new(data.Length);
            Span<int> buffer = _buffer.Array.AsSpan();

            using LessAllocArray<int> _expected = new(data.Length * 2);
            Span<int> expected = _expected.Array.AsSpan();
            dataSpan.CopyTo(expected);
            dataSpan.CopyTo(expected[data.Length..]);

            for (int i = 0; i < data.Length; i++)
            {
                list.PushFrontRange(dataSpan[^i..]);
                Assert.Equal(expected[^list.Count..], list.AsReadOnlySpan());
                list.PopFrontRange(buffer[..i]);
            }
        }
    }
}
