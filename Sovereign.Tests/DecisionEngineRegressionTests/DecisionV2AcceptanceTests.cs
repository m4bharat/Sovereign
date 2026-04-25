using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Moq;
using Sovereign.Domain.DTOs;
using Sovereign.Domain.Models;
using Sovereign.Domain.Services;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.DecisionV2;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;
using Xunit;

namespace Sovereign.Tests.DecisionEngineRegressionTests;

public class DecisionV2AcceptanceTests
{
    [Theory]
    [MemberData(nameof(GetGoldenScenarios))]
    public async Task DecideAsync_ShouldMatchGoldenScenario(GoldenScenario scenario)
    {
        var engine = CreateEngine();

        var result = await engine.DecideAsync(scenario.InputPayload);

        Assert.Equal(scenario.ExpectedSituationType, result.SituationType);
        Assert.Equal(NormalizeMove(scenario.ExpectedMoveFamily), NormalizeMove(result.Move));
        Assert.Equal(scenario.ShouldReply, result.ShouldReply);

        if (!scenario.ShouldReply)
        {
            Assert.Equal("no_reply", NormalizeMove(result.Move));
            Assert.True(string.IsNullOrWhiteSpace(result.Reply));
            return;
        }

        Assert.False(string.IsNullOrWhiteSpace(result.Reply));

        foreach (var forbidden in scenario.ForbiddenPatterns)
        {
            Assert.DoesNotContain(forbidden, result.Reply ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        if (scenario.AcceptableReplies.Count > 0)
        {
            Assert.Contains(
                scenario.AcceptableReplies,
                candidate => (result.Reply ?? string.Empty).Contains(candidate, StringComparison.OrdinalIgnoreCase));
        }

        Assert.False(ContainsUnsupportedNumber(result.Reply ?? string.Empty, scenario.InputPayload));

        if (string.Equals(scenario.ExpectedSituationType, "compose_post", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True((result.Reply ?? string.Empty).Length >= 120);
            Assert.False(LooksLikeComment(result.Reply ?? string.Empty));
        }

        if (string.Equals(scenario.Surface, "messaging_chat", StringComparison.OrdinalIgnoreCase) &&
            IsCommandOnlyMessage(scenario.Message))
        {
            Assert.False((result.Reply ?? string.Empty).StartsWith("reply", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("linkedin", result.Reply ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("point around", result.Reply ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(scenario.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase))
        {
            Assert.False(ContainsUnsupportedClaimMarkers(result.Reply ?? string.Empty, scenario.InputPayload));
        }
    }

    public static IEnumerable<object[]> GetGoldenScenarios()
    {
        foreach (var scenario in BuildDataset())
        {
            yield return new object[] { scenario };
        }
    }

    private static DecisionEngineV2 CreateEngine()
    {
        var mockRelationshipEngine = new Mock<IRelationshipIntelligenceEngine>();
        mockRelationshipEngine.Setup(e => e.Analyze(It.IsAny<RelationshipContext>()))
            .Returns(new SocialInsight
            {
                OpportunityScore = 0.5,
                RiskScore = 0.2
            });

        var mockAssembler = new Mock<IConversationContextAssembler>();
        mockAssembler.Setup(a => a.AssembleAsync(It.IsAny<AssembleAiContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssembleAiContextRequest request, CancellationToken _) => new MessageContext
            {
                UserId = request.UserId,
                ContactId = request.ContactId,
                Message = request.Message,
                Platform = request.Platform,
                Surface = request.Surface,
                CurrentUrl = request.CurrentUrl,
                SourceAuthor = request.SourceAuthor,
                SourceTitle = request.SourceTitle,
                SourceText = request.SourceText,
                ParentContextText = request.ParentContextText,
                NearbyContextText = request.NearbyContextText,
                InteractionMode = ResolveInteractionModeForTest(request)
            });

        return new DecisionEngineV2(
            mockAssembler.Object,
            mockRelationshipEngine.Object,
            new SocialSituationDetector(),
            new SocialMovePlanner(),
            new CandidateReplyGenerator(),
            new CandidateScoringEngine(),
            new WinnerSelectionEngine(),
            new NoOpDecisionV2LlmClient(),
            new Mock<ILogger<DecisionEngineV2>>().Object);
    }

    private static string ResolveInteractionModeForTest(AssembleAiContextRequest request)
    {
        var surface = (request.Surface ?? string.Empty).Trim().ToLowerInvariant();

        if (surface is "messaging_chat" or "chatbox" or "dm_chat" or "linkedin_chat")
            return "chat";

        if (surface is "feed_reply" or "comment_reply" or "reply" or "add_comment")
            return "reply";

        if (surface is "start_post" or "create_post" or "compose_post" or "write_post")
            return "compose";

        if (!string.IsNullOrWhiteSpace(request.SourceText))
            return "reply";

        if (!string.IsNullOrWhiteSpace(request.ParentContextText))
            return "chat";

        return "compose";
    }

    private static string NormalizeMove(string move)
    {
        var value = (move ?? string.Empty).Trim().ToLowerInvariant();

        return value switch
        {
            "answer" => "answer_supportively",
            "answer_question" => "answer_supportively",
            "insight" => "add_insight",
            "light_touch_question" => "light_touch",
            "acknowledge_update" => "acknowledge",
            _ => value
        };
    }

    private static IReadOnlyList<GoldenScenario> BuildDataset() =>
    [
        Scenario(
            "promotion_vp_announcement",
            "feed_reply",
            "make a comment",
            "Sunil",
            "",
            "Happy to share that I have stepped into the role of Vice President at Data Axle India.",
            "achievement_share",
            "congratulate",
            true,
            ["Congratulations", "milestone"],
            ["great post", "what stayed with me", "point around"]),

        Scenario(
            "joined_thoughtworks",
            "feed_reply",
            "comment",
            "",
            "",
            "Happy to share that I have joined Thoughtworks. Grateful for the opportunity and excited for this new chapter.",
            "achievement_share",
            "congratulate",
            true,
            ["Congratulations", "Thoughtworks"],
            ["great post", "point around"]),

        Scenario(
            "birthday_chat_reply",
            "messaging_chat",
            "reply",
            "",
            "",
            "Happy belated birthday!",
            "holiday_greeting",
            "respond",
            true,
            ["Thank you so much", "Really appreciate"],
            ["LinkedIn", "point around", "Reply."]),

        Scenario(
            "hello_dm",
            "messaging_chat",
            "reply",
            "",
            "",
            "Hey! Hope you are doing well.",
            "greeting",
            "respond",
            true,
            ["Thank you", "appreciate"],
            ["LinkedIn", "point around", "Reply."]),

        Scenario(
            "multi_model_architecture",
            "feed_reply",
            "comment",
            "Rajiv Kedia",
            "",
            "Anthropic blocked an entire org this week. Multi model architecture fixes this. Keep prompts and guardrails abstracted from the model layer.",
            "industry_news",
            "add_insight",
            true,
            ["operational", "portability"],
            ["great post", "spot on", "what stayed with me", "point around"]),

        Scenario(
            "ai_agents_production",
            "feed_reply",
            "comment",
            "",
            "",
            "AI agents look exciting in demos, but production systems fail when observability and workflow control are weak.",
            "industry_news",
            "add_insight",
            true,
            ["operational", "control"],
            ["great post", "spot on", "what stayed with me"]),

        Scenario(
            "educational_post",
            "feed_reply",
            "comment",
            "",
            "",
            "5 ways to improve API performance in production with caching, connection pooling, and query tuning.",
            "educational",
            "add_insight",
            true,
            ["trade-off", "coordination", "operational"],
            ["great post", "well said"]),

        Scenario(
            "leadership_opinion",
            "feed_reply",
            "comment",
            "",
            "",
            "The best leaders create clarity, not control.",
            "opinion",
            "add_nuance",
            true,
            ["clarity", "operational"],
            ["great post", "spot on"]),

        Scenario(
            "poll_post",
            "feed_reply",
            "comment",
            "",
            "",
            "What is your biggest challenge scaling engineering teams? Share your thoughts in the comments.",
            "cta_engagement",
            "answer_supportively",
            true,
            ["My take:", "start with"],
            ["great post", "what stayed with me"]),

        Scenario(
            "direct_question_post",
            "feed_reply",
            "comment",
            "",
            "",
            "How do you manage technical debt in fast-growing startups?",
            "question",
            "answer_supportively",
            true,
            ["My take:", "start with"],
            ["great post", "well said"]),

        Scenario(
            "compose_ai_trend_post",
            "start_post",
            "Write a post on AI trends",
            "",
            "",
            "",
            "compose_post",
            "draft_post",
            true,
            ["AI", "teams", "workflows"],
            ["great post", "point around LinkedIn"]),

        Scenario(
            "compose_hiring_post",
            "start_post",
            "Draft a hiring post for senior backend engineer",
            "",
            "",
            "",
            "compose_post",
            "draft_post",
            true,
            ["AI", "teams", "workflows"],
            ["great post", "point around LinkedIn"]),

        Scenario(
            "rewrite_feed_draft",
            "feed_reply",
            "Congrats! Great work on this milestone.",
            "",
            "",
            "Happy to share that I was promoted to Director this week.",
            "rewrite_feed_reply",
            "rewrite_user_intent",
            true,
            ["milestone", "progress"],
            ["point around"]),

        Scenario(
            "rewrite_chat_draft",
            "messaging_chat",
            "Thanks, appreciate your message!",
            "",
            "",
            "Congrats on your promotion!",
            "rewrite_direct_message",
            "rewrite_user_intent",
            true,
            ["Thanks", "appreciate"],
            ["LinkedIn", "Reply."]),

        Scenario(
            "low_signal_meme",
            "feed_reply",
            "comment",
            "",
            "",
            "Motivation Monday meme with low substance.",
            "low_signal",
            "no_reply",
            false),

        Scenario(
            "controversial_political",
            "feed_reply",
            "comment",
            "",
            "",
            "Strong political statement about the election and why everyone else is wrong.",
            "controversial_no_reply",
            "no_reply",
            false),

        Scenario(
            "layoff_sensitive",
            "feed_reply",
            "comment",
            "",
            "",
            "Today I was laid off after a difficult quarter.",
            "sensitive",
            "no_reply",
            false),

        Scenario(
            "startup_funding",
            "feed_reply",
            "comment",
            "",
            "",
            "Excited to announce our Series A funding and grateful to the team and backers who made it possible.",
            "achievement_share",
            "congratulate",
            true,
            ["Congratulations", "milestone"],
            ["great post", "point around"]),

        Scenario(
            "product_launch",
            "feed_reply",
            "comment",
            "",
            "",
            "We just launched our new platform today after months of work.",
            "achievement_share",
            "praise",
            true,
            ["Strong work", "result"],
            ["great post", "point around"]),

        Scenario(
            "networking_dm",
            "messaging_chat",
            "reply",
            "",
            "",
            "Would love to connect and learn more about your journey.",
            "direct_message",
            "respond_helpfully",
            true,
            ["Thanks", "appreciate"],
            ["LinkedIn", "Reply."]),

        Scenario(
            "open_to_work",
            "feed_reply",
            "comment",
            "",
            "",
            "Open to work and actively exploring backend engineering opportunities.",
            "job_search",
            "encourage",
            true,
            ["good direction", "momentum"],
            ["great post", "point around"]),

        Scenario(
            "holiday_post",
            "feed_reply",
            "comment",
            "",
            "",
            "Wishing everyone a joyful Diwali and a prosperous year ahead.",
            "holiday_greeting",
            "respond",
            true,
            ["Thank you", "wishing you the same"],
            ["great post", "point around"]),

        Scenario(
            "group_announcement",
            "feed_reply",
            "comment",
            "",
            "",
            "We are hiring across engineering this quarter and expanding the platform team.",
            "group_announcement",
            "acknowledge",
            true,
            ["Appreciate the update"],
            ["great post", "point around"]),

        Scenario(
            "career_reflection",
            "feed_reply",
            "comment",
            "",
            "",
            "10 things I learned in my first year as manager.",
            "reflection",
            "engage",
            true,
            ["operational", "complexity"],
            ["great post", "what stayed with me", "point around"]),

        Scenario(
            "technical_question_provider_failover",
            "feed_reply",
            "comment",
            "",
            "",
            "What changes first when provider failover starts affecting prompt behavior in production?",
            "question",
            "answer_supportively",
            true,
            ["My take:", "bottleneck"],
            ["great post", "point around"])
    ];

    private static GoldenScenario Scenario(
        string name,
        string surface,
        string message,
        string sourceAuthor,
        string sourceTitle,
        string sourceText,
        string expectedSituationType,
        string expectedMoveFamily,
        bool shouldReply = true,
        IReadOnlyList<string>? acceptableReplies = null,
        IReadOnlyList<string>? forbiddenPatterns = null)
    {
        return new GoldenScenario
        {
            Name = name,
            Surface = surface,
            Message = message,
            SourceAuthor = sourceAuthor,
            SourceTitle = sourceTitle,
            SourceText = sourceText,
            ExpectedSituationType = expectedSituationType,
            ExpectedMoveFamily = expectedMoveFamily,
            ShouldReply = shouldReply,
            AcceptableReplies = acceptableReplies ?? Array.Empty<string>(),
            ForbiddenPatterns = forbiddenPatterns ?? Array.Empty<string>(),
            InputPayload = new DecisionV2Input
            {
                UserId = "user-001",
                ContactId = name,
                Message = message,
                SourceAuthor = sourceAuthor,
                SourceTitle = sourceTitle,
                SourceText = sourceText,
                ParentContextText = sourceText,
                NearbyContextText = string.Empty,
                Platform = "linkedin",
                Surface = surface,
                CurrentUrl = surface == "start_post"
                    ? "https://www.linkedin.com/post/new"
                    : "https://www.linkedin.com/feed/",
                RelationshipRole = "Peer",
                AllowNoReply = true
            }
        };
    }

    private static bool IsCommandOnlyMessage(string? message)
    {
        var text = (message ?? string.Empty).Trim().ToLowerInvariant();
        return text is
            "reply" or
            "write reply" or
            "suggest reply" or
            "make a reply" or
            "comment" or
            "write comment" or
            "make a comment" or
            "suggest comment" or
            "add comment";
    }

    private static bool ContainsUnsupportedNumber(string reply, DecisionV2Input input)
    {
        var source = string.Join(" ",
            input.SourceText ?? string.Empty,
            input.SourceTitle ?? string.Empty,
            input.ParentContextText ?? string.Empty,
            input.NearbyContextText ?? string.Empty);

        var numbers = Regex.Matches(reply, @"\b\d+(\.\d+)?%?\b")
            .Select(m => m.Value)
            .Distinct()
            .ToArray();

        return numbers.Any(number => !source.Contains(number, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsUnsupportedClaimMarkers(string reply, DecisionV2Input input)
    {
        var source = string.Join(" ",
            input.SourceText ?? string.Empty,
            input.SourceTitle ?? string.Empty,
            input.ParentContextText ?? string.Empty,
            input.NearbyContextText ?? string.Empty);

        var markers = new[]
        {
            "studies show",
            "research shows",
            "research says",
            "on average",
            "according to data",
            "according to research",
            "survey shows"
        };

        return markers.Any(marker =>
            reply.Contains(marker, StringComparison.OrdinalIgnoreCase) &&
            !source.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeComment(string reply)
    {
        var trimmed = reply.Trim();
        return trimmed.Length < 120 || Regex.Matches(trimmed, @"[.!?]").Count < 2;
    }

    public sealed class GoldenScenario
    {
        public string Name { get; init; } = string.Empty;
        public string Surface { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string SourceAuthor { get; init; } = string.Empty;
        public string SourceTitle { get; init; } = string.Empty;
        public string SourceText { get; init; } = string.Empty;
        public string ExpectedSituationType { get; init; } = string.Empty;
        public string ExpectedMoveFamily { get; init; } = string.Empty;
        public bool ShouldReply { get; init; }
        public IReadOnlyList<string> AcceptableReplies { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> ForbiddenPatterns { get; init; } = Array.Empty<string>();
        public DecisionV2Input InputPayload { get; init; } = new();
    }

    private sealed class NoOpDecisionV2LlmClient : ILlmClient
    {
        public Task<string> CompleteAsync(string prompt, CancellationToken ct = default) =>
            Task.FromResult("{}");

        public Task<DecisionV2Result> CompleteDecisionV2Async(string prompt, CancellationToken ct = default) =>
            Task.FromResult(new DecisionV2Result
            {
                Reply = string.Empty,
                Confidence = 0.0,
                Rationale = string.Empty,
                Alternatives = new List<string>()
            });

        public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default) =>
            AsyncEnumerable.Empty<string>();
    }
}
