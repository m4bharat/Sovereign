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
        var detail = FormatFailure(scenario, result);

        Assert.True(
            string.Equals(result.SituationType, scenario.ExpectedSituationType, StringComparison.OrdinalIgnoreCase),
            $"Situation mismatch. {detail}");

        AssertMoveFamily(result.Move, scenario.ExpectedMoveFamily, detail);

        Assert.True(
            result.ShouldReply == scenario.ShouldReply,
            $"ShouldReply mismatch. {detail}");

        if (!scenario.ShouldReply)
        {
            Assert.True(
                string.IsNullOrWhiteSpace(result.Reply),
                $"Expected empty reply for no-reply scenario. {detail}");
            return;
        }

        var reply = result.Reply ?? string.Empty;

        Assert.True(!string.IsNullOrWhiteSpace(reply), $"Reply was empty. {detail}");
        Assert.True(reply.Length is >= 8 and <= 600, $"Reply length out of range ({reply.Length}). {detail}");

        foreach (var forbidden in scenario.ForbiddenPhrases)
        {
            Assert.True(
                !reply.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                $"Reply contained forbidden phrase '{forbidden}'. {detail}");
        }

        if (scenario.AcceptableReplies.Count > 0)
        {
            Assert.True(
                MatchesAcceptableReply(reply, scenario.AcceptableReplies),
                $"Reply did not match any acceptable answer. {detail}");
        }

        if (string.Equals(scenario.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(
                !ContainsUnsupportedNumber(reply, scenario.InputPayload),
                $"Feed reply contained unsupported numeric claim. {detail}");
            Assert.True(
                !ContainsUnsupportedClaimMarkers(reply, scenario.InputPayload),
                $"Feed reply contained unsupported factual framing. {detail}");
        }

        if (string.Equals(scenario.Surface, "messaging_chat", StringComparison.OrdinalIgnoreCase) &&
            IsCommandOnlyMessage(scenario.UserDraft))
        {
            Assert.True(
                !reply.StartsWith("reply", StringComparison.OrdinalIgnoreCase),
                $"Chat reply echoed command text. {detail}");
            Assert.True(
                !reply.Contains("linkedin", StringComparison.OrdinalIgnoreCase),
                $"Chat reply leaked platform wording. {detail}");
        }

        if (string.Equals(scenario.Surface, "start_post", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(reply.Length >= 120, $"Compose output was too short. {detail}");
            Assert.True(!LooksLikeComment(reply), $"Compose output looked like a comment. {detail}");
        }
    }

    public static IEnumerable<object[]> GetGoldenScenarios() =>
        BuildDataset().Select(scenario => new object[] { scenario });

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

    private static IReadOnlyList<GoldenScenario> BuildDataset() =>
    [
        Scenario("promotion_vp_announcement", "feed_reply", "Happy to share that I have stepped into the role of Vice President at Data Axle India.", "make a comment", "achievement_share", "reply", true, "Sunil", acceptableReplies: ["Congratulations", "Vice President"], forbiddenPhrases: CommonForbidden()),
        Scenario("new_job_thoughtworks", "feed_reply", "Happy to share that I have joined Thoughtworks. Grateful for the opportunity and excited for this new chapter.", "comment", "achievement_share", "reply", true, acceptableReplies: ["Congratulations", "Thoughtworks"], forbiddenPhrases: CommonForbidden()),
        Scenario("new_job_microsoft", "feed_reply", "Excited to start a new role at Microsoft as Principal Product Manager.", "make a comment", "achievement_share", "reply", true, acceptableReplies: ["Congratulations", "Microsoft"], forbiddenPhrases: CommonForbidden()),
        Scenario("promotion_director", "feed_reply", "Proud to share that I was promoted to Director this week after years of learning and support.", "write comment", "achievement_share", "reply", true, acceptableReplies: ["Congratulations", "Director"], forbiddenPhrases: CommonForbidden()),
        Scenario("hiring_platform_team", "feed_reply", "We are hiring across platform engineering and data infrastructure this quarter.", "comment", "group_announcement", "reply", true, acceptableReplies: ["Appreciate the update", "hiring"], forbiddenPhrases: CommonForbidden()),
        Scenario("hiring_sales_leader", "feed_reply", "We are hiring our first VP of Sales to build repeatable growth.", "make a comment", "group_announcement", "reply", true, acceptableReplies: ["Appreciate the update", "growth"], forbiddenPhrases: CommonForbidden()),
        Scenario("ai_thought_leadership_multimodel", "feed_reply", "Anthropic blocked an entire org this week. Multi model architecture fixes this by removing dependency on any single provider.", "comment", "industry_news", "reply", true, "Rajiv Kedia", acceptableReplies: ["portability", "operational", "provider"], forbiddenPhrases: InsightForbidden()),
        Scenario("ai_thought_leadership_agents", "feed_reply", "AI agents look impressive in demos, but production workflows fail when observability and control are weak.", "comment", "industry_news", "reply", true, acceptableReplies: ["control", "operational", "workflow"], forbiddenPhrases: InsightForbidden()),
        Scenario("api_education_post", "feed_reply", "5 ways to improve API performance in production with caching, query tuning, and connection pooling.", "comment", "educational", "reply", true, acceptableReplies: ["trade-off", "coordination", "operational"], forbiddenPhrases: CommonForbidden()),
        Scenario("leadership_opinion_clarity", "feed_reply", "The best leaders create clarity, not control.", "comment", "opinion", "reply", true, acceptableReplies: ["clarity", "judgment", "operational"], forbiddenPhrases: InsightForbidden()),
        Scenario("gratitude_post_community", "feed_reply", "Grateful for everyone who supported our launch this week. Thank you for the encouragement.", "comment", "personal_update", "reply", true, acceptableReplies: ["Appreciate", "support"], forbiddenPhrases: CommonForbidden()),
        Scenario("thank_you_customers", "feed_reply", "Thank you to our customers and partners for trusting us in year one.", "comment", "personal_update", "reply", true, acceptableReplies: ["Appreciate", "support"], forbiddenPhrases: CommonForbidden()),
        Scenario("product_launch_platform", "feed_reply", "We just launched our workflow platform after months of effort and customer feedback.", "comment", "achievement_share", "reply", true, acceptableReplies: ["Strong work", "launch", "result"], forbiddenPhrases: CommonForbidden()),
        Scenario("product_launch_mobile", "feed_reply", "Today we launched our mobile analytics app built for field teams.", "make a comment", "achievement_share", "reply", true, acceptableReplies: ["Strong work", "launch"], forbiddenPhrases: CommonForbidden()),
        Scenario("founder_story_origin", "feed_reply", "Three years ago I started this company after seeing how broken the onboarding experience was.", "comment", "reflection", "reply", true, acceptableReplies: ["complexity", "operational", "journey"], forbiddenPhrases: InsightForbidden()),
        Scenario("founder_story_bootstrap", "feed_reply", "Bootstrapping taught me that resilience matters more than momentum when markets turn.", "comment", "reflection", "reply", true, acceptableReplies: ["resilience", "trade-off", "operational"], forbiddenPhrases: InsightForbidden()),
        Scenario("low_context_generic_post", "feed_reply", "Big things coming soon. Stay tuned.", "comment", "low_signal", "no_reply", false),
        Scenario("motivation_quote_post", "feed_reply", "Monday motivation: believe in yourself and keep going.", "make a comment", "low_signal", "no_reply", false),
        Scenario("poll_scaling_teams", "feed_reply", "What is your biggest challenge scaling engineering teams? Share your thoughts below.", "comment", "cta_engagement", "reply", true, acceptableReplies: ["My take:", "start with"], forbiddenPhrases: CommonForbidden()),
        Scenario("direct_question_tech_debt", "feed_reply", "How do you manage technical debt in fast-growing startups?", "comment", "question", "reply", true, acceptableReplies: ["My take:", "start with"], forbiddenPhrases: CommonForbidden()),
        Scenario("networking_dm_thank_you", "messaging_chat", "Thanks for making the intro yesterday.", "reply", "direct_message", "reply", true, acceptableReplies: ["appreciate", "thanks"], forbiddenPhrases: ChatForbidden()),
        Scenario("birthday_dm_reply", "messaging_chat", "Happy belated birthday!", "reply", "holiday_greeting", "reply", true, acceptableReplies: ["Thank you so much", "Really appreciate"], forbiddenPhrases: ChatForbidden()),
        Scenario("generic_greeting_dm", "messaging_chat", "Hey! Hope you are doing well.", "reply", "greeting", "reply", true, acceptableReplies: ["Thank you", "appreciate"], forbiddenPhrases: ChatForbidden()),
        Scenario("reconnect_old_contact", "messaging_chat", "It has been a while. Would love to reconnect and hear what you are building now.", "reply", "direct_message", "reply", true, acceptableReplies: ["appreciate", "connect"], forbiddenPhrases: ChatForbidden()),
        Scenario("recruiter_outreach", "messaging_chat", "We are hiring for a senior backend role and your profile stood out.", "reply", "direct_message", "reply", true, acceptableReplies: ["Thanks", "appreciate", "interested"], forbiddenPhrases: ChatForbidden()),
        Scenario("customer_followup_dm", "messaging_chat", "Checking in to see if you are open to a quick conversation next week.", "reply", "direct_message", "reply", true, acceptableReplies: ["Thanks", "open", "happy"], forbiddenPhrases: ChatForbidden()),
        Scenario("chat_short_user_draft_rewrite", "messaging_chat", "Congrats on the new role!", "Thanks appreciate your note", "rewrite_direct_message", "rewrite", true, acceptableReplies: ["Thanks", "appreciate"], forbiddenPhrases: ChatForbidden()),
        Scenario("chat_recruiter_draft_rewrite", "messaging_chat", "Would love to chat about a staff engineer opportunity.", "Thanks, sounds interesting", "rewrite_direct_message", "rewrite", true, acceptableReplies: ["Thanks", "interesting"], forbiddenPhrases: ChatForbidden()),
        Scenario("compose_ai_trends_post", "start_post", "", "Write a LinkedIn post on AI trends", "compose_post", "compose", true, acceptableReplies: ["AI", "teams", "workflow"], forbiddenPhrases: ComposeForbidden()),
        Scenario("compose_hiring_post", "start_post", "", "Draft a hiring post for a senior backend engineer", "compose_post", "compose", true, acceptableReplies: ["team", "engineer", "role"], forbiddenPhrases: ComposeForbidden()),
        Scenario("compose_bullet_points_post", "start_post", "", "Write a LinkedIn post from these bullets: shipped onboarding revamp, reduced churn, learned the value of customer feedback", "compose_post", "compose", true, acceptableReplies: ["customer", "feedback", "churn"], forbiddenPhrases: ComposeForbidden()),
        Scenario("compose_meaningful_draft", "start_post", "", "Here is my draft: AI adoption is accelerating, but teams still struggle to connect pilots to measurable business outcomes. Improve this into a LinkedIn post.", "compose_post", "compose", true, acceptableReplies: ["AI", "business", "outcomes"], forbiddenPhrases: ComposeForbidden()),
        Scenario("compose_product_strategy", "start_post", "", "Draft a LinkedIn post on product strategy for AI workflows", "compose_post", "compose", true, acceptableReplies: ["product", "AI", "workflow"], forbiddenPhrases: ComposeForbidden()),
        Scenario("feed_rewrite_draft_promotion", "feed_reply", "Happy to share that I was promoted to Director this week.", "Congrats! Great work on this milestone.", "rewrite_feed_reply", "rewrite", true, acceptableReplies: ["milestone", "progress"], forbiddenPhrases: CommonForbidden()),
        Scenario("feed_rewrite_draft_launch", "feed_reply", "We launched our new platform today.", "Amazing launch and strong progress", "rewrite_feed_reply", "rewrite", true, acceptableReplies: ["launch", "progress"], forbiddenPhrases: CommonForbidden()),
        Scenario("irrelevant_announcement_no_reply", "feed_reply", "Our office pantry is now stocked with sparkling water.", "comment", "low_signal", "no_reply", false),
        Scenario("unsafe_personal_context_no_reply", "feed_reply", "After months of medical treatment, I am slowly recovering and taking things day by day.", "comment", "sensitive", "no_reply", false),
        Scenario("political_context_no_reply", "feed_reply", "If you disagree with this election result, you are part of the problem.", "comment", "controversial_no_reply", "no_reply", false),
        Scenario("user_explicit_ignore_no_reply", "feed_reply", "Excited to share our new funding news.", "ignore", "low_signal", "no_reply", false),
        Scenario("layoff_context_no_reply", "feed_reply", "Today I was laid off after the company restructured.", "comment", "sensitive", "no_reply", false),
        Scenario("funding_announcement", "feed_reply", "Excited to announce our Series A funding and grateful to the team who made it possible.", "comment", "achievement_share", "reply", true, acceptableReplies: ["Congratulations", "milestone", "team"], forbiddenPhrases: CommonForbidden()),
        Scenario("open_to_work_post", "feed_reply", "Open to work and actively exploring senior product roles.", "comment", "job_search", "reply", true, acceptableReplies: ["direction", "momentum", "opportunity"], forbiddenPhrases: CommonForbidden())
    ];

    private static GoldenScenario Scenario(
        string name,
        string surface,
        string sourceText,
        string userDraft,
        string expectedSituationType,
        string expectedMoveFamily,
        bool shouldReply,
        string sourceAuthor = "",
        string sourceTitle = "",
        IReadOnlyList<string>? acceptableReplies = null,
        IReadOnlyList<string>? forbiddenPhrases = null,
        bool? allowNoReply = null)
    {
        return new GoldenScenario
        {
            Name = name,
            Surface = surface,
            SourceText = sourceText,
            UserDraft = userDraft,
            SourceAuthor = sourceAuthor,
            SourceTitle = sourceTitle,
            ExpectedSituationType = expectedSituationType,
            ExpectedMoveFamily = expectedMoveFamily,
            ShouldReply = shouldReply,
            AcceptableReplies = acceptableReplies?.ToList() ?? [],
            ForbiddenPhrases = forbiddenPhrases?.ToList() ?? [],
            InputPayload = new DecisionV2Input
            {
                UserId = "user-001",
                ContactId = name,
                Message = userDraft,
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
                AllowNoReply = allowNoReply ?? !string.Equals(surface, "messaging_chat", StringComparison.OrdinalIgnoreCase)
            }
        };
    }

    private static void AssertMoveFamily(string actualMove, string expectedFamily, string detail)
    {
        var actualFamily = MapMoveFamily(actualMove);
        var expected = (expectedFamily ?? string.Empty).Trim().ToLowerInvariant();

        Assert.True(
            string.Equals(actualFamily, expected, StringComparison.OrdinalIgnoreCase),
            $"Move family mismatch. Expected '{expected}', got move '{actualMove}' mapped to '{actualFamily}'. {detail}");
    }

    private static string MapMoveFamily(string move)
    {
        var normalized = NormalizeMove(move);

        if (normalized is "rewrite_user_intent" or "rewrite" or "polish" or "improve_draft")
            return "rewrite";

        if (normalized is "draft_post" or "compose_post" or "create_post")
            return "compose";

        if (normalized is "no_reply")
            return "no_reply";

        return "reply";
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

    private static bool MatchesAcceptableReply(string reply, IReadOnlyList<string> acceptableReplies)
    {
        foreach (var acceptable in acceptableReplies)
        {
            if (string.IsNullOrWhiteSpace(acceptable))
                continue;

            if (reply.Contains(acceptable, StringComparison.OrdinalIgnoreCase))
                return true;

            var expectedTokens = Tokenize(acceptable);
            var replyTokens = Tokenize(reply);
            if (expectedTokens.Count == 0 || replyTokens.Count == 0)
                continue;

            var overlap = expectedTokens.Count(token => replyTokens.Contains(token));
            if (overlap >= Math.Max(1, (int)Math.Ceiling(expectedTokens.Count * 0.6)))
                return true;
        }

        return false;
    }

    private static HashSet<string> Tokenize(string text) =>
        Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9][a-z0-9'-]{1,}")
            .Select(match => match.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string FormatFailure(GoldenScenario scenario, DecisionV2Result result) =>
        $"Scenario={scenario.Name}; Surface={scenario.Surface}; Source='{scenario.SourceText}'; UserDraft='{scenario.UserDraft}'; ExpectedSituation={scenario.ExpectedSituationType}; ExpectedFamily={scenario.ExpectedMoveFamily}; ActualSituation={result.SituationType}; ActualMove={result.Move}; ShouldReply={result.ShouldReply}; Reply='{result.Reply}'";

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

    private static bool LooksLikeComment(string reply)
    {
        var trimmed = reply.Trim();
        return trimmed.Length < 120 || Regex.Matches(trimmed, @"[.!?]").Count < 2;
    }

    private static IReadOnlyList<string> CommonForbidden() =>
        ["great post", "well said", "thanks for sharing", "what stayed with me", "point around", "spot on"];

    private static IReadOnlyList<string> InsightForbidden() =>
        ["great post", "well said", "thanks for sharing", "what stayed with me", "point around", "spot on", "very insightful"];

    private static IReadOnlyList<string> ChatForbidden() =>
        ["linkedin", "point around", "reply."];

    private static IReadOnlyList<string> ComposeForbidden() =>
        ["great post", "well said", "thanks for sharing", "point around linkedin", "reply."];

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
