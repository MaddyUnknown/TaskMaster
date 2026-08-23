using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Configs;

namespace TaskMaster.API.Auth;

public interface IAuthDependency
{
    void AddAuthentication(IServiceCollection services, AuthConfig authConfig);

    void AddAuthorization(IServiceCollection services, AuthConfig authConfig);
}
