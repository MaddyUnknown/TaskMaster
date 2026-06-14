using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Models.JobType
{
    public class GetJobTypeResponse
    {
        public string Name { get; set; } = string.Empty;
        public long Version { get; set; }
        public string Schema { get; set; } = string.Empty;
    }
}
