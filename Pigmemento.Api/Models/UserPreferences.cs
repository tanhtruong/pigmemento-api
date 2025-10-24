namespace Pigmemento.Api.Models;

public class UserPreferences
{
    public Guid UserId { get; set; }
    public int DailyTarget { get; set; } = 20;
    public bool ExplorationEnabled { get; set; } = true;
    public double ExplorationRate { get; set; } = 0.15; // 15%
    public double RecentlyWrongRate { get; set; } = 0.25;
}