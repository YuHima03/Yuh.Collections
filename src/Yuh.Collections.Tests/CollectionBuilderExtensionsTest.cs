namespace Yuh.Collections.Tests
{
    public class CollectionBuilderExtensionsTest
    {
        [Fact]
        public void ToBasicStringTest()
        {
            CollectionBuilder<char> builder = new();
            builder.AddRange("foo\n".AsSpan());
            builder.AddRange("bar\n".AsSpan());
            builder.AddRange("qux\n".AsSpan());
            builder.AddRange("The quick brown fox jumps over the lazy dog.".AsSpan());

            Assert.True(builder.ToBasicString() == "foo\nbar\nqux\nThe quick brown fox jumps over the lazy dog.");
        }
    }
}
