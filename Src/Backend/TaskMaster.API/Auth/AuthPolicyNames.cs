namespace TaskMaster.API.Auth;

public static class AuthPolicyNames
{
    public const string NoAuth = "NoAuth";
    public const string JwtBearer = "Bearer";

    public static string PermissionPolicy(string permission) => $"RequirePermission:{permission}";
}