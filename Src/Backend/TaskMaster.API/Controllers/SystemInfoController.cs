using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.System;

namespace TaskMaster.API.Controllers;

[ApiController]
[Route("api/system")]
[AllowAnonymous]
public class SystemInfoController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SystemInfoController> _logger;

    public SystemInfoController(IWebHostEnvironment environment, ILogger<SystemInfoController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("info")]
    public ActionResult<ApiResponse<SystemInfoResponse>> GetInfo()
    {
        var version = typeof(Program).Assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .OfType<AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion;

        var response = new SystemInfoResponse
        {
            Version = TrimVersion(version),
            Environment = _environment.EnvironmentName
        };

        _logger.LogInformation("System information requested (version {Version}, environment {Environment})", response.Version, response.Environment);
        return Ok(ApiResponse<SystemInfoResponse>.Success(response));
    }

    private static string TrimVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        var plusIndex = version.IndexOf('+');
        return plusIndex > 0 ? version[..plusIndex] : version;
    }
}