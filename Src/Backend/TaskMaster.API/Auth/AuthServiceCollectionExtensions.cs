using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;

namespace TaskMaster.API.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddTaskMasterAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var authConfig = configuration.GetSection(AuthConfig.SectionName).Get<AuthConfig>() ?? AuthConfig.Empty;
        ValidateConfiguration(authConfig);

        services.AddSingleton(Options.Create(authConfig));

        var authDependency = AuthDependencyFactory.Create(authConfig.Mode);
        authDependency.AddAuthentication(services, authConfig);
        authDependency.AddAuthorization(services, authConfig);

        return services;
    }

    private static void ValidateConfiguration(AuthConfig authConfig)
    {
        if (authConfig.Mode == AuthMode.Oidc && string.IsNullOrWhiteSpace(authConfig.Oidc.Authority))
        {
            throw new InvalidOperationException($"Auth mode is '{AuthMode.Oidc.ToString()}' but '{nameof(authConfig.Oidc.Authority)}' is not configured.");
        }
    }
}
