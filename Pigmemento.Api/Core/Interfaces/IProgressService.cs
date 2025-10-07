using Pigmemento.Api.Contracts;

namespace Pigmemento.Api.Core.Interfaces;

public interface IProgressService {
  Task<ProgressDto> GetProgressAsync(Guid userId, CancellationToken ct);
  Task<List<RecentAttemptDto>> GetRecentAttemptsAsync(Guid userId, int limit, CancellationToken ct);
  Task<DrillsDueDto> GetDrillsDueAsync(Guid userId, CancellationToken ct);
}