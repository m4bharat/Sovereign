namespace Sovereign.Application.Telemetry;

public sealed class SuggestionAnalyticsResponse
{
    public int TotalSuggestions { get; set; }
    public int TotalGenerated { get; set; }
    public int TotalInserted { get; set; }
    public int TotalPosted { get; set; }
    public int TotalDiscarded { get; set; }
    public int TotalRegenerated { get; set; }
    public double AcceptanceRate { get; set; }
    public double PostRate { get; set; }
    public double DiscardRate { get; set; }
    public double RegenerationRate { get; set; }
    public double AverageEditRatio { get; set; }
    public double AverageLatencyMs { get; set; }
    public double GenericComplaintRate { get; set; }
    public double WrongContextRate { get; set; }
    public double WrongToneRate { get; set; }
    public double HallucinationRate { get; set; }
    public List<SuggestionMetricBucket> BySurface { get; set; } = [];
    public List<SuggestionMetricBucket> BySituationType { get; set; } = [];
    public List<SuggestionMetricBucket> ByMove { get; set; } = [];
}

public sealed class SuggestionMetricBucket
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AcceptanceRate { get; set; }
    public double AverageEditRatio { get; set; }
    public double RegenerationRate { get; set; }
    public double AverageLatencyMs { get; set; }
}
