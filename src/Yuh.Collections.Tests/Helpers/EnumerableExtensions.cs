namespace Yuh.Collections.Tests.Helpers
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> enumerable)
            => enumerable.Aggregate(Enumerable.Empty<T>(), (accum, item) => accum.Concat(item));

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable, int? seed = null)
        {
            Random rand = seed.HasValue ? new(seed.Value) : new();
            PriorityQueue<T, int> pq = new(enumerable.Select(x => (x, rand.Next())));
            while (pq.Count != 0)
            {
                yield return pq.Dequeue();
            }
        }
    }
}
