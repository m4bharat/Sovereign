using System.Text.Json;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Parsers;

public sealed class AiDecisionJsonParser
{
    public AiDecision Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new AiDecision
            {
                Action = AiDecision.ReplyAction,
                Reply = string.Empty,
                Confidence = 0.0
            };
        }

        var json = ExtractJsonObject(raw);

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            return new AiDecision
            {
                Action = GetString(root, "action", AiDecision.ReplyAction),
                Reply = GetString(root, "reply"),
                MemoryKey = GetString(root, "memoryKey"),
                MemoryValue = GetString(root, "memoryValue"),
                Summary = GetString(root, "summary"),
                Confidence = GetDouble(root, "confidence", 0.55)
            };
        }
        catch
        {
            return new AiDecision
            {
                Action = AiDecision.ReplyAction,
                Reply = raw.Trim(),
                Summary = string.Empty,
                Confidence = 0.45
            };
        }
    }

    private static string ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        if (start >= 0 && end > start)
        {
            return raw[start..(end + 1)];
        }

        return raw;
    }

    private static string GetString(JsonElement root, string propertyName, string defaultValue = "")
    {
        if (root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? defaultValue;
        }

        return defaultValue;
    }

    private static double GetDouble(JsonElement root, string propertyName, double defaultValue)
    {
        if (root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            if (value.TryGetDouble(out var result))
            {
                return result;
            }
        }

        return defaultValue;
    }
}
