namespace TaskMaster.API.Auth.Oidc;

public static class OidcScopeMapper
{
    private static readonly Dictionary<string, string> ScopeToPermission = new(StringComparer.Ordinal)
    {
        ["jobs:read"] = AuthPermissions.ReadJobs,
        ["jobs:stats"] = AuthPermissions.ReadJobStats,
        ["jobs:create"] = AuthPermissions.CreateJob,
        ["jobs:pull"] = AuthPermissions.PullJobs,
        ["jobs:report"] = AuthPermissions.ReportJobs,

        ["workers:read"] = AuthPermissions.ReadWorkers,
        ["workers:stats"] = AuthPermissions.ReadWorkerStats,
        ["workers:register"] = AuthPermissions.RegisterWorkers,
        ["workers:heartbeat"] = AuthPermissions.WorkersHeartbeat,
        ["workers:remove"] = AuthPermissions.RemoveWorker,

        ["jobtypes:read"] = AuthPermissions.ReadJobTypes,
        ["jobtypes:create"] = AuthPermissions.CreateJobTypes,

        ["dashboard:read"] = AuthPermissions.ReadDashboard
    };

    public static IReadOnlyDictionary<string, string> Mappings => ScopeToPermission;

    public static bool TryGetPermission(string scope, out string permission)
    {
        return ScopeToPermission.TryGetValue(scope, out permission!);
    }

    public static IReadOnlyCollection<string> TranslateScopes(IEnumerable<string> scopes)
    {
        var permissions = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var scope in scopes)
        {
            if (TryGetPermission(scope, out var permission))
            {
                permissions.Add(permission);
            }
        }

        return permissions.ToArray();
    }
}
