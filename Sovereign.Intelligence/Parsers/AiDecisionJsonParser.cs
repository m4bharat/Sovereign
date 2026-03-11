using System.Text.Json;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Parsers;

public sealed class AiDecisionJsonParser
{
    public AiDecision Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Fallback();

        try
        {
            var trimmed = raw.Trim();
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
                trimmed = trimmed[start..(end + 1)];

            var result = JsonSerializer.Deserialize<AiDecision>(trimmed, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Validate(result);
        }
        catch
        {
            return Fallback();
        }
    }

    private static AiDecision Validate(AiDecision? result)
    {
        if (result is null || string.IsNullOrWhiteSpace(result.Action) || !AiDecision.AllowedActions.Contains(result.Action))
            return Fallback();

        var confidence = Math.Clamp(result.Confidence, 0, 1);

        return result.Action.ToLowerInvariant() switch
        {
            AiDecision.ReplyAction when string.IsNullOrWhiteSpace(result.Reply) => Fallback(),
            AiDecision.SaveMemoryAction when string.IsNullOrWhiteSpace(result.MemoryKey) || string.IsNullOrWhiteSpace(result.MemoryValue) => Fallback(),
            AiDecision.SummarizeAction when string.IsNullOrWhiteSpace(result.Summary) => Fallback(),
            _ => new AiDecision
            {
                Action = result.Action.ToLowerInvariant(),
                Reply = result.Reply?.Trim() ?? string.Empty,
                MemoryKey = result.MemoryKey?.Trim() ?? string.Empty,
                MemoryValue = result.MemoryValue?.Trim() ?? string.Empty,
                Summary = result.Summary?.Trim() ?? string.Empty,
                Confidence = confidence
            }
        };
    }

    private static AiDecision Fallback() => new()
    {
        Action = AiDecision.NoAction,
        Reply = string.Empty,
        MemoryKey = string.Empty,
        MemoryValue = string.Empty,
        Summary = string.Empty,
        Confidence = 0.0
    };
}
