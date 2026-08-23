namespace TaskMaster.Library.Common.Configs
{
    public class AuthConfig
    {
        public OidcConfig? Oidc { get; set; }

        public static AuthConfig Empty => new AuthConfig();
    }
}