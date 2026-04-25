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
            throw new InvalidOperationException("OpenAI:ApiKey is missing.");

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = BuildRequest(prompt);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxAttempts)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    throw new HttpRequestException($"OpenAI rate limit hit after {maxAttempts} attempts. Body: {body}", null, response.StatusCode);
                }

                await Task.Delay(GetRetryDelay(response, attempt), ct);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"OpenAI request failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}", null, response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";
        }

        throw new InvalidOperationException("OpenAI request failed unexpectedly.");
    }

    public async Task<DecisionV2Result> CompleteDecisionV2Async(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenAI:ApiKey is missing.");

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = BuildRequest(prompt);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxAttempts)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    throw new HttpRequestException($"OpenAI rate limit hit after {maxAttempts} attempts. Body: {body}", null, response.StatusCode);
                }

                await Task.Delay(GetRetryDelay(response, attempt), ct);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"OpenAI request failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}", null, response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            return ParseDecisionResult(doc.RootElement);
        }

        throw new InvalidOperationException("OpenAI request failed unexpectedly.");
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
            yield return await reader.ReadLineAsync();
        }
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
}
