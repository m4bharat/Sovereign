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
        Assert.Equal(scenario.ShouldReply, result.ShouldReply);

        if (!scenario.ShouldReply)
        {
            Assert.Equal("no_reply", NormalizeMove(result.Move));
            Assert.True(string.IsNullOrWhiteSpace(result.Reply));
            return;
        }

        Assert.Equal(NormalizeMove(scenario.ExpectedMoveFamily), NormalizeMove(result.Move));
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
        Scenario("promotion_vp_announcement", "feed_reply", "make a comment",
            "Sunil", "", "Happy to share that I’ve stepped into the role of Vice President at Data Axle India...",
            "achievement_share", "congratulate", true,
            ["Congratulations on stepping into the VP role", "Well-earned milestone"],
            ["great post", "what stayed with me", "point around"]),

        Scenario("joined_thoughtworks", "feed_reply", "comment",
            "", "", "Happy to share that I’ve joined Thoughtworks...",
            "achievement_share", "congratulate"),

        Scenario("birthday_chat_reply", "messaging_chat", "reply",
            "", "", "Happy belated birthday!",
            "holiday_greeting", "respond"),

        Scenario("hello_dm", "messaging_chat", "reply",
            "", "", "Hey! Hope you’re doing well.",
            "greeting", "respond"),

        Scenario("multi_model_architecture", "feed_reply", "comment",
            "", "", "Anthropic blocked an entire org this week...",
            "industry_news", "add_insight"),

        Scenario("leadership_opinion", "feed_reply", "comment",
            "", "", "The best leaders create clarity, not control.",
            "opinion", "add_nuance"),

        Scenario("educational_post", "feed_reply", "comment",
            "", "", "5 ways to improve API performance in production...",
            "educational", "add_insight"),

        Scenario("poll_post", "feed_reply", "comment",
            "", "", "What’s your biggest challenge scaling engineering teams?",
            "cta_engagement", "answer_supportively"),

        Scenario("direct_question_post", "feed_reply", "comment",
            "", "", "How do you manage technical debt in fast-growing startups?",
            "question", "answer_supportively"),

        Scenario("political_post", "feed_reply", "comment",
            "", "", "Strong political statement / divisive topic...",
            "controversial_no_reply", "no_reply", false),

        Scenario("low_signal_meme", "feed_reply", "comment",
            "", "", "Monday motivation meme / low substance...",
            "low_signal", "no_reply", false),

        Scenario("rewrite_feed_draft", "feed_reply", "Congrats! Great work on this milestone.",
            "", "", "Promotion announcement...",
            "rewrite_feed_reply", "rewrite_user_intent"),

        Scenario("rewrite_chat_draft", "messaging_chat", "Thanks, appreciate your message!",
            "", "", "Congrats on your promotion!",
            "rewrite_direct_message", "rewrite_user_intent"),

        Scenario("compose_ai_trend_post", "start_post", "Write a post on AI trends",
            "", "", "",
            "compose_post", "draft_post"),

        Scenario("compose_hiring_post", "start_post", "Draft a hiring post for senior backend engineer",
            "", "", "",
            "compose_post", "draft_post"),

        Scenario("startup_funding", "feed_reply", "comment",
            "", "", "Excited to announce our Series A funding...",
            "achievement_share", "congratulate"),

        Scenario("product_launch", "feed_reply", "comment",
            "", "", "We just launched our new platform today...",
            "achievement_share", "praise"),

        Scenario("career_reflection", "feed_reply", "comment",
            "", "", "10 things I learned in my first year as manager...",
            "reflection", "engage"),

        Scenario("layoff_sensitive", "feed_reply", "comment",
            "", "", "Today I was laid off...",
            "sensitive", "no_reply", false),

        Scenario("networking_dm", "messaging_chat", "reply",
            "", "", "Would love to connect and learn more about your journey.",
            "direct_message", "respond_helpfully")
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
                ContactId = "contact-001",
                Message = message,
                SourceAuthor = sourceAuthor,
                SourceTitle = sourceTitle,
                SourceText = sourceText,
                ParentContextText = sourceText,
                NearbyContextText = string.Empty,
                Platform = "linkedin",
                Surface = surface,
                CurrentUrl = "https://www.linkedin.com/feed/",
                RelationshipRole = "Peer",
                AllowNoReply = true
            }
        };
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
