using Microsoft.AspNetCore.Mvc;
using TaskMaster.API.Auth;
using TaskMaster.API.Interfaces.Queries;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Dashboard;

namespace TaskMaster.API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private IDashboardService _dashboardService;
        private ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet("activity")]
        [RequirePermission(AuthPermissions.ReadDashboard)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ActivityItem>>>> GetRecentActivity([FromQuery] int items = 10)
        {
            var activities = await _dashboardService.GetRecentActivityAsync(items);
            return Ok(ApiResponse<IEnumerable<ActivityItem>>.Success(activities));
        }

        [HttpGet("metrics")]
        [RequirePermission(AuthPermissions.ReadDashboard)]
        public async Task<ActionResult<ApiResponse<SystemMetrics>>> GetSystemMetrics()
        {
            var metrics = await _dashboardService.GetSystemMetricsAsync();
            return Ok(ApiResponse<SystemMetrics>.Success(metrics));
        }

        [HttpGet("job-stats")]
        [RequirePermission(AuthPermissions.ReadDashboard)]
        public async Task<ActionResult<ApiResponse<IEnumerable<JobStatsItem>>>> GetJobStats()
        {
            var stats = await _dashboardService.GetJobStatsAsync();
            return Ok(ApiResponse<IEnumerable<JobStatsItem>>.Success(stats));
        }

        [HttpGet("health")]
        [RequirePermission(AuthPermissions.ReadDashboard)]
        public async Task<ActionResult<ApiResponse<SystemHealth>>> GetSystemHealth()
        {
            var health = await _dashboardService.GetSystemHealthAsync();
            return Ok(ApiResponse<SystemHealth>.Success(health));
        }
    }
}
