namespace Yuh.Collections.Tests.Helpers
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> enumerable)
            => enumerable.Aggregate(Enumerable.Empty<T>(), (accum, item) => accum.Concat(item));
    }
}
