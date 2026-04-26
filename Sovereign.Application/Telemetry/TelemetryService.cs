using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;

namespace Sovereign.Application.Telemetry;

public sealed class TelemetryService : ITelemetryService
{
    private static readonly HashSet<string> AllowedEventTypes =
    [
        "suggestion_requested",
        "suggestion_generated",
        "suggestion_inserted",
        "suggestion_edited",
        "suggestion_posted",
        "suggestion_discarded",
        "suggestion_regenerated",
        "feedback_submitted"
    ];

    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TelemetryService(ITelemetryRepository telemetryRepository, IUnitOfWork unitOfWork)
    {
        _telemetryRepository = telemetryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task TrackEventAsync(TrackSuggestionEventRequest request, CancellationToken cancellationToken)
    {
        ValidateTrackEventRequest(request);

        var normalizedMetadata = request.Metadata ?? new Dictionary<string, string>();
        var debugMode = IsDebugMode(normalizedMetadata);
        var suggestionEvent = MapEvent(request, normalizedMetadata);

        await _telemetryRepository.AddEventAsync(suggestionEvent, cancellationToken);

        if (request.SuggestionId.HasValue)
        {
            await UpsertSnapshotAsync(request, debugMode, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitFeedbackAsync(SubmitSuggestionFeedbackRequest request, CancellationToken cancellationToken)
    {
        if (request.SuggestionId == Guid.Empty)
            throw new ArgumentException("SuggestionId is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("UserId is required.", nameof(request));

        var feedback = new SuggestionFeedback
        {
            SuggestionId = request.SuggestionId,
            UserId = request.UserId.Trim(),
            Rating = request.Rating,
            FeedbackType = NormalizeText(request.FeedbackType),
            FeedbackText = NormalizeText(request.FeedbackText),
            WasUseful = request.WasUseful,
            WasGeneric = request.WasGeneric,
            WasWrongContext = request.WasWrongContext,
            WasWrongTone = request.WasWrongTone,
            WasTooLong = request.WasTooLong,
            WasTooShort = request.WasTooShort,
            Hallucinated = request.Hallucinated
        };

        await _telemetryRepository.AddFeedbackAsync(feedback, cancellationToken);
        await _telemetryRepository.AddEventAsync(new SuggestionEvent
        {
            UserId = feedback.UserId,
            SuggestionId = feedback.SuggestionId,
            EventType = "feedback_submitted",
            MetadataJson = JsonSerializer.Serialize(new
            {
                feedback.Rating,
                feedback.FeedbackType,
                feedback.WasUseful,
                feedback.WasGeneric,
                feedback.WasWrongContext,
                feedback.WasWrongTone,
                feedback.WasTooLong,
                feedback.WasTooShort,
                feedback.Hallucinated
            })
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<SuggestionAnalyticsResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var suggestions = await BuildSuggestionRollupsAsync(cancellationToken);
        var feedback = await _telemetryRepository.ListFeedbackAsync(cancellationToken);

        var generatedSuggestions = suggestions.Where(x => x.Generated).ToList();
        var denominator = generatedSuggestions.Count;

        return new SuggestionAnalyticsResponse
        {
            TotalSuggestions = suggestions.Count,
            TotalGenerated = generatedSuggestions.Count,
            TotalInserted = generatedSuggestions.Count(x => x.Inserted),
            TotalPosted = generatedSuggestions.Count(x => x.Posted),
            TotalDiscarded = generatedSuggestions.Count(x => x.Discarded),
            TotalRegenerated = generatedSuggestions.Count(x => x.Regenerated),
            AcceptanceRate = Rate(generatedSuggestions.Count(x => x.Inserted), denominator),
            PostRate = Rate(generatedSuggestions.Count(x => x.Posted), denominator),
            DiscardRate = Rate(generatedSuggestions.Count(x => x.Discarded), denominator),
            RegenerationRate = Rate(generatedSuggestions.Count(x => x.Regenerated), denominator),
            AverageEditRatio = Average(generatedSuggestions.Where(x => x.EditRatio.HasValue).Select(x => x.EditRatio!.Value)),
            AverageLatencyMs = Average(generatedSuggestions.Where(x => x.LatencyMs.HasValue).Select(x => (double)x.LatencyMs!.Value)),
            GenericComplaintRate = FeedbackRate(feedback, x => x.WasGeneric == true),
            WrongContextRate = FeedbackRate(feedback, x => x.WasWrongContext == true),
            WrongToneRate = FeedbackRate(feedback, x => x.WasWrongTone == true),
            HallucinationRate = FeedbackRate(feedback, x => x.Hallucinated == true),
            BySurface = BuildBuckets(generatedSuggestions, x => x.Surface),
            BySituationType = BuildBuckets(generatedSuggestions, x => x.SituationType),
            ByMove = BuildBuckets(generatedSuggestions, x => x.Move)
        };
    }

    public async Task<IReadOnlyList<SuggestionMetricBucket>> GetBySurfaceAsync(CancellationToken cancellationToken)
    {
        var suggestions = await BuildSuggestionRollupsAsync(cancellationToken);
        return BuildBuckets(suggestions.Where(x => x.Generated).ToList(), x => x.Surface);
    }

    public async Task<IReadOnlyList<SuggestionMetricBucket>> GetBySituationAsync(CancellationToken cancellationToken)
    {
        var suggestions = await BuildSuggestionRollupsAsync(cancellationToken);
        return BuildBuckets(suggestions.Where(x => x.Generated).ToList(), x => x.SituationType);
    }

    public async Task<IReadOnlyList<SuggestionMetricBucket>> GetByMoveAsync(CancellationToken cancellationToken)
    {
        var suggestions = await BuildSuggestionRollupsAsync(cancellationToken);
        return BuildBuckets(suggestions.Where(x => x.Generated).ToList(), x => x.Move);
    }

    public async Task<IReadOnlyList<SuggestionFailureResponse>> GetRecentFailuresAsync(CancellationToken cancellationToken)
    {
        var suggestions = await BuildSuggestionRollupsAsync(cancellationToken);

        return suggestions
            .Where(x => x.Discarded || x.Regenerated || x.Hallucinated || x.WasWrongContext || x.WasWrongTone || x.WasGeneric)
            .OrderByDescending(x => x.EventTime)
            .Take(25)
            .Select(x => new SuggestionFailureResponse
            {
                SuggestionId = x.SuggestionId,
                UserId = x.UserId,
                Surface = x.Surface,
                SituationType = x.SituationType,
                Move = x.Move,
                LatestEventType = x.LatestEventType,
                FailureReason = BuildFailureReason(x),
                EditRatio = x.EditRatio ?? 0,
                Regenerated = x.Regenerated,
                Discarded = x.Discarded,
                EventTime = x.EventTime
            })
            .ToList();
    }

    private async Task UpsertSnapshotAsync(TrackSuggestionEventRequest request, bool debugMode, CancellationToken cancellationToken)
    {
        var suggestionId = request.SuggestionId!.Value;
        var snapshot = await _telemetryRepository.GetSnapshotBySuggestionIdAsync(suggestionId, cancellationToken);

        if (snapshot is null)
        {
            snapshot = new SuggestionSnapshot
            {
                SuggestionId = suggestionId,
                UserId = request.UserId.Trim(),
                IsDebugSample = debugMode
            };

            await _telemetryRepository.AddSnapshotAsync(snapshot, cancellationToken);
        }

        snapshot.IsDebugSample = snapshot.IsDebugSample || debugMode;

        if (!debugMode)
        {
            return;
        }

        snapshot.SourceText ??= NormalizeText(request.SourceText);
        snapshot.InputMessage ??= NormalizeText(request.InputMessage);
        snapshot.GeneratedReply ??= NormalizeText(request.Reply);
        snapshot.EditedReply = NormalizeText(request.EditedReply) ?? snapshot.EditedReply;

        if (request.EventType == "suggestion_requested")
        {
            snapshot.RequestPayloadJson = JsonSerializer.Serialize(new
            {
                request.UserId,
                request.TenantId,
                request.SessionId,
                request.Platform,
                request.Surface,
                request.CurrentUrl,
                request.RequestId,
                request.SourceAuthor,
                request.SourceTitle,
                request.SourceText,
                request.InputMessage,
                request.Metadata
            });
        }

        if (request.EventType is "suggestion_generated" or "suggestion_inserted" or "suggestion_edited" or "suggestion_posted")
        {
            snapshot.ResponsePayloadJson = JsonSerializer.Serialize(new
            {
                request.SuggestionId,
                request.RequestId,
                request.SituationType,
                request.Move,
                request.Strategy,
                request.Tone,
                request.Confidence,
                request.Reply,
                request.EditedReply,
                request.LatencyMs,
                request.ModelProvider,
                request.ModelName,
                request.Metadata
            });
        }
    }

    private SuggestionEvent MapEvent(TrackSuggestionEventRequest request, Dictionary<string, string> metadata)
    {
        var reply = NormalizeText(request.Reply);
        var editedReply = NormalizeText(request.EditedReply);
        var editDistance = ComputeEditDistance(reply, editedReply);
        var maxLength = Math.Max(reply?.Length ?? 0, editedReply?.Length ?? 0);

        return new SuggestionEvent
        {
            UserId = request.UserId.Trim(),
            TenantId = NormalizeText(request.TenantId),
            SessionId = NormalizeText(request.SessionId),
            EventType = request.EventType.Trim(),
            Platform = NormalizeText(request.Platform),
            Surface = NormalizeText(request.Surface),
            CurrentUrl = NormalizeText(request.CurrentUrl),
            SuggestionId = request.SuggestionId,
            RequestId = request.RequestId,
            SituationType = NormalizeText(request.SituationType),
            Move = NormalizeText(request.Move),
            Strategy = NormalizeText(request.Strategy),
            Tone = NormalizeText(request.Tone),
            Confidence = request.Confidence,
            SourceAuthor = NormalizeText(request.SourceAuthor),
            SourceTitle = NormalizeText(request.SourceTitle),
            SourceTextHash = HashValue(request.SourceText),
            InputMessageHash = HashValue(request.InputMessage),
            ReplyHash = HashValue(editedReply ?? reply),
            ReplyLength = reply?.Length,
            EditedReplyLength = editedReply?.Length,
            EditDistance = editDistance,
            EditRatio = maxLength == 0 || editDistance is null ? null : Math.Round((double)editDistance.Value / maxLength, 4),
            LatencyMs = request.LatencyMs,
            ModelProvider = NormalizeText(request.ModelProvider),
            ModelName = NormalizeText(request.ModelName),
            Accepted = request.Accepted ?? InferAccepted(request.EventType),
            Posted = request.Posted ?? (request.EventType == "suggestion_posted"),
            Regenerated = request.Regenerated ?? (request.EventType == "suggestion_regenerated"),
            MetadataJson = metadata.Count == 0 ? null : JsonSerializer.Serialize(metadata)
        };
    }

    private async Task<List<SuggestionRollup>> BuildSuggestionRollupsAsync(CancellationToken cancellationToken)
    {
        var events = await _telemetryRepository.ListEventsAsync(cancellationToken);
        var feedback = await _telemetryRepository.ListFeedbackAsync(cancellationToken);

        var feedbackBySuggestion = feedback
            .GroupBy(x => x.SuggestionId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var groupedEvents = events
            .Where(x => x.SuggestionId.HasValue)
            .GroupBy(x => x.SuggestionId!.Value);

        var rollups = new List<SuggestionRollup>();

        foreach (var group in groupedEvents)
        {
            var ordered = group.OrderBy(x => x.EventTime).ToList();
            var generated = ordered.FirstOrDefault(x => x.EventType == "suggestion_generated");
            var inserted = ordered.LastOrDefault(x => x.EventType == "suggestion_inserted");
            var discarded = ordered.LastOrDefault(x => x.EventType == "suggestion_discarded");
            var posted = ordered.LastOrDefault(x => x.EventType == "suggestion_posted");
            var regenerated = ordered.LastOrDefault(x => x.EventType == "suggestion_regenerated");
            var edited = ordered.LastOrDefault(x => x.EventType == "suggestion_edited" && x.EditRatio.HasValue);
            var latest = ordered[^1];
            var relevantFeedback = feedbackBySuggestion.GetValueOrDefault(group.Key) ?? [];

            rollups.Add(new SuggestionRollup
            {
                SuggestionId = group.Key,
                UserId = latest.UserId,
                Surface = FirstNonEmpty(ordered.Select(x => x.Surface)) ?? "unknown",
                SituationType = FirstNonEmpty(ordered.Select(x => x.SituationType)) ?? "unknown",
                Move = FirstNonEmpty(ordered.Select(x => x.Move)) ?? "unknown",
                Generated = generated is not null,
                Inserted = inserted is not null || ordered.Any(x => x.Accepted == true),
                Posted = posted is not null || ordered.Any(x => x.Posted == true),
                Discarded = discarded is not null,
                Regenerated = regenerated is not null || ordered.Any(x => x.Regenerated == true),
                LatencyMs = generated?.LatencyMs,
                EditRatio = edited?.EditRatio ?? ordered.Where(x => x.EditRatio.HasValue).Select(x => x.EditRatio).LastOrDefault(),
                LatestEventType = latest.EventType,
                EventTime = latest.EventTime,
                WasGeneric = relevantFeedback.Any(x => x.WasGeneric == true),
                WasWrongContext = relevantFeedback.Any(x => x.WasWrongContext == true),
                WasWrongTone = relevantFeedback.Any(x => x.WasWrongTone == true),
                Hallucinated = relevantFeedback.Any(x => x.Hallucinated == true)
            });
        }

        return rollups;
    }

    private static List<SuggestionMetricBucket> BuildBuckets(
        List<SuggestionRollup> suggestions,
        Func<SuggestionRollup, string> keySelector)
    {
        return suggestions
            .GroupBy(x => string.IsNullOrWhiteSpace(keySelector(x)) ? "unknown" : keySelector(x))
            .Select(group => new SuggestionMetricBucket
            {
                Name = group.Key,
                Count = group.Count(),
                AcceptanceRate = Rate(group.Count(x => x.Inserted), group.Count()),
                AverageEditRatio = Average(group.Where(x => x.EditRatio.HasValue).Select(x => x.EditRatio!.Value)),
                RegenerationRate = Rate(group.Count(x => x.Regenerated), group.Count()),
                AverageLatencyMs = Average(group.Where(x => x.LatencyMs.HasValue).Select(x => (double)x.LatencyMs!.Value))
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static double FeedbackRate(IEnumerable<SuggestionFeedback> feedback, Func<SuggestionFeedback, bool> predicate)
    {
        var items = feedback.ToList();
        return Rate(items.Count(predicate), items.Count);
    }

    private static double Rate(int numerator, int denominator)
        => denominator == 0 ? 0 : Math.Round((double)numerator / denominator, 4);

    private static double Average(IEnumerable<double> values)
    {
        var materialized = values.ToList();
        return materialized.Count == 0 ? 0 : Math.Round(materialized.Average(), 2);
    }

    private static void ValidateTrackEventRequest(TrackSuggestionEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("UserId is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.EventType))
            throw new ArgumentException("EventType is required.", nameof(request));

        if (!AllowedEventTypes.Contains(request.EventType.Trim()))
            throw new ArgumentException($"Unsupported event type '{request.EventType}'.", nameof(request));
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool? InferAccepted(string eventType)
        => eventType switch
        {
            "suggestion_inserted" => true,
            "suggestion_posted" => true,
            "suggestion_discarded" => false,
            _ => null
        };

    private static bool IsDebugMode(IReadOnlyDictionary<string, string> metadata)
        => metadata.TryGetValue("debugMode", out var value) &&
           bool.TryParse(value, out var enabled) &&
           enabled;

    private static string? HashValue(string? value)
    {
        var normalized = NormalizeText(value);
        if (normalized is null)
            return null;

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static int? ComputeEditDistance(string? source, string? target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
        {
            if (source is null && target is null)
                return null;

            return Math.Max(source?.Length ?? 0, target?.Length ?? 0);
        }

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++)
            matrix[i, 0] = i;

        for (var j = 0; j <= target.Length; j++)
            matrix[0, j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }

    private static string? FirstNonEmpty(IEnumerable<string?> values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private static string BuildFailureReason(SuggestionRollup suggestion)
    {
        if (suggestion.Hallucinated) return "hallucination_feedback";
        if (suggestion.WasWrongContext) return "wrong_context_feedback";
        if (suggestion.WasWrongTone) return "wrong_tone_feedback";
        if (suggestion.WasGeneric) return "generic_feedback";
        if (suggestion.Discarded) return "discarded";
        if (suggestion.Regenerated) return "regenerated";
        return "needs_review";
    }

    private sealed class SuggestionRollup
    {
        public Guid SuggestionId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string Surface { get; init; } = "unknown";
        public string SituationType { get; init; } = "unknown";
        public string Move { get; init; } = "unknown";
        public bool Generated { get; init; }
        public bool Inserted { get; init; }
        public bool Posted { get; init; }
        public bool Discarded { get; init; }
        public bool Regenerated { get; init; }
        public int? LatencyMs { get; init; }
        public double? EditRatio { get; init; }
        public string LatestEventType { get; init; } = string.Empty;
        public DateTimeOffset EventTime { get; init; }
        public bool WasGeneric { get; init; }
        public bool WasWrongContext { get; init; }
        public bool WasWrongTone { get; init; }
        public bool Hallucinated { get; init; }
    }
}
