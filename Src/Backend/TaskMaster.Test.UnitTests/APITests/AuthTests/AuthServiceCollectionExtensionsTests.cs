using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskMaster.API.Auth;

namespace TaskMaster.Test.UnitTests.APITests.AuthTests;

public class AuthServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfig(params (string Key, string Value)[] settings)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var (key, value) in settings)
        {
            dict[key] = value;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Test]
    public void AddTaskMasterAuth_WhenModeIsNone_ShouldRegisterNoAuthScheme()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = BuildConfig(("Auth:Mode", "None"));

        // Act
        services.AddTaskMasterAuth(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var defaultScheme = schemeProvider.GetDefaultAuthenticateSchemeAsync().Result;

        Assert.That(defaultScheme, Is.Not.Null);
        Assert.That(defaultScheme!.Name, Is.EqualTo("NoAuth"));
    }

    [Test]
    public void AddTaskMasterAuth_WhenModeIsOidc_ShouldRegisterBearerScheme()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = BuildConfig(
            ("Auth:Mode", "Oidc"),
            ("Auth:Oidc:Authority", "https://idp.example.com"));

        // Act
        services.AddTaskMasterAuth(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var defaultScheme = schemeProvider.GetDefaultAuthenticateSchemeAsync().Result;

        Assert.That(defaultScheme, Is.Not.Null);
        Assert.That(defaultScheme!.Name, Is.EqualTo("Bearer"));
    }

    [Test]
    public void AddTaskMasterAuth_WhenModeIsOidcWithoutAuthority_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = BuildConfig(("Auth:Mode", "Oidc"));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddTaskMasterAuth(config));

        // Assert
        Assert.That(exception!.Message, Does.Contain("Authority"));
    }

    [Test]
    public void AddTaskMasterAuth_ShouldRegisterPoliciesForEveryScope()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = BuildConfig(("Auth:Mode", "None"));

        // Act
        services.AddTaskMasterAuth(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var authorizationOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>();

        foreach (var scope in AuthPermissions.All)
        {
            var policy = authorizationOptions.Value.GetPolicy(AuthPolicyNames.PermissionPolicy(scope));
            Assert.That(policy, Is.Not.Null, $"Policy for scope '{scope}' not registered");
        }
    }

    [Test]
    public void AddTaskMasterAuth_ShouldSetFallbackPolicyRequiringAuthentication()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = BuildConfig(("Auth:Mode", "None"));

        // Act
        services.AddTaskMasterAuth(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var authorizationOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>();

        Assert.That(authorizationOptions.Value.FallbackPolicy, Is.Not.Null);
    }
}