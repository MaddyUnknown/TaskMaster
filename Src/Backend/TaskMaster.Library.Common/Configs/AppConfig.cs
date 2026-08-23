namespace TaskMaster.Library.Common.Configs
{
    public class AppConfig
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public AuthConfig Auth { get; set; } = AuthConfig.Empty;
    }
}