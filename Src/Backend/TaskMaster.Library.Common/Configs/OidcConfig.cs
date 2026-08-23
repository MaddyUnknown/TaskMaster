namespace TaskMaster.Library.Common.Configs
{
    public class OidcConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RequestedScope { get; set; } = string.Empty;
    }
}