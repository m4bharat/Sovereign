using Microsoft.Extensions.Logging.Abstractions;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.DecisionV2;
using Sovereign.Intelligence.Services;
using Xunit;

namespace Sovereign.Tests.Services;

public sealed class AiSituationClassifierTests
{
    [Fact]
    public async Task ClassifyAsync_ShouldParseWrappedJsonPayload()
    {
        var sut = new AiSituationClassifier(
            new StubLlmClient("""
```json
{
  "situationType": "achievement_post",
  "userIntent": "comment",
  "recommendedMove": "comment",
  "isCommandOnly": "true",
  "confidence": "0.91",
  "rationale": "Wrapped payload"
}
```
"""),
            NullLogger<AiSituationClassifier>.Instance);

        var result = await sut.ClassifyAsync(
            new MessageContext
            {
                Surface = "feed_reply",
                Message = "comment",
                SourceText = "Excited to share that I joined a new team."
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("achievement_share", result!.SituationType);
        Assert.Equal("generate_comment", result.UserIntent);
        Assert.Equal("engage", result.RecommendedMove);
        Assert.True(result.IsCommandOnly);
        Assert.Equal(0.91, result.Confidence, 2);
    }

    [Fact]
    public async Task ClassifyAsync_ShouldFallbackForBlankSituationTypeBasedOnSurface()
    {
        var sut = new AiSituationClassifier(
            new StubLlmClient("""{"situationType":"","userIntent":"","recommendedMove":"","confidence":1.4}"""),
            NullLogger<AiSituationClassifier>.Instance);

        var result = await sut.ClassifyAsync(
            new MessageContext
            {
                Surface = "start_post",
                Message = "Write a post on AI operations"
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("compose_post", result!.SituationType);
        Assert.Equal("generate_reply", result.UserIntent);
        Assert.Equal("engage", result.RecommendedMove);
        Assert.Equal(1.0, result.Confidence, 3);
    }

    [Fact]
    public async Task ClassifyAsync_ShouldReturnNullForNonJsonPayload()
    {
        var sut = new AiSituationClassifier(
            new StubLlmClient("not json at all"),
            NullLogger<AiSituationClassifier>.Instance);

        var result = await sut.ClassifyAsync(
            new MessageContext
            {
                Surface = "feed_reply",
                Message = "comment"
            },
            CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class StubLlmClient : ILlmClient
    {
        private readonly string _payload;

        public StubLlmClient(string payload)
        {
            _payload = payload;
        }

        public Task<string> CompleteAsync(string prompt, CancellationToken ct = default) =>
            Task.FromResult(_payload);

        public Task<DecisionV2Result> CompleteDecisionV2Async(string prompt, CancellationToken ct = default) =>
            Task.FromResult(new DecisionV2Result());

        public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default) =>
            AsyncEnumerable.Empty<string>();
    }
}
