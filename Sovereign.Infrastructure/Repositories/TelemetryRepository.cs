using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public sealed class TelemetryRepository : ITelemetryRepository
{
    private readonly SovereignDbContext _dbContext;

    public TelemetryRepository(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddEventAsync(SuggestionEvent suggestionEvent, CancellationToken ct = default)
        => _dbContext.SuggestionEvents.AddAsync(suggestionEvent, ct).AsTask();

    public Task AddFeedbackAsync(SuggestionFeedback feedback, CancellationToken ct = default)
        => _dbContext.SuggestionFeedback.AddAsync(feedback, ct).AsTask();

    public async Task<SuggestionSnapshot?> GetSnapshotBySuggestionIdAsync(Guid suggestionId, CancellationToken ct = default)
        => await _dbContext.SuggestionSnapshots.FirstOrDefaultAsync(x => x.SuggestionId == suggestionId, ct);

    public Task AddSnapshotAsync(SuggestionSnapshot snapshot, CancellationToken ct = default)
        => _dbContext.SuggestionSnapshots.AddAsync(snapshot, ct).AsTask();

    public async Task<IReadOnlyList<SuggestionEvent>> ListEventsAsync(CancellationToken ct = default)
        => await _dbContext.SuggestionEvents.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<SuggestionFeedback>> ListFeedbackAsync(CancellationToken ct = default)
        => await _dbContext.SuggestionFeedback.AsNoTracking().ToListAsync(ct);
}
