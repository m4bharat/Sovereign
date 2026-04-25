using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sovereign.Intelligence.Configuration;
using Sovereign.Intelligence.DecisionV2;

namespace Sovereign.Intelligence.Clients;

public sealed class OpenAiLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiLlmClient(HttpClient httpClient, IOptions<OpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return "{}";

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = BuildRequest(prompt);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxAttempts)
                    return "{}";

                await Task.Delay(GetRetryDelay(response, attempt), ct);
                continue;
            }

            if (!response.IsSuccessStatusCode)
                return "{}";

            var json = await response.Content.ReadAsStringAsync(ct);
            return ExtractChoiceContent(json);
        }

        return "{}";
    }

    public async Task<DecisionV2Result> CompleteDecisionV2Async(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return SafeDecisionResult("OpenAI api key missing");

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = BuildRequest(prompt);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxAttempts)
                    return SafeDecisionResult("OpenAI rate limited");

                await Task.Delay(GetRetryDelay(response, attempt), ct);
                continue;
            }

            if (!response.IsSuccessStatusCode)
                return SafeDecisionResult("OpenAI request failed");

            var json = await response.Content.ReadAsStringAsync(ct);
            return ParseDecisionResponse(json);
        }

        return SafeDecisionResult("OpenAI request failed unexpectedly");
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default)
    {
        using var request = BuildRequest(prompt);
        request.Headers.Add("Accept", "text/event-stream");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await foreach (var line in ParseStream(response.Content.ReadAsStream(ct)))
        {
            yield return line;
        }
    }

    private HttpRequestMessage BuildRequest(string prompt)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = "You are a strict JSON decision engine." },
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            max_tokens = 300
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var retryAfter = values.FirstOrDefault();
            if (int.TryParse(retryAfter, out var seconds) && seconds > 0)
                return TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 15));
    }

    private async IAsyncEnumerable<string> ParseStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line is not null)
                yield return line;
        }
    }

    private static DecisionV2Result ParseDecisionResponse(string json)
    {
        var content = ExtractChoiceContent(json);
        if (string.IsNullOrWhiteSpace(content))
            return SafeDecisionResult("Failed to parse OpenAI response");

        var extractedJson = TryExtractJsonObject(content);
        if (string.IsNullOrWhiteSpace(extractedJson))
        {
            return new DecisionV2Result
            {
                Reply = content.Trim(),
                Confidence = 0.0,
                Rationale = "Parsed plain-text response",
                Alternatives = new List<string>()
            };
        }

        try
        {
            using var contentDoc = JsonDocument.Parse(extractedJson);
            return ParseDecisionResult(contentDoc.RootElement);
        }
        catch
        {
            return new DecisionV2Result
            {
                Reply = content.Trim(),
                Confidence = 0.0,
                Rationale = "Failed to parse structured response",
                Alternatives = new List<string>()
            };
        }
    }

    private static string ExtractChoiceContent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choicesElement) &&
                choicesElement.ValueKind == JsonValueKind.Array &&
                choicesElement.GetArrayLength() > 0)
            {
                var firstChoice = choicesElement[0];

                if (firstChoice.TryGetProperty("message", out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.Object &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    if (contentElement.ValueKind == JsonValueKind.String)
                        return contentElement.GetString() ?? "{}";

                    if (contentElement.ValueKind == JsonValueKind.Array)
                    {
                        var parts = contentElement.EnumerateArray()
                            .Select(part =>
                                part.ValueKind == JsonValueKind.Object &&
                                part.TryGetProperty("text", out var textElement) &&
                                textElement.ValueKind == JsonValueKind.String
                                    ? textElement.GetString()
                                    : null)
                            .Where(part => !string.IsNullOrWhiteSpace(part));

                        var combined = string.Concat(parts);
                        if (!string.IsNullOrWhiteSpace(combined))
                            return combined;
                    }
                }
            }

            if (root.ValueKind == JsonValueKind.String)
                return root.GetString() ?? "{}";
        }
        catch
        {
            return TryExtractJsonObject(json) ?? json;
        }

        return TryExtractJsonObject(json) ?? json;
    }

    private static string? TryExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var start = text.IndexOf('{');
        if (start < 0)
            return null;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < text.Length; i++)
        {
            var ch = text[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                escaped = true;
                continue;
            }

            if (ch == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (ch == '{')
                depth++;
            else if (ch == '}')
                depth--;

            if (depth == 0)
                return text[start..(i + 1)];
        }

        return null;
    }

    private static DecisionV2Result ParseDecisionResult(JsonElement root)
    {
        var reply = TryGetString(root, "reply", "message", "content");
        var confidence = TryGetDouble(root, "confidence");
        var rationale = TryGetString(root, "brief_rationale", "rationale", "reason");
        var alternatives = TryGetStringArray(root, "alternative_rewrites", "alternatives");

        return new DecisionV2Result
        {
            Reply = reply,
            Confidence = confidence,
            Rationale = rationale,
            Alternatives = alternatives
        };
    }

    private static string TryGetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
                return property.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static double TryGetDouble(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var property) && property.TryGetDouble(out var value))
                return value;
        }

        return 0.0;
    }

    private static List<string> TryGetStringArray(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Array)
            {
                return property
                    .EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? string.Empty : string.Empty)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
        }

        return new List<string>();
    }

    private static DecisionV2Result SafeDecisionResult(string rationale)
    {
        return new DecisionV2Result
        {
            Reply = string.Empty,
            Confidence = 0.0,
            Rationale = rationale,
            Alternatives = new List<string>()
        };
    }
}
