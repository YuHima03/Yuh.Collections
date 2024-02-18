using System.Text;
using Xunit.Abstractions;

namespace Yuh.Collections.Tests
{
    public class CircularBufferTest(ITestOutputHelper @out)
    {
        private readonly ITestOutputHelper _out = @out;

        [Fact]
        public void PushAndPopTest()
        {
            CircularBuffer<int> buffer = new(4);

            buffer.PushBack(1);
            buffer.PushBack(2);
            buffer.PushBack(3);
            buffer.PushBack(4);
            OutputElements(buffer);

            buffer.PushBack(5);
            buffer.PushBack(6);
            OutputElements(buffer);

            buffer.PushFront(1);
            buffer.PushFront(2);
            OutputElements(buffer);

            buffer.PopBack();
            buffer.PopFront();
            OutputElements(buffer);

            buffer.Clear();
        }

        private void OutputElements<T>(CircularBuffer<T> buffer)
        {
            StringBuilder sb = new();

            foreach(var v in buffer)
            {
                sb.Append(v?.ToString()).Append('\x20');
            }

            _out.WriteLine(sb.ToString());
        }
    }
}