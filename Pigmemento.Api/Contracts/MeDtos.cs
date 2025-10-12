namespace Pigmemento.Api.Contracts;

public class ProgressDto
{
  public double Accuracy { get; set; }
  public double Sensitivity { get; set; }
  public double Specificity { get; set; }
  public double AvgTimeMs { get; set; }
  public int TotalAttempts { get; set; }
  public int StreakDays { get; set; }
  public List<TrendPoint> Trend { get; set; } = new();
}

public class TrendPoint
{
  public DateOnly Date { get; set; }
  public double Accuracy { get; set; }
  public double Sensitivity { get; set; }
}

public class RecentAttemptDto
{
  public Guid Id { get; set; }
  public Guid CaseId { get; set; }
  public bool Correct { get; set; }
  public string Answer { get; set; } = default!; // 'benign' | 'malignant'
  public DateTime CreatedAt { get; set; }
  public int TimeToAnswerMs { get; set; }
}

public class DrillsDueDto
{
  public int Count { get; set; }
  public DateTime? NextDueAt { get; set; }
}

public class AttemptsTodayDto
{
  public int Limit { get; set; }
  public int Used { get; set; }
  public int Remaining { get; set; }
  public string ResetAtLocal { get; set; } = default!;
  public string ResetAtUtc { get; set; } = default!;
}