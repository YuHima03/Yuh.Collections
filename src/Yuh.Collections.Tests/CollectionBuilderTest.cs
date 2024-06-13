using Xunit.Abstractions;

namespace Yuh.Collections.Tests
{
    public class CollectionBuilderTest(ITestOutputHelper @out)
    {
        private readonly ITestOutputHelper _out = @out;

        [Fact]
        public void AddTest()
        {
            CollectionBuilder<int> builder = new();
            foreach (var i in Enumerable.Range(0, 100))
            {
                builder.Add(i);
            }

            var array = new int[builder.Count];
            builder.CopyTo(array.AsSpan());
            OutputHelpers.OutputElements(array, _out);
        }

        [Fact]
        public void AddRangeEnumerableTest()
        {
            CollectionBuilder<int> builder = new();

            for (int i = 0; i < 8; i++)
            {
                builder.AddRange(Enumerable.Range(0, 64));
            }

            OutputHelpers.OutputElements(builder.ToArray(), _out);
        }

        [Fact]
        public void AddRangeTest()
        {
            CollectionBuilder<int> builder = new();

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
