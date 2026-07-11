using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Queries;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Dashboard;
using TaskMaster.API.Models.Enums;

namespace TaskMaster.API.Services
{
    public class DashboardService : IDashboardService
    {
        private IDashboardQuery _dashboardQuery;

        public DashboardService(IDashboardQuery dashboardQuery)
        {
            _dashboardQuery = dashboardQuery;
        }

        public async Task<IEnumerable<ActivityItem>> GetRecentActivityAsync()
        {
            var recentJobs = await _dashboardQuery.GetRecentJobsAsync(10);

            return recentJobs.Select(j =>
            {
                ActivityStatus status = j.Status switch
                {
                    JobStatusEnum.Completed => ActivityStatus.Success,
                    JobStatusEnum.Failed => ActivityStatus.Error,
                    _ => ActivityStatus.Info
                };

                var message = $"{j.JobType.Name} job {j.Status.ToString()}";

                return new ActivityItem
                {
                    Type = j.Status,
                    Message = message,
                    Timestamp = j.ModifyDateTime ?? j.CreatedDateTime,
                    Status = status
                };
            });
        }

        public async Task<SystemMetrics> GetSystemMetricsAsync()
        {
            var data = await _dashboardQuery.GetDashboardDataAsync();
            var totalJobs = data.QueuedJobs + data.InProgressJobs + data.CompletedJobs + data.FailedJobs;

            return new SystemMetrics
            {
                ActiveWorkers = data.ActiveWorkers,
                TotalJobs = totalJobs,
                QueuedJobs = data.QueuedJobs
            };
        }

        public async Task<IEnumerable<JobStatsItem>> GetJobStatsAsync()
        {
            return await _dashboardQuery.GetJobStatsAsync();
        }

        public async Task<SystemHealth> GetSystemHealthAsync()
        {
            var data = await _dashboardQuery.GetDashboardDataAsync();

            return new SystemHealth
            {
                Api = new ComponentHealth
                {
                    Status = HealthStatusEnum.Healthy,
                },
                Database = new ComponentHealth
                {
                    Status = data.DatabaseHealthy ? HealthStatusEnum.Healthy : HealthStatusEnum.Unhealthy,
                },
                Workers = new ComponentHealth
                {
                    Status = data.InactiveWorkers > 0 ? HealthStatusEnum.Degraded : HealthStatusEnum.Healthy,
                }
            };
        }
    }
}
