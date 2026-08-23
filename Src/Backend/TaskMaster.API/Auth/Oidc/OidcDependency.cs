using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TaskMaster.API.Configs;

namespace TaskMaster.API.Auth.Oidc;

public class OidcDependency : IAuthDependency
{
    public void AddAuthentication(IServiceCollection services, AuthConfig authConfig)
    {
        services
            .AddAuthentication(AuthPolicyNames.JwtBearer)
            .AddJwtBearer(AuthPolicyNames.JwtBearer, options =>
            {
                options.Authority = authConfig.Oidc.Authority;
                options.RequireHttpsMetadata = authConfig.Oidc.RequireHttpsMetadata;
                options.MapInboundClaims = false;

                if (!string.IsNullOrWhiteSpace(authConfig.Oidc.Audience))
                {
                    options.Audience = authConfig.Oidc.Audience;
                }

                //if (!string.IsNullOrWhiteSpace(authConfig.Oidc.MetadataAddress))
                //{
                //    options.MetadataAddress = authConfig.Oidc.MetadataAddress;
                //}

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = authConfig.Oidc.NameClaim,
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            NormalizeOidcClaims(identity, authConfig.Oidc);
                        }

                        return Task.CompletedTask;
                    },
                };
            });
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

    private static void NormalizeOidcClaims(ClaimsIdentity identity, OidcAuthConfig oidcConfig)
    {
        var scopeClaims = identity.FindAll(oidcConfig.ScopeClaim).ToArray();
        if (scopeClaims.Length == 0) return;

        // OIDC scopes are colon-separated and space-delimited within a claim.
        // Translate them into TaskMaster permissions (dot format) before authorization.
        var rawScopes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var claim in scopeClaims)
        {
            if (string.IsNullOrWhiteSpace(claim.Value)) continue;

            foreach (var scope in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                rawScopes.Add(scope);
            }
        }

        // Remove old scope claims
        foreach (var claim in scopeClaims)
        {
            identity.RemoveClaim(claim);
        }

        // Add one permission claim per granted TaskMaster permission
        foreach (var permission in OidcScopeMapper.TranslateScopes(rawScopes))
        {
            identity.AddClaim(new Claim(AuthClaimTypes.Permission, permission));
        }
    }
}
