using TaskMaster.API.Enums;

namespace TaskMaster.Test.IntegrationTests.Providers;

public static class TestProviderFactory
{
    public const string ProviderConfigurationKey = "Database:Provider";

    public static ITestProvider CreateForCurrentRun(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var providerName = configuration[ProviderConfigurationKey];
        return Create(providerName ?? string.Empty);
    }

    private static ITestProvider Create(string providerName)
    {
        if (Enum.TryParse(providerName, true, out DatabaseProviderEnum provider))
        {
            switch (provider)
            {
                case DatabaseProviderEnum.SqlServer: return new SqlServerTestProvider();
                case DatabaseProviderEnum.PostgreSql: return new PostgreSqlTestProvider();
                default: throw new ArgumentOutOfRangeException(nameof(providerName), provider, "Unsupported test database provider.");
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(providerName), providerName, "Unsupported test database provider.");
        }
    }
}
