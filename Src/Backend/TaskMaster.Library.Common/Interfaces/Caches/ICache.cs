using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Interfaces.Caches
{
    internal interface ICache
    {
        T GetOrAdd<T>(string key, Func<T>? factory = null);
    }
}
