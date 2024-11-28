using System.Collections;

namespace Yuh.Collections.Tests.DataProviders
{
    public class RandomGuidEnumerable : IEnumerable<Guid>
    {
        private readonly int _length;
        private readonly Random _random;

        public int Count => _length;

        public RandomGuidEnumerable(int length)
        {
            _length = length;
            _random = new();
        }

        public RandomGuidEnumerable(int length, int seed)
        {
            _length = length;
            _random = new(seed);
        }

        public IEnumerator<Guid> GetEnumerator()
        {
            byte[] buffer = new byte[16];
            for (int i = 0; i < _length; i++)
            {
                _random.NextBytes(buffer);
                yield return new Guid(buffer);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
