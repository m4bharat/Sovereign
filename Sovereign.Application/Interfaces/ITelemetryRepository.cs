using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface ITelemetryRepository
{
    Task AddEventAsync(SuggestionEvent suggestionEvent, CancellationToken ct = default);
    Task AddFeedbackAsync(SuggestionFeedback feedback, CancellationToken ct = default);
    Task<SuggestionSnapshot?> GetSnapshotBySuggestionIdAsync(Guid suggestionId, CancellationToken ct = default);
    Task AddSnapshotAsync(SuggestionSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<SuggestionEvent>> ListEventsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SuggestionFeedback>> ListFeedbackAsync(CancellationToken ct = default);
}
