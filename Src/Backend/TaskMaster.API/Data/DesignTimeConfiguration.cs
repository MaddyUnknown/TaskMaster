using Microsoft.Extensions.Configuration;

namespace TaskMaster.API.Data
{
    /// <summary>
    /// Configuration loading shared by the design time context factories, mirroring how the
    /// host loads configuration so scaffolded migrations see the same connection strings.
    /// </summary>
    internal static class DesignTimeConfiguration
    {
        public static IConfigurationRoot Load()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets(typeof(ApplicationDbContext).Assembly, optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
