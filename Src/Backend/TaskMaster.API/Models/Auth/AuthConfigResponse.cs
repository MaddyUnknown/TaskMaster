using TaskMaster.API.Configs;

namespace TaskMaster.API.Models.Auth;

public class AuthConfigResponse
{
    public AuthMode Mode { get; set; } = AuthMode.None;

    public OidcAuthConfigResponse? Oidc { get; set; }
}
