using System.Security.Claims;

namespace Pigmemento.Api.Core.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? user.FindFirst("sub"); // fallback for JWT 'sub' claim

        if (userIdClaim == null)
            throw new InvalidOperationException("User ID claim not found.");

        return Guid.Parse(userIdClaim.Value);
    }
}
