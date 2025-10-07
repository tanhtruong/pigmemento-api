using System.Security.Claims;

namespace Pigmemento.Api.Auth.Core;

public static class ClaimsPrincipalExtensions {
  public static Guid GetUserId(this ClaimsPrincipal user) {
    var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    return Guid.Parse(id!);
  }
}