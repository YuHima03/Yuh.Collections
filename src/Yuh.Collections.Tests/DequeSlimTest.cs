using Xunit.Abstractions;

namespace Yuh.Collections.Tests
{
    public class DequeSlimTest(ITestOutputHelper @out)
    {
        private readonly ITestOutputHelper _out = @out;

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

        [Fact]
        public void PushAndPopTest()
        {
            Deque<int> buffer = new(4);

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
    }
}
