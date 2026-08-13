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

        public async Task<IEnumerable<ActivityItem>> GetRecentActivityAsync(int totalItems)
        {
            var recentActivities = await _dashboardQuery.GetRecentSystemActivitiesAsync(totalItems);

            return recentActivities.Select(a =>
            {
                ActivityStatus status = a.ActivityType switch
                {
                    ActivityType.JobCompleted => ActivityStatus.Success,
                    ActivityType.JobFailed => ActivityStatus.Error,
                    ActivityType.WorkerRegistered => ActivityStatus.Success,
                    ActivityType.WorkerInactive => ActivityStatus.Error,
                    ActivityType.WorkerRemoved => ActivityStatus.Error,
                    _ => ActivityStatus.Info
                };

                return new ActivityItem
                {
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    ActivityType = a.ActivityType,
                    Message = a.Message,
                    Timestamp = a.CreatedDateTime,
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
