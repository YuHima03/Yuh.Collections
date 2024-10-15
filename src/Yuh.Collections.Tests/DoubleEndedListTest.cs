using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests
{
    public class DoubleEndedListTest(ITestOutputHelper @out)
    {
        private readonly ITestOutputHelper _out = @out;

        public static IEnumerable<object[]> PushCountTestData => [[0], [256], [1024], [4096]];

        public static IEnumerable<object[]> PushAndPopCountTestData
            => PushCountTestData
                .Select(x => (int)x[0])
                .Select(
                    x => Enumerable.Distinct([0, x / 4, x / 2, x]).Select<int, object[]>(y => [x, y])
                )
                .Aggregate<IEnumerable<object[]>, IEnumerable<object[]>>([], (val, res) => res.Concat(val));

        public static IEnumerable<object[]> PushRangeTestData => EnumerableHelper.Permutations([0, 256, 4096]).Select<int[], object[]>(x => [x]);

        [Theory]
        [MemberData(nameof(PushAndPopCountTestData))]
        public void PopBackTest(int pushCount, int popCount)
        {
            DoubleEndedList<int> list = new(Enumerable.Range(0, pushCount));
            for (int i = 0; i < popCount; i++)
            {
                _ = list.PopBack();
            }
            Assert.Equal(Enumerable.Range(0, pushCount - popCount), list);
        }

        [Theory]
        [MemberData(nameof(PushAndPopCountTestData))]
        public void PopFrontTest(int pushCount, int popCount)
        {
            DoubleEndedList<int> list = new(Enumerable.Range(0, pushCount));
            for (int i = 0; i < popCount; i++)
            {
                _ = list.PopFront();
            }
            Assert.Equal(Enumerable.Range(popCount, pushCount - popCount), list);
        }

        [Theory]
        [MemberData(nameof(PushRangeTestData))]
        public void PushBackRangeTest(IEnumerable<int> counts)
        {
            IEnumerable<int> enumerable = [];
            DoubleEndedList<int> list = [];
            foreach (int c in counts)
            {
                var range = Enumerable.Range(0, c);
                enumerable = enumerable.Concat(range);
                list.PushBackRange(range);
            }
            Assert.Equal(enumerable, list);
        }

        [Theory]
        [MemberData(nameof(PushRangeTestData))]
        public void PushFrontRangeTest(IEnumerable<int> counts)
        {
            IEnumerable<int> enumerable = [];
            DoubleEndedList<int> list = [];
            foreach (int c in counts)
            {
                var range = Enumerable.Range(0, c);
                enumerable = range.Concat(enumerable);
                list.PushFrontRange(range);
            }
            Assert.Equal(enumerable, list);
        }

        [Theory]
        [MemberData(nameof(PushCountTestData))]
        public void PushBackTest(int count)
        {
            var enumerable = Enumerable.Range(0, count);
            DoubleEndedList<int> list = [];
            foreach (var n in enumerable)
            {
                list.PushBack(n);
            }
            Assert.Equal(enumerable, list);
        }

        [Theory]
        [MemberData(nameof(PushCountTestData))]
        public void PushFrontTest(int count)
        {
            var enumerable = Enumerable.Range(0, count);
            DoubleEndedList<int> list = [];
            foreach (var n in enumerable)
            {
                list.PushFront(n);
            }
            Assert.Equal(enumerable.Reverse(), list);
        }

        [Fact]
        public void GrowTest()
        {
            DoubleEndedList<int> list = new(6);

            list.PushBack(0);
            list.PushBack(1);
            list.PushBack(2);
            OutputCapacityAndMargin(list);

            list.PushBack(3);
            OutputCapacityAndMargin(list);

            list.PushFront(4);
            OutputCapacityAndMargin(list);

            list.PushFront(5);
            OutputCapacityAndMargin(list);

            list.PushFront(6);
            OutputCapacityAndMargin(list);

            list.PushFront(7);
            OutputCapacityAndMargin(list);

            DoubleEndedList<int> list2 = new([0, 1, 2, 3]);
            list2.EnsureCapacity(8);
            OutputCapacityAndMargin(list2);

            DoubleEndedList<int> list3 = new([0, 1, 2, 3]);
            list3.EnsureCapacity(1, 2);
            OutputCapacityAndMargin(list3);
        }

        [Fact]
        public void PushAndPopRangeTest()
        {
            DoubleEndedList<int> list = new([0, 1, 2, 3]);

            list.PushBackRange([4, 5, 6]);
            OutputHelpers.OutputElements(list, _out);

            OutputHelpers.OutputElements(list.PopFrontRange(2), _out);
            OutputHelpers.OutputElements(list, _out);

            list.PushFrontRange([7, 8, 9, 10, 11, 12]);
            OutputHelpers.OutputElements(list, _out);

            OutputHelpers.OutputElements(list.PopBackRange(4), _out);
            OutputHelpers.OutputElements(list, _out);
        }

        [Fact]
        public void PushAndPopTest()
        {
            DoubleEndedList<int> buffer = new(4);

            buffer.PushBack(1);
            buffer.PushBack(2);
            buffer.PushBack(3);
            buffer.PushBack(4);
            OutputHelpers.OutputElements(buffer, _out);

            buffer.PushBack(5);
            buffer.PushBack(6);
            OutputHelpers.OutputElements(buffer, _out);

            buffer.PushFront(7);
            buffer.PushFront(8);
            OutputHelpers.OutputElements(buffer, _out);

            buffer.PopBack();
            buffer.PopFront();
            OutputHelpers.OutputElements(buffer, _out);

            buffer.Clear();
        }

        [Fact]
        public void InsertTest()
        {
            DoubleEndedList<int> list = new([0, 1, 2, 3, 4, 5, 6, 7]);

            list.InsertRange(6, [8, 9, 10, 11]);
            OutputHelpers.OutputElements(list, _out);
        }

        internal void OutputCapacityAndMargin(DoubleEndedList<int> list, [CallerArgumentExpression(nameof(list))] string? argName = null)
        {
            _out.WriteLine($"{argName}: {list.Capacity}, {list.FrontMargin}, {list.BackMargin}");
        }
    }
}
