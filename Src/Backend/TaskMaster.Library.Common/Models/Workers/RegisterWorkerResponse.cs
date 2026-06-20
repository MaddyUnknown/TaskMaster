using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Models.Workers
{
    public class RegisterWorkerResponse
    {
        public WorkerDetails WorkerDetails { get; set; } = WorkerDetails.Empty;
        public int HeartBeatIntervalSeconds { get; set; }
    }
}
