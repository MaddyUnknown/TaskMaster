using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Configs;

namespace TaskMaster.API.Auth.NoAuth;

public class NoAuthDependency : IAuthDependency
{
    public void AddAuthentication(IServiceCollection services, AuthConfig authConfig)
    {
        services
            .AddAuthentication(AuthPolicyNames.NoAuth)
            .AddScheme<AuthenticationSchemeOptions, NoAuthAuthenticationHandler>(AuthPolicyNames.NoAuth, _ => { });
    }

    public void AddAuthorization(IServiceCollection services, AuthConfig authConfig)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in AuthPermissions.All)
            {
                options.AddPolicy(
                    AuthPolicyNames.PermissionPolicy(permission),
                    policy => policy.RequireClaim(AuthClaimTypes.Permission, permission));
            }

            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });
    }
}
