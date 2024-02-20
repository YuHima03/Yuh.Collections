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

        internal void OutputCapacityAndMargin(Deque<int> deque, [CallerArgumentExpression(nameof(deque))] string? argName = null)
        {
            _out.WriteLine($"{argName}: {deque.Capacity}, {deque.FrontMargin}, {deque.BackMargin}");
        }
    }
}
