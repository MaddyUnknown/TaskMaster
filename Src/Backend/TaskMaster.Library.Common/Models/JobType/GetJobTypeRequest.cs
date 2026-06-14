using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Models.JobType
{
    public class GetJobTypeRequest
    {
        public string JobTypeName { get; set; } = string.Empty;
        public long JobTypeVersion { get; set; }
    }
}
