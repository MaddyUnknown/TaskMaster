namespace TaskMaster.Library.Common.Models.Auth
{
    public class AuthConfigDetails
    {
        public string Mode { get; set; } = string.Empty;

        public OidcAuthConfigDetails? Oidc { get; set; }
    }
}