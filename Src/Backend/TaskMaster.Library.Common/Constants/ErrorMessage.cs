using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Constants
{
    internal static class ErrorMessage
    {
        internal static string CacheKeyNotFound(string key) => $"Could not find data for key: '{key}'";
        internal static string JobTypeNotFound(string name, long version) => $"Job Type not found for name: '{name}' and version: '{version}'";
    }
}
