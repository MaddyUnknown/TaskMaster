using TaskMaster.API.Enums;
using TaskMaster.API.Models.Common;

namespace TaskMaster.API.Models.Jobs
{
    public class JobQuery : PaginationQuery
    {
        public JobStatusEnum? Status { get; set; }
    }
}
