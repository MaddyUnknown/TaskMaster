using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Interfaces.Registries
{
    public interface IJobTypeSchemaRegistry
    {
        Task<string> GetByJobTypeNameAndVersion(string jobName, long jobVersion);
    }
}
