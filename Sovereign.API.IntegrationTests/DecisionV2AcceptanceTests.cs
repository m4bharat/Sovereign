using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Sovereign.Domain.DTOs;
using Sovereign.Domain.Services;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.DecisionV2;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;
using Sovereign.Intelligence.Evaluation;
using Sovereign.Intelligence.Tests;

namespace Sovereign.Intelligence.Tests;

public class DecisionV2AcceptanceTests
{
    [Theory]
    [MemberData(nameof(GetGoldenScenarios))]
    public async Task DecideAsync_ShouldMatchGoldenScenario(GoldenScenario scenario)
    {
        // Arrange
        var mockRelationshipEngine = new Mock<IRelationshipIntelligenceEngine>();
        mockRelationshipEngine.Setup(e => e.Analyze(It.IsAny<RelationshipContext>()))
            .Returns(new SocialInsight
            {
                OpportunityScore = scenario.InputPayload.ReciprocityScore,
                RiskScore = scenario.InputPayload.PowerDifferential
            });

        var mockAssembler = new Mock<IConversationContextAssembler>();
        mockAssembler.Setup(a => a.AssembleAsync(It.IsAny<AssembleAiContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssembleAiContextRequest request, CancellationToken ct) => new MessageContext
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

        var realSituationDetector = new SocialSituationDetector();
        var realMovePlanner = new SocialMovePlanner();
        var realReplyGenerator = new CandidateReplyGenerator();
        var realScoringEngine = new CandidateScoringEngine();
        var realWinnerSelector = new WinnerSelectionEngine();
        var fakeLlmClient = new FakeDecisionV2LlmClient();
        var mockLogger = new Mock<ILogger<DecisionEngineV2>>();

        var engine = new DecisionEngineV2(
            mockAssembler.Object,
            mockRelationshipEngine.Object,
            realSituationDetector,
            realMovePlanner,
            realReplyGenerator,
            realScoringEngine,
            realWinnerSelector,
            fakeLlmClient,
            mockLogger.Object);

        // Act
        var result = await engine.DecideAsync(scenario.InputPayload);

        // Assert
        AssertReplyPolicy(scenario.ShouldReply, result);

        if (scenario.ShouldReply)
        {
            Assert.NotNull(result.Reply);
            Assert.NotEmpty(result.Reply);
            Assert.True(result.Reply.Length <= scenario.MaxReplyLength);

            AssertMoveFamily(scenario.ExpectedMoveFamily, scenario.AllowedMoveSynonyms, result);
            AssertAcceptableReplySoft(scenario.AcceptableReplies, result);

            var replyLower = result.Reply.ToLower();
            var hasForbiddenPattern = scenario.ForbiddenReplyPatterns.Any(pattern => replyLower.Contains(pattern.ToLower()));
            Assert.False(hasForbiddenPattern, $"Reply should not contain forbidden patterns: {string.Join(", ", scenario.ForbiddenReplyPatterns)}");

            var moveLower = result.Move.ToLower();
            var hasForbiddenBehavior = scenario.ForbiddenBehaviors
                .Any(forbidden => moveLower.Contains(forbidden.ToLower()));
            Assert.False(hasForbiddenBehavior, $"Move should not contain forbidden behaviors: {string.Join(", ", scenario.ForbiddenBehaviors)}");
        }
        else
        {
            Assert.Equal("no_reply", result.Move);
        }
    }

    public static IEnumerable<object[]> GetGoldenScenarios()
    {
        foreach (var scenario in GoldenScenarioDataset.GetAllScenarios())
        {
            yield return new object[] { scenario };
        }
    }

    [Fact]
    public async Task EvaluateGoldenScenarios_PrintAggregateSummary()
    {
        var mockRelationshipEngine = new Mock<IRelationshipIntelligenceEngine>();
        mockRelationshipEngine.Setup(e => e.Analyze(It.IsAny<RelationshipContext>()))
            .Returns(new SocialInsight());

        var mockAssembler = new Mock<IConversationContextAssembler>();
        mockAssembler.Setup(a => a.AssembleAsync(It.IsAny<AssembleAiContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssembleAiContextRequest request, CancellationToken ct) => new MessageContext
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

        var engine = new DecisionEngineV2(
            mockAssembler.Object,
            mockRelationshipEngine.Object,
            new SocialSituationDetector(),
            new SocialMovePlanner(),
            new CandidateReplyGenerator(),
            new CandidateScoringEngine(),
            new WinnerSelectionEngine(),
            new FakeDecisionV2LlmClient(),
            new Mock<ILogger<DecisionEngineV2>>().Object);

        var moveMismatchCount = 0;
        var replyPolicyMismatchCount = 0;
        var acceptableReplyMismatchCount = 0;

        foreach (var scenario in GoldenScenarioDataset.GetAllScenarios())
        {
            var result = await engine.DecideAsync(scenario.InputPayload);

            if (!IsMoveFamilyMatch(scenario.ExpectedMoveFamily, scenario.AllowedMoveSynonyms, result))
                moveMismatchCount++;

            if (!IsReplyPolicyMatch(scenario.ShouldReply, result))
                replyPolicyMismatchCount++;

            if (scenario.ShouldReply && !IsAcceptableReplyMatch(scenario.AcceptableReplies, result))
                acceptableReplyMismatchCount++;
        }

        Console.WriteLine($"Move mismatches: {moveMismatchCount}");
        Console.WriteLine($"Reply-policy mismatches: {replyPolicyMismatchCount}");
        Console.WriteLine($"Acceptable-reply mismatches: {acceptableReplyMismatchCount}");

        Assert.Equal(0, moveMismatchCount);
        Assert.Equal(0, replyPolicyMismatchCount);
        Assert.Equal(0, acceptableReplyMismatchCount);
    }

    private static void AssertMoveFamily(
        string expectedMoveFamily,
        IReadOnlyList<string> allowedMoveSynonyms,
        DecisionV2Result result)
    {
        Assert.False(string.IsNullOrWhiteSpace(expectedMoveFamily));

        var expected = NormalizeMove(expectedMoveFamily);
        var actual = NormalizeMove(result.Move);

        var isAllowed = expected == actual ||
            allowedMoveSynonyms.Any(alias => NormalizeMove(alias) == actual);

        Assert.True(isAllowed,
            $"Move '{result.Move}' should match expected '{expectedMoveFamily}' or allowed synonyms: {string.Join(", ", allowedMoveSynonyms)}");
    }

    private static string NormalizeMove(string move)
    {
        var value = (move ?? string.Empty).Trim().ToLowerInvariant();

        return value switch
        {
            "answer" => "answer_question",
            "insight" => "add_specific_insight",
            "light_touch" => "light_touch_question",
            _ => value
        };
    }

    private static void AssertAcceptableReplySoft(
        IReadOnlyList<string>? acceptableReplies,
        DecisionV2Result result)
    {
        if (acceptableReplies == null || acceptableReplies.Count == 0)
            return;

        var actual = NormalizeText(result.Reply ?? string.Empty);

        Assert.Contains(
            acceptableReplies,
            candidate => actual.Contains(NormalizeText(candidate), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeText(string text) =>
        string.Join(" ",
            (text ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static void AssertReplyPolicy(
        bool shouldReplyExpected,
        DecisionV2Result result)
    {
        Assert.Equal(shouldReplyExpected, result.ShouldReply);

        if (!shouldReplyExpected)
        {
            Assert.True(string.IsNullOrWhiteSpace(result.Reply));
        }
    }

    private static bool IsMoveFamilyMatch(
     string expectedMoveFamily,
     IReadOnlyList<string> allowedMoveSynonyms,
     DecisionV2Result result)
    {
        var expected = NormalizeMove(expectedMoveFamily);
        var actual = NormalizeMove(result.Move);

        return expected == actual ||
               allowedMoveSynonyms.Any(alias => NormalizeMove(alias) == actual);
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

    private static bool IsReplyPolicyMatch(bool shouldReplyExpected, DecisionV2Result result) =>
        shouldReplyExpected == result.ShouldReply &&
        (shouldReplyExpected || string.IsNullOrWhiteSpace(result.Reply));

    private static bool IsAcceptableReplyMatch(IReadOnlyList<string>? acceptableReplies, DecisionV2Result result)
    {
        if (acceptableReplies == null || acceptableReplies.Count == 0)
            return true;

        var actual = NormalizeText(result.Reply ?? string.Empty);
        return acceptableReplies.Any(candidate => actual.Contains(NormalizeText(candidate), StringComparison.OrdinalIgnoreCase));
    }
}
