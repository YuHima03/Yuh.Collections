using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Yuh.Collections.Tests
{
    public class DoubleEndedListTest(ITestOutputHelper @out)
    {
        private readonly ITestOutputHelper _out = @out;

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
