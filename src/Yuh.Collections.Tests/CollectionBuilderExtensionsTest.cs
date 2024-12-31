using System.Runtime.CompilerServices;
using System.Text;
using Yuh.Collections.Tests.DataProviders;
using Yuh.Collections.Tests.Helpers;

namespace Yuh.Collections.Tests
{
    public class CollectionBuilderExtensionsTest
    {
        public static TheoryData<IFormattable[]> FormattedData => [
            [.. Enumerable.Range(0, 1024)],
            [.. new RandomGuidEnumerable(256, 10424)],
            [.. Enumerable.Repeat(DateTime.Parse("2024-11-03T01:23:45.6789+09:00"), 256).Select((dt, i) => dt + TimeSpan.FromHours(i))]
        ];

        [Theory]
        [MemberData(nameof(FormattedData))]
        public void AppendFormattedTest(IFormattable[] items)
        {
            CollectionBuilder<char> builder = new();
            DefaultInterpolatedStringHandler handler = new(0, items.Length);

            try
            {
                foreach (var f in items)
                {
                    builder.AppendFormatted(f);
                    handler.AppendFormatted(f);
                }

                Assert.Equal(handler.ToStringAndClear(), builder.ToSystemString());
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Theory]
        [MemberData(nameof(FormattedData))]
        public void AppendUtf8FormattedTest(IFormattable[] items)
        {
            CollectionBuilder<byte> builder = [];
            DefaultInterpolatedStringHandler handler = new(0, items.Length);

            try
            {
                foreach (var f in items)
                {
                    builder.AppendUtf8Formatted(f);
                    handler.AppendFormatted(f);
                }

                Assert.Equal(Encoding.UTF8.GetBytes(handler.ToStringAndClear()), builder.ToArray());
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Theory]
        [ClassData(typeof(StringArrayData))]
        public void AppendUtf8LiteralTest(string[] data)
        {
            CollectionBuilder<byte> builder = [];
            DefaultInterpolatedStringHandler handler = new(data.Select(x => x.Length).Sum(), 0);

            try
            {
                foreach (var s in data)
                {
                    builder.AppendLiteral(s);
                    handler.AppendLiteral(s);
                }

                Assert.Equal(Encoding.UTF8.GetBytes(handler.ToStringAndClear()), builder.ToArray());
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Theory]
        [ClassData(typeof(StringArrayData))]
        public void AppendLiteralToUtf8StringBuilderTest(string[] data)
        {
            CollectionBuilder<byte> builder = [];
            DefaultInterpolatedStringHandler handler = new(data.Select(x => x.Length).Sum(), 0);

            try
            {
                foreach (var s in data)
                {
                    builder.AppendLiteral(s);
                    handler.AppendLiteral(s);
                }

                Assert.Equal(handler.ToStringAndClear(), Encoding.UTF8.GetString(builder.ToArray()));
            }
            finally
            {
                builder.Dispose();
            }
        }

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
        public void ToStringFromUtf8StringTest(string[] data)
        {
            CollectionBuilder<byte> builder = [];
            DefaultInterpolatedStringHandler handler = new(0, data.Length);

            try
            {
                foreach (var str in data)
                {
                    builder.AppendLiteral(str);
                    handler.AppendFormatted(str);
                }

                Assert.Equal(handler.ToStringAndClear(), builder.ToSystemString());
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Theory]
        [ClassData(typeof(StringArrayData))]
        public void ToSystemStringTest(string[] items)
        {
            using CollectionBuilder<char> builder = new();
            DefaultInterpolatedStringHandler handler = new(0, items.Length);

            foreach (var str in items)
            {
                builder.AppendRange(str.AsSpan());
                handler.AppendFormatted(str);
            }

            Assert.Equal(handler.ToStringAndClear(), builder.ToSystemString());
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

            Assert.Equal(builder.ToSystemString(), builder.ToStringBuilder().ToString());
        }
    }
}
