namespace TaskMaster.API.Models.Jobs
{
    public class BulkUpdateJobStatusResponse
    {
        public int UpdatedRecordCount { get; set; }
        public IEnumerable<UpdateJobStatusErrorResponse> Errors { get; set; } = Enumerable.Empty<UpdateJobStatusErrorResponse>();
    }
}
