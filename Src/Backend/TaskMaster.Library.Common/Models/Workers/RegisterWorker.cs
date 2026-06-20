using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Library.Common.Models.JobType;

namespace TaskMaster.Library.Common.Models.Workers
{
    public class RegisterWorker
    {
        public string WorkerName { get; set; } = string.Empty;
        public IEnumerable<JobTypeRef> JobTypeCapabilities { get; set; } = Enumerable.Empty<JobTypeRef>();
    }
}
