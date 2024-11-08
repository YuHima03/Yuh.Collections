namespace Yuh.Collections.Tests
{
    public class SpanSequenceTest
    {
        public static IEnumerable<object[]> TestSequences => [
            [Enumerable.Range(0, 5).Select(x => Enumerable.Range(0, (int)Math.Pow(2.0, x)).OfType<object>().ToArray()).ToArray()],
            [Enumerable.Empty<object[]>()]
        ];

        public struct RefList(object[][] seq) : IRefList<object>
        {
            public readonly object[][] Seq = seq;
            public readonly int Count => Seq.Length;
            public readonly Span<object> this[int index] => Seq[index].AsSpan();
        }

        private static void CheckEquals(ref SpanSequence<object, RefList> s1, Span<object> s2)
        {
            for (int i = 0; i < s1.Length; i++)
            {
                if (!s1[i].Equals(s2[i]))
                {
                    Assert.Fail($"The span-sequence doesn't equal to original collection. (i: {i})");
                }
            }
        }

        private static object[] Flatten(object[][] arrays)
        {
            return [.. Enumerable.Aggregate(arrays, Enumerable.Empty<object>(), (a, s) => Enumerable.Concat(a, s))];
        }

        private static void SetCountBefore(ReadOnlySpan<object[]> seq, Span<int> countBefore)
        {
            for (int i = 1; i < countBefore.Length; i++)
            {
                countBefore[i] = countBefore[i - 1] + seq[i - 1].Length;
            }
        }

        private static SpanSequence<object, RefList> ToSpanSequence(object[][] seq)
        {
            int[] countBefore = new int[seq.Length];
            SetCountBefore(seq, countBefore);
            return new(new(seq), countBefore, 0);
        }

        [Theory]
        [MemberData(nameof(TestSequences))]
        public void EnumerateTest(object[][] seq)
        {
            var spanSequence = ToSpanSequence(seq);
            var flatten = Flatten(seq);

            Assert.Equal(flatten.Length, spanSequence.Length);
            CheckEquals(ref spanSequence, flatten);
        }

        [Theory]
        [MemberData(nameof(TestSequences))]
        public void IntIndexerTest(object[][] seq)
        {
            var ss = ToSpanSequence(seq);

            try
            {
                for (int i = 0; i < ss.Length; i++)
                {
                    _ = ss[i];
                }
            }
            catch (Exception e)
            {
                Assert.Fail($"An exception is thrown: {e}");
            }
        }

        [Theory]
        [MemberData(nameof(TestSequences))]
        public void IndexIndexerTest(object[][] seq)
        {
            var ss = ToSpanSequence(seq);

            try
            {
                for (int i = 0; i < ss.Length; i++)
                {
                    _ = ss[^(i + 1)];
                }
            }
            catch (Exception e)
            {
                Assert.Fail($"An exception is thrown: {e}");
            }

            for (int i = 0; i < ss.Length; i++)
            {
                Assert.Equal(ss[i], ss[^(ss.Length - i)]);
            }
        }

        [Theory]
        [MemberData(nameof(TestSequences))]
        public void InitializeTest(object[][] seq)
        {
            Span<int> countBefore = stackalloc int[seq.Length];
            SetCountBefore(seq, countBefore);

            _ = new SpanSequence<object, RefList>(new RefList(seq), countBefore, 0);
        }

        [Theory]
        [MemberData(nameof(TestSequences))]
        public void SliceTest(object[][] seq)
        {
            var ss = ToSpanSequence(seq);

            if (2 < ss.Length)
            {
                var sliced = ss[1..^1];
                var slicedFlatten = Flatten(seq)[1..^1];
                CheckEquals(ref sliced, slicedFlatten);
            }
        }
    }
}
