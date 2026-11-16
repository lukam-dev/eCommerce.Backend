using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
namespace eCommerce.Backend;
public sealed class DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var owner = Request.Headers["X-Demo-User"].ToString();
        if (string.IsNullOrWhiteSpace(owner)) return Task.FromResult(AuthenticateResult.NoResult());
        if (owner.Length > 100 || owner.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            return Task.FromResult(AuthenticateResult.Fail("Invalid demo user."));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", owner) }, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}

