using TaskMaster.API.Auth;
using TaskMaster.API.Auth.Oidc;

namespace TaskMaster.Test.UnitTests.APITests.AuthTests;

public class OidcScopeMapperTests
{
    [Test]
    public void TryGetPermission_WhenKnownScope_ReturnsMappedPermission()
    {
        var found = OidcScopeMapper.TryGetPermission("jobs:read", out var permission);

        Assert.That(found, Is.True);
        Assert.That(permission, Is.EqualTo(AuthPermissions.ReadJobs));
    }

    [Test]
    public void TryGetPermission_WhenUnknownScope_ReturnsFalse()
    {
        var found = OidcScopeMapper.TryGetPermission("foo:bar", out _);

        Assert.That(found, Is.False);
    }

    [Test]
    public void TryGetPermission_WhenDifferentCase_DoesNotMatch()
    {
        var found = OidcScopeMapper.TryGetPermission("Jobs:Read", out _);

        Assert.That(found, Is.False);
    }

    [Test]
    public void TranslateScopes_WhenMixedKnownAndUnknown_ReturnsOnlyPermissions()
    {
        var result = OidcScopeMapper.TranslateScopes(new[] { "jobs:read", "unknown:scope", "workers:remove" });

        Assert.That(result, Is.EquivalentTo(new[] { AuthPermissions.ReadJobs, AuthPermissions.RemoveWorker }));
    }

    [Test]
    public void TranslateScopes_WhenDuplicates_ReturnsDistinctPermissions()
    {
        var result = OidcScopeMapper.TranslateScopes(new[] { "jobs:read", "jobs:read" });

        Assert.That(result, Has.Exactly(1).Items);
    }

    [Test]
    public void TranslateScopes_WhenEmpty_ReturnsEmpty()
    {
        var result = OidcScopeMapper.TranslateScopes(Array.Empty<string>());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Mappings_ShouldCoverEveryPermissionExactlyOnce()
    {
        var mappedPermissions = OidcScopeMapper.Mappings.Values;

        Assert.That(mappedPermissions, Is.EquivalentTo(AuthPermissions.All));
    }
}
