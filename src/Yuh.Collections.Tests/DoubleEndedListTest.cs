using Yuh.Collections.Tests.DataProviders;

namespace Yuh.Collections.Tests
{
    public class DoubleEndedListTest
    {
        [Theory]
        [ClassData(typeof(IntArrayData))]
        public void InitializeTest(int[] items)
        {
            DoubleEndedList<int> list = new(items as IEnumerable<int>);
            Assert.Equal(items, list);
        }
    }
}
