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

                var message = $"{j.JobType?.Name ?? "Unknown"} job {GetStatusText(j.Status)}";

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
            var successRate = totalJobs > 0 ? Math.Round((double)data.CompletedJobs / totalJobs * 100, 1) : 100;

            return new SystemMetrics
            {
                SuccessRate = successRate,
                // TODO: Compute from actual job duration data once processing timestamps are tracked
                AvgProcessingTime = 1.8,
                ActiveWorkers = data.ActiveWorkers,
                QueueDepth = data.QueuedJobs
            };
        }

        public async Task<SystemHealth> GetSystemHealthAsync()
        {
            var data = await _dashboardQuery.GetDashboardDataAsync();

            var uptime = Environment.TickCount64 > 0
                ? $"{TimeSpan.FromMilliseconds(Environment.TickCount64).Days}d"
                : "unknown";

            return new SystemHealth
            {
                Api = new ComponentHealth
                {
                    Status = HealthStatus.Healthy,
                    Uptime = uptime,
                    // TODO: Replace with real API latency measurement
                    Latency = new Random().Next(5, 30)
                },
                Database = new DatabaseHealth
                {
                    Status = data.DatabaseHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                    // TODO: Query real connection count from DB
                    Connections = data.ActiveWorkers + 2,
                    // TODO: Read from actual SQL Server pool config
                    PoolSize = 100
                },
                Queue = new QueueHealth
                {
                    Status = HealthStatus.Healthy,
                    Depth = data.QueuedJobs,
                    // TODO: Calculate from actual queue processing rate
                    Throughput = new Random().Next(30, 80)
                },
                Workers = new WorkerHealth
                {
                    Status = data.ActiveWorkers > 0 ? HealthStatus.Healthy : HealthStatus.Degraded,
                    Active = data.ActiveWorkers,
                    Inactive = data.InactiveWorkers
                }
            };
        }

        private static string GetStatusText(JobStatusEnum status) => status switch
        {
            JobStatusEnum.Completed => "completed",
            JobStatusEnum.Failed => "failed",
            JobStatusEnum.InProgress => "started",
            _ => "queued"
        };
    }
}
