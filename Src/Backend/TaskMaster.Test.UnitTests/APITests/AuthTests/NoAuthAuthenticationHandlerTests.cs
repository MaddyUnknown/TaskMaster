using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.API.Auth;
using TaskMaster.API.Auth.NoAuth;
using TaskMaster.API.Configs;

namespace TaskMaster.Test.UnitTests.APITests.AuthTests;

public class NoAuthAuthenticationHandlerTests
{
    private static NoAuthAuthenticationHandler CreateHandler()
    {
        var optionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(new AuthenticationSchemeOptions());
        optionsMonitor.Setup(x => x.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

        return new NoAuthAuthenticationHandler(
            optionsMonitor.Object,
            NullLoggerFactory.Instance,
            UrlEncoder.Default);
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(NoAuthAuthenticationHandler handler, string schemeName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        using var serviceProvider = services.BuildServiceProvider();

        var scheme = new AuthenticationScheme(schemeName, schemeName, typeof(NoAuthAuthenticationHandler));
        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    [Test]
    public async Task AuthenticateAsync_ShouldAlwaysSucceed()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await AuthenticateAsync(handler, "NoAuth");

        // Assert
        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public async Task AuthenticateAsync_ShouldAssignAllPermissionClaims()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await AuthenticateAsync(handler, "NoAuth");

        // Assert
        var permissionClaims = result.Principal!.FindAll(AuthClaimTypes.Permission)
            .Select(c => c.Value)
            .ToList();
        Assert.That(permissionClaims, Is.EquivalentTo(AuthPermissions.All));
    }
}