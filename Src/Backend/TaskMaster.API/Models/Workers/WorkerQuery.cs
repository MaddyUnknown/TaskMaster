using TaskMaster.API.Enums;
using TaskMaster.API.Models.Common;

namespace TaskMaster.API.Models.Workers
{
    public class WorkerQuery : PaginationQuery
    {
        public WorkerStatusEnum? Status { get; set; }
    }
}
