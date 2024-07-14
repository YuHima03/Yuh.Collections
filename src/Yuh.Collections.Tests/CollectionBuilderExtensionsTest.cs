namespace Yuh.Collections.Tests
{
    public class CollectionBuilderExtensionsTest
    {
        [Fact]
        public void ToBasicStringTest()
        {
            CollectionBuilder<char> builder = new();
            builder.AppendRange("foo\n".AsSpan());
            builder.AppendRange("bar\n".AsSpan());
            builder.AppendRange("qux\n".AsSpan());
            builder.AppendRange("The quick brown fox jumps over the lazy dog.".AsSpan());

            Assert.True(builder.ToBasicString() == "foo\nbar\nqux\nThe quick brown fox jumps over the lazy dog.");
        }
    }
}
