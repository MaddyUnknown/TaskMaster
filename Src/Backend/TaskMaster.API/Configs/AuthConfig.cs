namespace TaskMaster.API.Configs;

public enum AuthMode
{
    None,
    Oidc
}

public class AuthConfig
{
    public AuthMode Mode { get; set; } = AuthMode.None;

    public OidcAuthConfig Oidc { get; set; } = new();

    public static string SectionName => "Auth";
    public static AuthConfig Empty => new();
}

public class OidcAuthConfig
{
    public string Authority { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    //public string MetadataAddress { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;

    public string ScopeClaim { get; set; } = "scope";

    public string NameClaim { get; set; } = "sub";
}