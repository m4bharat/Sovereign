namespace Sovereign.Application.Telemetry;

public interface ITelemetryService
{
    Task TrackEventAsync(TrackSuggestionEventRequest request, CancellationToken cancellationToken);
    Task SubmitFeedbackAsync(SubmitSuggestionFeedbackRequest request, CancellationToken cancellationToken);
    Task<SuggestionAnalyticsResponse> GetSummaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SuggestionMetricBucket>> GetBySurfaceAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SuggestionMetricBucket>> GetBySituationAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SuggestionMetricBucket>> GetByMoveAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SuggestionFailureResponse>> GetRecentFailuresAsync(CancellationToken cancellationToken);
}
