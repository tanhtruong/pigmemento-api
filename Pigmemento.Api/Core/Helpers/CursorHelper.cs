using System.Text;

namespace Pigmemento.Api.Core.Helpers;


public static class CursorHelper
{
    public static string EncodeCursor(DateTime createdAtUtc, Guid id)
    {
        // Use ticks for precision; ensure UTC
        var ticks = createdAtUtc.Kind == DateTimeKind.Utc ? createdAtUtc.Ticks : createdAtUtc.ToUniversalTime().Ticks;
        var raw = $"{ticks}:{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static (DateTime?, Guid?) TryDecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return (null, null);
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(':', 2);
            if (parts.Length != 2) return (null, null);
            if (!long.TryParse(parts[0], out var ticks)) return (null, null);
            if (!Guid.TryParse(parts[1], out var id)) return (null, null);
            return (new DateTime(ticks, DateTimeKind.Utc), id);
        }
        catch
        {
            return (null, null);
        }
    }
}
