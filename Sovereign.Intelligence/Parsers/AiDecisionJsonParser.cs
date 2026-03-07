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

            var result = JsonSerializer.Deserialize<AiDecision>(trimmed,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result is null || string.IsNullOrWhiteSpace(result.Action))
                return Fallback();

            return result;
        }
        catch
        {
            return Fallback();
        }
    }

    private static AiDecision Fallback() => new()
    {
        Action = "no_action",
        Reply = string.Empty,
        MemoryKey = string.Empty,
        MemoryValue = string.Empty,
        Summary = string.Empty,
        Confidence = 0.0
    };
}
