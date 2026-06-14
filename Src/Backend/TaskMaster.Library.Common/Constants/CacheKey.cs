using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Constants
{
    internal static class CacheKey
    {
        internal static string JobType(string name, long version) => $"JobType${name}:{version}";
    }
}
