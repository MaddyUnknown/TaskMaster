namespace TaskMaster.API.Models.Auth;

public class OidcAuthConfigResponse
{
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ScopeClaim { get; set; } = string.Empty;
}