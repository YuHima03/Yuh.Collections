using Xunit.Abstractions;

namespace Yuh.Collections.Tests
{
    public class CollectionBuilderTest(ITestOutputHelper @out)
    {
        private readonly ITestOutputHelper _out = @out;

        [Fact]
        public void AddTest()
        {
            var _container = new int[31][];
            CollectionBuilder<int> builder = new(_container.AsSpan());
            foreach (var i in Enumerable.Range(0, 100))
            {
                builder.Add(i);
            }

            var array = new int[builder.Count];
            builder.CopyTo(array.AsSpan());
            OutputHelpers.OutputElements(array, _out);
        }

        [Fact]
        public void AddRangeTest()
        {
            var _container = new int[31][];
            CollectionBuilder<int> builder = new(_container.AsSpan());

            int[] input = [0, 1, 2, 3, 4, 5, 6, 7];
            for(int i = 0; i < 8; i++)
            {
                try
                {
                    builder.AddRange(input.AsSpan());
                }
                catch(Exception e)
                {
                    _out.WriteLine($"Error at {i}.");
                    throw new Exception(null, e);
                }
            }

            var array = new int[builder.Count];
            builder.CopyTo(array.AsSpan());
            OutputHelpers.OutputElements(array, _out);
        }
    }
}
