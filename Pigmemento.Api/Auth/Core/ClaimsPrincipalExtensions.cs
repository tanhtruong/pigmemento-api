using System.Security.Claims;
using Npgsql.Replication;

namespace Pigmemento.Api.Auth.Core;

public static class ClaimsPrincipalExtensions
{
  public static Guid GetUserId(this ClaimsPrincipal user)
  {
    var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    return Guid.Parse(id!);
  }

  public static string GetUserRole(this ClaimsPrincipal user)
  {
    var role = user.FindFirstValue(ClaimTypes.Role) ?? "user";
    return role;
  }
}