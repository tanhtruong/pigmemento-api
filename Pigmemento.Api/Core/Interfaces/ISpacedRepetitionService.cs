namespace Pigmemento.Api.Core.Interfaces;

public interface ISpacedRepetitionService
{
    Task UpdateUserCaseStatsAsync(Guid userId, Guid caseId, bool correct, long latencyMs, CancellationToken ct);
    Task MarkSeenAsync(Guid userId, IEnumerable<Guid> caseIds, CancellationToken ct);
}