using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Yuh.Collections.Tests
{
    public class DequeTest(ITestOutputHelper @out)
    {
        private readonly ITestOutputHelper _out = @out;

        [Fact]
        public void GrowTest()
        {
            Deque<int> deque = new(6);

            deque.PushBack(0);
            deque.PushBack(1);
            deque.PushBack(2);
            OutputCapacityAndMargin(deque);

            deque.PushBack(3);
            OutputCapacityAndMargin(deque);

            deque.PushFront(4);
            OutputCapacityAndMargin(deque);

            deque.PushFront(5);
            OutputCapacityAndMargin(deque);

            deque.PushFront(6);
            OutputCapacityAndMargin(deque);

            deque.PushFront(7);
            OutputCapacityAndMargin(deque);

            Deque<int> deque2 = new([0, 1, 2, 3]);
            deque2.EnsureCapacity(8);
            OutputCapacityAndMargin(deque2);

            Deque<int> deque3 = new([0, 1, 2, 3]);
            deque3.EnsureCapacity(1, 2);
            OutputCapacityAndMargin(deque3);
        }

        [Fact]
        public void PushAndPopRangeTest()
        {
            Deque<int> deque = new([0, 1, 2, 3]);

            deque.PushBackRange([4, 5, 6]);
            OutputHelpers.OutputElements(deque, _out);

            OutputHelpers.OutputElements(deque.PopFrontRange(2), _out);
            OutputHelpers.OutputElements(deque, _out);

            deque.PushFrontRange([7, 8, 9, 10, 11, 12]);
            OutputHelpers.OutputElements(deque, _out);

            OutputHelpers.OutputElements(deque.PopBackRange(4), _out);
            OutputHelpers.OutputElements(deque, _out);
        }

        internal void OutputCapacityAndMargin(Deque<int> deque, [CallerArgumentExpression(nameof(deque))] string? argName = null)
        {
            _out.WriteLine($"{argName}: {deque.Capacity}, {deque.FrontMargin}, {deque.BackMargin}");
        }
    }
}
