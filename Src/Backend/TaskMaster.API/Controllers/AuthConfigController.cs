using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;
using TaskMaster.API.Models.Auth;
using TaskMaster.API.Models.Common;

namespace TaskMaster.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthConfigController : ControllerBase
{
    private readonly IOptions<AuthConfig> _authConfig;
    private readonly ILogger<AuthConfigController> _logger;

    public AuthConfigController(IOptions<AuthConfig> authConfig, ILogger<AuthConfigController> logger)
    {
        _authConfig = authConfig;
        _logger = logger;
    }

    [HttpGet("config")]
    public ActionResult<ApiResponse<AuthConfigResponse>> GetConfig()
    {
        var config = _authConfig.Value;

        var response = new AuthConfigResponse
        {
            Mode = config.Mode
        };

        if (config.Mode == AuthMode.Oidc)
        {
            response.Oidc = new OidcAuthConfigResponse
            {
                Authority = config.Oidc.Authority,
                Audience = config.Oidc.Audience,
                ScopeClaim = config.Oidc.ScopeClaim
            };
        }

        _logger.LogInformation("Auth configuration requested (mode {Mode})", response.Mode);
        return Ok(ApiResponse<AuthConfigResponse>.Success(response));
    }
}