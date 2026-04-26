using FluentAssertions;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Telemetry;
using Sovereign.Domain.Entities;
using Xunit;

namespace Sovereign.Tests.Services;

public sealed class TelemetryServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ComputesLifecycleMetricsAcrossSuggestions()
    {
        var repository = new InMemoryTelemetryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new TelemetryService(repository, unitOfWork);

        var suggestionA = Guid.NewGuid();
        var suggestionB = Guid.NewGuid();

        await service.TrackEventAsync(new TrackSuggestionEventRequest
        {
            UserId = "user-001",
            SuggestionId = suggestionA,
            EventType = "suggestion_generated",
            Surface = "feed_reply",
            SituationType = "industry_news",
            Move = "affirm_then_extend",
            Reply = "Original reply",
            LatencyMs = 800
        }, CancellationToken.None);

        await service.TrackEventAsync(new TrackSuggestionEventRequest
        {
            UserId = "user-001",
            SuggestionId = suggestionA,
            EventType = "suggestion_inserted",
            Surface = "feed_reply",
            Reply = "Original reply",
            Accepted = true
        }, CancellationToken.None);

        await service.TrackEventAsync(new TrackSuggestionEventRequest
        {
            UserId = "user-001",
            SuggestionId = suggestionA,
            EventType = "suggestion_edited",
            Surface = "feed_reply",
            Reply = "Original reply",
            EditedReply = "Edited reply"
        }, CancellationToken.None);

        await service.TrackEventAsync(new TrackSuggestionEventRequest
        {
            UserId = "user-001",
            SuggestionId = suggestionA,
            EventType = "suggestion_posted",
            Surface = "feed_reply",
            Reply = "Original reply",
            EditedReply = "Edited reply",
            Posted = true
        }, CancellationToken.None);

        await service.TrackEventAsync(new TrackSuggestionEventRequest
        {
            UserId = "user-001",
            SuggestionId = suggestionB,
            EventType = "suggestion_generated",
            Surface = "messaging_chat",
            SituationType = "direct_message",
            Move = "nudge_forward",
            Reply = "Second reply",
            LatencyMs = 1200
        }, CancellationToken.None);

        await service.TrackEventAsync(new TrackSuggestionEventRequest
        {
            UserId = "user-001",
            SuggestionId = suggestionB,
            EventType = "suggestion_regenerated",
            Surface = "messaging_chat",
            Regenerated = true
        }, CancellationToken.None);

        await service.TrackEventAsync(new TrackSuggestionEventRequest
        {
            UserId = "user-001",
            SuggestionId = suggestionB,
            EventType = "suggestion_discarded",
            Surface = "messaging_chat"
        }, CancellationToken.None);

        await service.SubmitFeedbackAsync(new SubmitSuggestionFeedbackRequest
        {
            SuggestionId = suggestionB,
            UserId = "user-001",
            WasGeneric = true,
            Hallucinated = true
        }, CancellationToken.None);

        var summary = await service.GetSummaryAsync(CancellationToken.None);

        summary.TotalSuggestions.Should().Be(2);
        summary.TotalGenerated.Should().Be(2);
        summary.TotalInserted.Should().Be(1);
        summary.TotalPosted.Should().Be(1);
        summary.TotalDiscarded.Should().Be(1);
        summary.TotalRegenerated.Should().Be(1);
        summary.AcceptanceRate.Should().Be(0.5);
        summary.PostRate.Should().Be(0.5);
        summary.DiscardRate.Should().Be(0.5);
        summary.RegenerationRate.Should().Be(0.5);
        summary.AverageLatencyMs.Should().Be(1000);
        summary.GenericComplaintRate.Should().Be(1);
        summary.HallucinationRate.Should().Be(1);
        summary.BySurface.Should().Contain(x => x.Name == "feed_reply" && x.AcceptanceRate == 1);
        summary.BySurface.Should().Contain(x => x.Name == "messaging_chat" && x.RegenerationRate == 1);
    }

    private sealed class InMemoryTelemetryRepository : ITelemetryRepository
    {
        private readonly List<SuggestionEvent> _events = [];
        private readonly List<SuggestionFeedback> _feedback = [];
        private readonly List<SuggestionSnapshot> _snapshots = [];

        public Task AddEventAsync(SuggestionEvent suggestionEvent, CancellationToken ct = default)
        {
            _events.Add(suggestionEvent);
            return Task.CompletedTask;
        }

        public Task AddFeedbackAsync(SuggestionFeedback feedback, CancellationToken ct = default)
        {
            _feedback.Add(feedback);
            return Task.CompletedTask;
        }

        public Task<SuggestionSnapshot?> GetSnapshotBySuggestionIdAsync(Guid suggestionId, CancellationToken ct = default)
            => Task.FromResult(_snapshots.FirstOrDefault(x => x.SuggestionId == suggestionId));

        public Task AddSnapshotAsync(SuggestionSnapshot snapshot, CancellationToken ct = default)
        {
            _snapshots.Add(snapshot);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SuggestionEvent>> ListEventsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SuggestionEvent>>(_events.ToList());

        public Task<IReadOnlyList<SuggestionFeedback>> ListFeedbackAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SuggestionFeedback>>(_feedback.ToList());
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    }
}
