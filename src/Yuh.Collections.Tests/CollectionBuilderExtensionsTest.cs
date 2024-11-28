using System.Runtime.CompilerServices;
using Yuh.Collections.Tests.DataProviders;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests
{
    public class CollectionBuilderExtensionsTest
    {
        [Theory]
        [ClassData(typeof(SegmentedIntArrayData))]
        public void ToDoubleEndedListTest(int[][] items)
        {
            using CollectionBuilder<int> builder = new();

            foreach (var array in items)
            {
                builder.AppendRange(array.AsSpan());
            }

            Assert.Equal(items.Flatten(), builder.ToDoubleEndedList());
        }

        [Theory]
        [ClassData(typeof(SegmentedIntArrayData))]
        public void ToListTest(int[][] items)
        {
            using CollectionBuilder<int> builder = new();

            foreach (var array in items)
            {
                builder.AppendRange(array.AsSpan());
            }

            Assert.Equal(items.Flatten().ToList(), builder.ToList());
        }

        [Theory]
        [ClassData(typeof(StringArrayData))]
        public void ToBasicStringTest(string[] items)
        {
            using CollectionBuilder<char> builder = new();
            DefaultInterpolatedStringHandler handler = new(0, items.Length);

            foreach (var str in items)
            {
                builder.AppendRange(str.AsSpan());
                handler.AppendFormatted(str);
            }

            Assert.Equal(handler.ToStringAndClear(), builder.ToBasicString());
        }

        [Theory]
        [ClassData(typeof(StringArrayData))]
        public void ToStringBuilderTest(string[] items)
        {
            using CollectionBuilder<char> builder = new();

            foreach (var str in items)
            {
                builder.AppendRange(str.AsSpan());
            }

            Assert.Equal(builder.ToBasicString(), builder.ToStringBuilder().ToString());
        }
    }
}
