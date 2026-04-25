using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sovereign.Intelligence.Configuration;
using Sovereign.Intelligence.DecisionV2;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sovereign.Intelligence.Clients;

public sealed class OllamaLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaLlmClient(HttpClient httpClient, IOptions<OllamaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl.TrimEnd('/')}/chat");
            // --- ADD THIS LINE FOR CLOUD AUTHENTICATION ---
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
            // ----------------------------------------------
            var payload = new
            {
                model = _options.Model,
                stream = _options.Stream,
                messages = new object[]
                {
                new { role = "system", content = _options.SystemPrompt },
                new { role = "user", content = prompt }
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Ollama request failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}",
                    null,
                    response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                return contentElement.GetString() ?? "{}";
            }

            return "{}";
        }

        catch (Exception ex)
        {
            throw new Exception("Error completing prompt with Ollama", ex);
        }
    }

    public async Task<DecisionV2Result> CompleteDecisionV2Async(string prompt, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl.TrimEnd('/')}/chat");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var payload = new
            {
                model = _options.Model,
                stream = _options.Stream,
                messages = new object[]
                {
                    new { role = "system", content = _options.SystemPrompt },
                    new { role = "user", content = prompt }
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Ollama request failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}",
                    null,
                    response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                var content = contentElement.GetString() ?? "{}";
                using var contentDoc = JsonDocument.Parse(content);
                return ParseDecisionResult(contentDoc.RootElement);
            }

            return new DecisionV2Result
            {
                Reply = "{}",
                Confidence = 0.0,
                Rationale = "Failed to parse response",
                Alternatives = new List<string>()
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Error completing DecisionV2 prompt with Ollama", ex);
        }
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/stream");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            model = _options.Model,
            stream = true,
            messages = new object[]
            {
                new { role = "system", content = _options.SystemPrompt },
                new { role = "user", content = prompt }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await foreach (var line in ParseStream(response.Content.ReadAsStream(ct)))
        {
            yield return line;
        }
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
