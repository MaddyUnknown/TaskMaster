namespace TaskMaster.Library.Common.Models.Auth
{
    public class OidcAuthConfigDetails
    {
        public string Authority { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public string ScopeClaim { get; set; } = "scope";
    }
}