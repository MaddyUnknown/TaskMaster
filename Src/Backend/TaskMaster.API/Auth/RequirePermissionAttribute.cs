using Microsoft.AspNetCore.Authorization;

namespace TaskMaster.API.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = AuthPolicyNames.PermissionPolicy(permission);
    }
}