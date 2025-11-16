using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Pigmemento.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        // Try standard NameIdentifier (because ASP.NET maps "sub" into this)
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (Guid.TryParse(id, out var guid))
            return guid;

        return null; // Not authenticated or invalid
    }
}