using FluentAssertions;
using Sovereign.Tests.InfrastructureTests;
using Sovereign.Intelligence.DecisionV2;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sovereign.Tests.APIEndpointTests;

public sealed class DecisionV2IntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DecisionV2IntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DecideV2_WithPromotionMilestone_ShouldReturnValidResponse()
    {
        var client = _factory.CreateClient();

        var input = new DecisionV2Input
        {
            UserId = "user1",
            ContactId = "contact1",
            Message = "I just got promoted to Senior Engineer!",
            Platform = "linkedin",
            Surface = "post_compose",
            SourceAuthor = "John Doe",
            SourceText = "Excited to announce my promotion to Senior Engineer at TechCorp!",
            RelationshipRole = "Peer",
            LastInteractionDays = 7,
            TotalInteractions = 15,
            ReciprocityScore = 0.8,
            MomentumScore = 0.7,
            PowerDifferential = 0.1,
            EmotionalTemperature = 0.9,
            RecentRelationshipSummary = "Regular professional interactions, positive momentum",
            RelevantMemories = new List<string> { "congratulated on previous achievement", "discussed career goals" },
            AllowNoReply = true,
            RequestAlternatives = true
        };

        var response = await client.PostAsJsonAsync("/api/ai/conversations/decide-v2", input);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DecisionV2Result>();
        result.Should().NotBeNull();
        result!.Move.Should().NotBeNullOrEmpty();
        result.Reply.Should().NotBeNullOrEmpty();
        result.ShouldReply.Should().BeTrue();
        result.ShouldReplyNow.Should().BeTrue();
        result.Strategy.Should().NotBeNullOrEmpty();
        result.Tone.Should().NotBeNullOrEmpty();
        result.Confidence.Should().BeGreaterThan(0);
        result.Rationale.Should().NotBeNullOrEmpty();
        result.SituationType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DecideV2_WithLowSignalPost_ShouldReturnNoReply()
    {
        var client = _factory.CreateClient();

        var input = new DecisionV2Input
        {
            UserId = "user1",
            ContactId = "contact4",
            Message = "Good morning everyone",
            Platform = "linkedin",
            Surface = "post_compose",
            SourceAuthor = "Generic User",
            SourceText = "Good morning! Have a great day.",
            RelationshipRole = "WeakTie",
            LastInteractionDays = 365,
            TotalInteractions = 1,
            ReciprocityScore = 0.1,
            MomentumScore = 0.1,
            PowerDifferential = 0.0,
            EmotionalTemperature = 0.2,
            RecentRelationshipSummary = "Minimal interaction history",
            RelevantMemories = new List<string>(),
            AllowNoReply = true,
            RequestAlternatives = false
        };

        var response = await client.PostAsJsonAsync("/api/ai/conversations/decide-v2", input);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DecisionV2Result>();
        result.Should().NotBeNull();
        result!.ShouldReply.Should().BeFalse();
        result.Move.Should().Be("no_reply");
        result.Reply.Should().BeEmpty();
    }

    [Fact]
    public async Task DecideV2_WithExtensionPayloadShape_ShouldProcessSuccessfully()
    {
        var client = _factory.CreateClient();

        // Extension sends a simplified payload shape from the Chrome extension
        var extensionPayload = new
        {
            UserId = "user-001",
            ContactId = "john-doe-linkedin",
            Message = "That's a great point about AI governance!",
            RelationshipRole = "Peer",
            Platform = "linkedin",
            Surface = "feed_reply",
            CurrentUrl = "https://www.linkedin.com/feed/",
            SourceAuthor = "John Doe",
            SourceTitle = "John Doe | Software Engineer at TechCorp",
            SourceText = "Excited about the latest developments in AI governance and regulation.",
            ParentContextText = "Excited about the latest developments in AI governance and regulation.",
            NearbyContextText = "",
            InteractionMetadata = new
            {
                mode = "reply",
                pageTitle = "LinkedIn Feed"
            }
        };

        var response = await client.PostAsJsonAsync("/api/ai/conversations/decide-v2", extensionPayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DecisionV2Result>();
        result.Should().NotBeNull();
        result!.Move.Should().NotBeNullOrEmpty();
        result.Reply.Should().NotBeNullOrEmpty();
        result.ShouldReply.Should().BeTrue();
        result.Strategy.Should().NotBeNullOrEmpty();
    }
}