using TaskMaster.API.Auth.NoAuth;
using TaskMaster.API.Auth.Oidc;
using TaskMaster.API.Configs;

namespace TaskMaster.API.Auth;

public static class AuthDependencyFactory
{
    public static IAuthDependency Create(AuthMode mode) => mode switch
    {
        AuthMode.None => new NoAuthDependency(),
        AuthMode.Oidc => new OidcDependency(),
        _ => throw new InvalidOperationException($"Unsupported auth mode: {mode}.")
    };
}
