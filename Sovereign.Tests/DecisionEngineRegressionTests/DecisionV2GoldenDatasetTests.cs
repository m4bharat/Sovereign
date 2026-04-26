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
using Xunit.Abstractions;

namespace Sovereign.Tests.DecisionEngineRegressionTests;

public sealed class DecisionV2GoldenDatasetTests
{
    private readonly ITestOutputHelper _output;

    public DecisionV2GoldenDatasetTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetGoldenScenarios))]
    public async Task DecideAsync_ShouldPassGoldenScenario(DecisionGoldenScenario scenario)
    {
        var engine = CreateEngine();
        var result = await engine.DecideAsync(scenario.InputPayload);
        var detail = FormatFailure(scenario, result);

        Assert.Equal(25, BuildDataset().Count);

        Assert.True(
            string.Equals(result.SituationType, scenario.ExpectedSituationType, StringComparison.OrdinalIgnoreCase),
            $"Situation mismatch. {detail}");

        Assert.True(
            string.Equals(MapMoveFamily(result.Move), scenario.ExpectedMoveFamily, StringComparison.OrdinalIgnoreCase),
            $"Move mismatch. {detail}");

        Assert.True(
            result.ShouldReply == scenario.ShouldReply,
            $"ShouldReply mismatch. {detail}");

        if (!scenario.ShouldReply)
        {
            Assert.True(string.IsNullOrWhiteSpace(result.Reply), $"Expected empty reply. {detail}");
            return;
        }

        var reply = result.Reply ?? string.Empty;
        Assert.True(!string.IsNullOrWhiteSpace(reply), $"Reply was empty. {detail}");

        if (scenario.RequiresNoHallucination)
        {
            Assert.True(!ContainsUnsupportedNumber(reply, scenario.InputPayload), $"Unsupported number. {detail}");
            Assert.True(!ContainsUnsupportedClaimMarkers(reply, scenario.InputPayload), $"Unsupported factual framing. {detail}");
        }

        Assert.True(!ContainsForbiddenGenericPhrase(reply), $"Generic phrase found. {detail}");

        if (scenario.RequiresComposeLength)
            Assert.True(reply.Length >= 120, $"Compose output too short ({reply.Length}). {detail}");

        if (scenario.RequiresChatCommandHandling)
        {
            Assert.True(!reply.StartsWith("reply", StringComparison.OrdinalIgnoreCase), $"Echoed command text. {detail}");
            Assert.True(!reply.Contains("linkedin", StringComparison.OrdinalIgnoreCase), $"Leaked platform wording. {detail}");
        }
    }

    [Fact]
    public async Task DecideAsync_ShouldReportGoldenDatasetPassRates()
    {
        var engine = CreateEngine();
        var scenarios = BuildDataset();

        var situationPass = 0;
        var movePass = 0;
        var hallucinationEligible = 0;
        var hallucinationPass = 0;
        var genericEligible = 0;
        var genericPass = 0;
        var composeEligible = 0;
        var composePass = 0;
        var chatCommandEligible = 0;
        var chatCommandPass = 0;

        foreach (var scenario in scenarios)
        {
            var result = await engine.DecideAsync(scenario.InputPayload);
            var reply = result.Reply ?? string.Empty;

            if (string.Equals(result.SituationType, scenario.ExpectedSituationType, StringComparison.OrdinalIgnoreCase))
                situationPass++;

            if (string.Equals(MapMoveFamily(result.Move), scenario.ExpectedMoveFamily, StringComparison.OrdinalIgnoreCase))
                movePass++;

            if (scenario.RequiresNoHallucination && scenario.ShouldReply)
            {
                hallucinationEligible++;
                if (!ContainsUnsupportedNumber(reply, scenario.InputPayload) &&
                    !ContainsUnsupportedClaimMarkers(reply, scenario.InputPayload))
                {
                    hallucinationPass++;
                }
            }

            if (scenario.ShouldReply)
            {
                genericEligible++;
                if (!ContainsForbiddenGenericPhrase(reply))
                    genericPass++;
            }

            if (scenario.RequiresComposeLength && scenario.ShouldReply)
            {
                composeEligible++;
                if (reply.Length >= 120)
                    composePass++;
            }

            if (scenario.RequiresChatCommandHandling && scenario.ShouldReply)
            {
                chatCommandEligible++;
                if (!reply.StartsWith("reply", StringComparison.OrdinalIgnoreCase) &&
                    !reply.Contains("linkedin", StringComparison.OrdinalIgnoreCase))
                {
                    chatCommandPass++;
                }
            }
        }

        _output.WriteLine($"Scenario count: {scenarios.Count}");
        _output.WriteLine($"Situation pass rate: {situationPass}/{scenarios.Count} ({FormatRate(situationPass, scenarios.Count)})");
        _output.WriteLine($"Move pass rate: {movePass}/{scenarios.Count} ({FormatRate(movePass, scenarios.Count)})");
        _output.WriteLine($"No hallucination pass rate: {hallucinationPass}/{hallucinationEligible} ({FormatRate(hallucinationPass, hallucinationEligible)})");
        _output.WriteLine($"No generic phrases pass rate: {genericPass}/{genericEligible} ({FormatRate(genericPass, genericEligible)})");
        _output.WriteLine($"Compose length pass rate: {composePass}/{composeEligible} ({FormatRate(composePass, composeEligible)})");
        _output.WriteLine($"Chat command handling pass rate: {chatCommandPass}/{chatCommandEligible} ({FormatRate(chatCommandPass, chatCommandEligible)})");

        Assert.Equal(25, scenarios.Count);
        Assert.Equal(scenarios.Count, situationPass);
        Assert.Equal(scenarios.Count, movePass);
        Assert.Equal(hallucinationEligible, hallucinationPass);
        Assert.Equal(genericEligible, genericPass);
        Assert.Equal(composeEligible, composePass);
        Assert.Equal(chatCommandEligible, chatCommandPass);
    }

    public static IEnumerable<object[]> GetGoldenScenarios() =>
        BuildDataset().Select(scenario => new object[] { scenario });

    private static List<DecisionGoldenScenario> BuildDataset() =>
    [
        Scenario("promotion_vp_announcement", "feed_reply", "Happy to share that I have stepped into the role of Vice President at Data Axle India.", "make a comment", "achievement_share", "reply", true),
        Scenario("new_job_thoughtworks", "feed_reply", "Happy to share that I have joined Thoughtworks. Grateful for the opportunity and excited for this new chapter.", "comment", "achievement_share", "reply", true),
        Scenario("new_job_microsoft", "feed_reply", "Excited to start a new role at Microsoft as Principal Product Manager.", "make a comment", "achievement_share", "reply", true),
        Scenario("promotion_director", "feed_reply", "Proud to share that I was promoted to Director this week after years of learning and support.", "write comment", "achievement_share", "reply", true),
        Scenario("hiring_platform_team", "feed_reply", "We are hiring across platform engineering and data infrastructure this quarter.", "comment", "group_announcement", "reply", true),
        Scenario("ai_thought_leadership_multimodel", "feed_reply", "Anthropic blocked an entire org this week. Multi model architecture fixes this by removing dependency on any single provider.", "comment", "industry_news", "reply", true),
        Scenario("ai_thought_leadership_agents", "feed_reply", "AI agents look impressive in demos, but production workflows fail when observability and control are weak.", "comment", "industry_news", "reply", true),
        Scenario("api_education_post", "feed_reply", "5 ways to improve API performance in production with caching, query tuning, and connection pooling.", "comment", "educational", "reply", true),
        Scenario("leadership_opinion_clarity", "feed_reply", "The best leaders create clarity, not control.", "comment", "opinion", "reply", true),
        Scenario("gratitude_post_community", "feed_reply", "Grateful for everyone who supported our launch this week. Thank you for the encouragement.", "comment", "personal_update", "reply", true),
        Scenario("product_launch_platform", "feed_reply", "We just launched our workflow platform after months of effort and customer feedback.", "comment", "achievement_share", "reply", true),
        Scenario("founder_story_origin", "feed_reply", "Three years ago I started this company after seeing how broken the onboarding experience was.", "comment", "reflection", "reply", true),
        Scenario("low_context_generic_post", "feed_reply", "Big things coming soon. Stay tuned.", "comment", "low_signal", "no_reply", false),
        Scenario("motivation_quote_post", "feed_reply", "Monday motivation: believe in yourself and keep going.", "make a comment", "low_signal", "no_reply", false),
        Scenario("poll_scaling_teams", "feed_reply", "What is your biggest challenge scaling engineering teams? Share your thoughts below.", "comment", "cta_engagement", "reply", true),
        Scenario("direct_question_tech_debt", "feed_reply", "How do you manage technical debt in fast-growing startups?", "comment", "question", "reply", true),
        Scenario("networking_dm_thank_you", "messaging_chat", "Thanks for making the intro yesterday.", "reply", "direct_message", "reply", true),
        Scenario("birthday_dm_reply", "messaging_chat", "Happy belated birthday!", "reply", "holiday_greeting", "reply", true),
        Scenario("generic_greeting_dm", "messaging_chat", "Hey! Hope you are doing well.", "reply", "greeting", "reply", true),
        Scenario("reconnect_old_contact", "messaging_chat", "It has been a while. Would love to reconnect and hear what you are building now.", "reply", "direct_message", "reply", true),
        Scenario("recruiter_outreach", "messaging_chat", "We are hiring for a senior backend role and your profile stood out.", "reply", "direct_message", "reply", true),
        Scenario("compose_ai_trends_post", "start_post", "", "Write a LinkedIn post on AI trends", "compose_post", "compose", true),
        Scenario("compose_hiring_post", "start_post", "", "Draft a hiring post for a senior backend engineer", "compose_post", "compose", true),
        Scenario("compose_meaningful_draft", "start_post", "", "Here is my draft: AI adoption is accelerating, but teams still struggle to connect pilots to measurable business outcomes. Improve this into a LinkedIn post.", "compose_post", "compose", true),
        Scenario("feed_rewrite_draft_launch", "feed_reply", "We launched our new platform today.", "Amazing launch and strong progress", "rewrite_feed_reply", "rewrite", true)
    ];

    private static DecisionGoldenScenario Scenario(
        string name,
        string surface,
        string sourceText,
        string userDraft,
        string expectedSituationType,
        string expectedMoveFamily,
        bool shouldReply)
    {
        var requiresNoHallucination = string.Equals(surface, "feed_reply", StringComparison.OrdinalIgnoreCase) && shouldReply;
        var requiresComposeLength = string.Equals(surface, "start_post", StringComparison.OrdinalIgnoreCase);
        var requiresChatCommandHandling = string.Equals(surface, "messaging_chat", StringComparison.OrdinalIgnoreCase) &&
                                          IsCommandOnlyMessage(userDraft);

        return new DecisionGoldenScenario
        {
            Name = name,
            ExpectedSituationType = expectedSituationType,
            ExpectedMoveFamily = expectedMoveFamily,
            ShouldReply = shouldReply,
            RequiresNoHallucination = requiresNoHallucination,
            RequiresComposeLength = requiresComposeLength,
            RequiresChatCommandHandling = requiresChatCommandHandling,
            InputPayload = new DecisionV2Input
            {
                UserId = "user-001",
                ContactId = name,
                Message = userDraft,
                SourceAuthor = string.Empty,
                SourceTitle = string.Empty,
                SourceText = sourceText,
                ParentContextText = sourceText,
                NearbyContextText = string.Empty,
                Platform = "linkedin",
                Surface = surface,
                CurrentUrl = surface == "start_post"
                    ? "https://www.linkedin.com/post/new"
                    : "https://www.linkedin.com/feed/",
                RelationshipRole = "Peer",
                AllowNoReply = !string.Equals(surface, "messaging_chat", StringComparison.OrdinalIgnoreCase)
            }
        };
    }

    private static DecisionEngineV2 CreateEngine()
    {
        var mockRelationshipEngine = new Mock<IRelationshipIntelligenceEngine>();
        mockRelationshipEngine.Setup(engine => engine.Analyze(It.IsAny<RelationshipContext>()))
            .Returns(new SocialInsight
            {
                OpportunityScore = 0.5,
                RiskScore = 0.2
            });

        var mockAssembler = new Mock<IConversationContextAssembler>();
        mockAssembler.Setup(assembler => assembler.AssembleAsync(It.IsAny<AssembleAiContextRequest>(), It.IsAny<CancellationToken>()))
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
                InteractionMode = ResolveInteractionModeForTest(request),
                InteractionMetadata = request.InteractionMetadata
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

        return !string.IsNullOrWhiteSpace(request.SourceText) ? "reply" : "compose";
    }

    private static string MapMoveFamily(string move)
    {
        var normalized = (move ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "rewrite_user_intent" or "rewrite" or "polish" or "improve_draft" => "rewrite",
            "draft_post" or "compose_post" or "create_post" => "compose",
            "no_reply" => "no_reply",
            _ => "reply"
        };
    }

    private static bool ContainsUnsupportedNumber(string reply, DecisionV2Input input)
    {
        var source = string.Join(" ",
            input.SourceText ?? string.Empty,
            input.SourceTitle ?? string.Empty,
            input.ParentContextText ?? string.Empty,
            input.NearbyContextText ?? string.Empty);

        var numbers = Regex.Matches(reply, @"\b\d+(\.\d+)?%?\b")
            .Select(match => match.Value)
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

    private static bool ContainsForbiddenGenericPhrase(string reply)
    {
        var banned = new[]
        {
            "great post",
            "well said",
            "thanks for sharing",
            "your experience underscores",
            "you nailed",
            "what stayed with me",
            "point around",
            "spot on",
            "this is a useful point",
            "appreciate the update here"
        };

        return banned.Any(phrase => reply.Contains(phrase, StringComparison.OrdinalIgnoreCase));
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

    private static string FormatFailure(DecisionGoldenScenario scenario, DecisionV2Result result) =>
        $"Scenario={scenario.Name}; Surface={scenario.InputPayload.Surface}; Source='{scenario.InputPayload.SourceText}'; UserDraft='{scenario.InputPayload.Message}'; ExpectedSituation={scenario.ExpectedSituationType}; ExpectedFamily={scenario.ExpectedMoveFamily}; ActualSituation={result.SituationType}; ActualMove={result.Move}; ShouldReply={result.ShouldReply}; Reply='{result.Reply}'";

    private static string FormatRate(int passed, int total)
    {
        if (total == 0)
            return "n/a";

        return $"{(passed * 100.0 / total):0.0}%";
    }

    public sealed class DecisionGoldenScenario
    {
        public string Name { get; init; } = string.Empty;
        public string ExpectedSituationType { get; init; } = string.Empty;
        public string ExpectedMoveFamily { get; init; } = string.Empty;
        public bool ShouldReply { get; init; }
        public bool RequiresNoHallucination { get; init; }
        public bool RequiresComposeLength { get; init; }
        public bool RequiresChatCommandHandling { get; init; }
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
                Alternatives = []
            });

        public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default) =>
            AsyncEnumerable.Empty<string>();
    }
}
