using System.Numerics;

namespace Yuh.Collections.Tests.Helpers
{
    internal static class EnumerableHelper
    {
        public static IEnumerable<T[]> Permutations<T>(T[] array) where T : IComparisonOperators<T, T, bool>, IEqualityOperators<T, T, bool>
        {
            int len = array.Length;
            switch (len)
            {
                case 0:
                    yield break;
                case 1:
                    yield return array;
                    yield break;
                case 2:
                    yield return array;
                    yield return [array[1], array[0]];
                    yield break;
            }

            T[] items = new T[len];
            Array.Copy(array, items, len);
            Array.Sort(items);
            yield return items.ToArray();

            while (true)
            {
                int i = len - 2, j = len;
                while (i >= 0 && items[i] >= items[i + 1])
                {
                    i--;
                }
                if (i == -1)
                {
                    yield break;
                }

                do
                {
                    j--;
                }
                while (items[i] >= items[j]);

                if (i == j)
                {
                    yield break;
                }

                (items[i], items[j]) = (items[j], items[i]);
                items.AsSpan()[(i + 1)..len].Reverse();
                yield return items.ToArray();
            }
        }
    }
}
