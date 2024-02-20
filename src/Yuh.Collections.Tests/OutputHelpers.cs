using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Yuh.Collections.Tests
{
    internal static class OutputHelpers
    {
        internal static void OutputElements<T>(IEnumerable<T> buffer, ITestOutputHelper @out)
        {
            StringBuilder sb = new();

            foreach (var v in buffer)
            {
                sb.Append(v?.ToString()).Append('\x20');
            }

            @out.WriteLine(sb.ToString());
        }
    }
}
