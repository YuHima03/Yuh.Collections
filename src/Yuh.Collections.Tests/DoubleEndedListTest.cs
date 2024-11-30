using Yuh.Collections.Tests.DataProviders;

namespace Yuh.Collections.Tests
{
    public class DoubleEndedListTest
    {
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
        [ClassData(typeof(IntArrayData))]
        public void InitializeTest(int[] items)
        {
            DoubleEndedList<int> list = new(items as IEnumerable<int>);
            Assert.Equal(items.AsSpan(), list.AsReadOnlySpan());
        }
    }
}
