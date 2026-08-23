using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;
using TaskMaster.API.Controllers;
using TaskMaster.API.Models.Auth;
using TaskMaster.API.Models.Common;

namespace TaskMaster.Test.UnitTests.APITests.AuthTests;

public class AuthConfigControllerTests
{
    private static AuthConfigController CreateController(AuthConfig authConfig)
    {
        var controller = new AuthConfigController(Options.Create(authConfig), NullLogger<AuthConfigController>.Instance);
        //controller.ControllerContext = new ControllerContext();
        return controller;
    }

    [Test]
    public void GetConfig_WhenModeIsNone_ShouldReturnNoneAndNullOidc()
    {
        // Arrange
        var controller = CreateController(new AuthConfig { Mode = AuthMode.None });

        // Act
        var result = controller.GetConfig();

        // Assert
        var ok = result!.Result as OkObjectResult;
        var payload = ok!.Value as ApiResponse<AuthConfigResponse>;
        Assert.That(payload!.IsSuccess, Is.True);
        Assert.That(payload.Data!.Mode, Is.EqualTo(AuthMode.None));
        Assert.That(payload.Data.Oidc, Is.Null);
    }

    [Test]
    public void GetConfig_WhenModeIsOidc_ShouldReturnProviderDetailsAndRequiredScopes()
    {
        // Arrange
        var authConfig = new AuthConfig
        {
            Mode = AuthMode.Oidc,
            Oidc = new OidcAuthConfig
            {
                Authority = "https://idp.example.com",
                Audience = "taskmaster-api",
                ScopeClaim = "scp"
            }
        };
        var controller = CreateController(authConfig);

        // Act
        var result = controller.GetConfig() as ActionResult<ApiResponse<AuthConfigResponse>>;

        // Assert
        var ok = result!.Result as OkObjectResult;
        var payload = ok!.Value as ApiResponse<AuthConfigResponse>;
        Assert.That(payload!.Data!.Oidc, Is.Not.Null);
        Assert.That(payload.Data.Oidc!.Authority, Is.EqualTo("https://idp.example.com"));
        Assert.That(payload.Data.Oidc.Audience, Is.EqualTo("taskmaster-api"));
        Assert.That(payload.Data.Oidc.ScopeClaim, Is.EqualTo("scp"));

    }
}