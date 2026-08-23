using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;

namespace TaskMaster.API.Auth.NoAuth;

public class NoAuthAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly string DummyUserName = "TaskMasterUser";

    public NoAuthAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {

    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name, DummyUserName),
        };

        // All claims are added to dummy user for no auth
        foreach(var permission in AuthPermissions.All)
        {
            claims.Add(new Claim(AuthClaimTypes.Permission, permission));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}