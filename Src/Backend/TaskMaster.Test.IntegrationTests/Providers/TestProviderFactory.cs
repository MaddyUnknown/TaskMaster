using TaskMaster.API.Enums;

namespace TaskMaster.Test.IntegrationTests.Providers;

public static class TestProviderFactory
{
    public const string ProviderEnvironmentVariable = "TASKMASTER_TEST_PROVIDER";
    public const string ProviderConfigurationKey = "TestDatabase:Provider";

    public static ITestProvider Create(string? name) =>
        Enum.TryParse<DatabaseProviderEnum>(name, ignoreCase: true, out var provider)
            ? Create(provider)
            : throw new ArgumentException(
                $"'{name}' is not a supported test database provider. Use '{DatabaseProviderEnum.SqlServer}' or '{DatabaseProviderEnum.PostgreSql}'.");

    public static ITestProvider Create(DatabaseProviderEnum provider) => provider switch
    {
        DatabaseProviderEnum.SqlServer => new SqlServerTestProvider(),
        DatabaseProviderEnum.PostgreSql => new PostgreSqlTestProvider(),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported test database provider.")
    };

    public static ITestProvider CreateForCurrentRun(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var name = Environment.GetEnvironmentVariable(ProviderEnvironmentVariable) ?? configuration[ProviderConfigurationKey];

        return Create(string.IsNullOrWhiteSpace(name) ? nameof(DatabaseProviderEnum.SqlServer) : name);
    }
}
