using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Yuh.Collections
{
    public static class CollectionBuilderExtensions
    {
        public static string ToString(in this CollectionBuilder<char> builder)
        {
            int length = builder.Count;
            if (length <= 1024)
            {
                Span<char> chars = stackalloc char[length];
                builder.CopyTo(chars);
                return new(chars);
            }
            else
            {
                var charsArray = ArrayPool<char>.Shared.Rent(length);
                var chars = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(charsArray), length);
                builder.CopyTo(chars);
                string s = new(chars);

                ArrayPool<char>.Shared.Return(charsArray);
                return s;
            }
        }

        public static StringBuilder ToStringBuilder(in this CollectionBuilder<char> builder)
        {
            int length = builder.Count;
            StringBuilder sb = new(length);
            if (length <= 1024)
            {
                Span<char> chars = stackalloc char[length];
                builder.CopyTo(chars);
                return sb.Append(chars);
            }
            else
            {
                var charsArray = ArrayPool<char>.Shared.Rent(length);
                var chars = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(charsArray), length);
                builder.CopyTo(chars);
                sb.Append(chars);

                ArrayPool<char>.Shared.Return(charsArray);
                return sb;
            }
        }
    }
}
