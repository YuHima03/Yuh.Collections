using Xunit.Abstractions;
using Yuh.Collections.Views;

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

        [Fact]
        public void RemoveRangeTest()
        {
            DequeSlimView<int> view = new()
            {
                Capacity = 12,
                Count = 12,
                Head = 6,
                Items = [6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5],
                //                           ^
                //                           head
                Version = 0
            };

            var deque = view.Encapsulate();

            var dq00 = DequeSlim.CreateClone(deque);
            dq00.RemoveRange(0, 12);
            Assert.Empty(dq00);

            var dq01 = DequeSlim.CreateClone(deque);
            dq01.RemoveRange(0, 4);
            Assert.Equal(dq01, Enumerable.Range(4, 8));

            var dq02 = DequeSlim.CreateClone(deque);
            dq02.RemoveRange(0, 8);
            Assert.Equal(dq02, Enumerable.Range(8, 4));

            var dq03 = DequeSlim.CreateClone(deque);
            dq03.RemoveRange(8, 4);
            Assert.Equal(dq03, Enumerable.Range(0, 8));

            var dq04 = DequeSlim.CreateClone(deque);
            dq04.RemoveRange(4, 8);
            Assert.Equal(dq04, Enumerable.Range(0, 4));

            var dq05 = DequeSlim.CreateClone(deque);
            dq05.RemoveRange(2, 2);
            Assert.Equal((IEnumerable<int>)dq05, [0, 1, .. Enumerable.Range(4, 8)]);

            var dq06 = DequeSlim.CreateClone(deque);
            dq06.RemoveRange(2, 6);
            Assert.Equal((IEnumerable<int>)dq06, [0, 1, 8, 9, 10, 11]);

            var dq07 = DequeSlim.CreateClone(deque);
            dq07.RemoveRange(8, 2);
            Assert.Equal((IEnumerable<int>)dq07, [.. Enumerable.Range(0, 8), 10, 11]);

            var dq08 = DequeSlim.CreateClone(deque);
            dq08.RemoveRange(4, 6);
            Assert.Equal((IEnumerable<int>)dq08, [0, 1, 2, 3, 10, 11]);
        }
    }
}
