using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
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

        var realSituationDetector = new SocialSituationDetector();
        var realMovePlanner = new SocialMovePlanner();
        var realReplyGenerator = new CandidateReplyGenerator();
        var realScoringEngine = new CandidateScoringEngine();
        var realWinnerSelector = new WinnerSelectionEngine();
        var fakeLlmClient = new FakeDecisionV2LlmClient();
        var mockLogger = new Mock<ILogger<DecisionEngineV2>>();

        var engine = new DecisionEngineV2(
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

        // Assert - enforce tightened acceptance harness
        AssertGoldenScenarioMatch(result, scenario);
    }

    /// <summary>
    /// Enforces tightened acceptance harness: move-family correctness, reply-quality grounding,
    /// and forbidden-behavior checks (both move and reply).
    /// </summary>
    private static void AssertGoldenScenarioMatch(DecisionV2Result result, GoldenScenario scenario)
    {
        // Primary gate: ShouldReply correctness
        Assert.Equal(scenario.ShouldReply, result.ShouldReply);

        if (!scenario.ShouldReply)
        {
            // No-reply case: hard assertion that move is no_reply
            Assert.Equal("no_reply", result.Move);
            return;
        }

        // Reply-expected case: enforce all dimensions in order
        
        // 1. Move-family correctness first
        var moveMatches = MatchesExpectedMoveFamily(result.Move, scenario.ExpectedMoveFamily, scenario.AllowedMoveSynonyms);
        Assert.True(moveMatches, 
            $"Move '{result.Move}' should match family '{scenario.ExpectedMoveFamily}' or synonyms: {string.Join(", ", scenario.AllowedMoveSynonyms)}");

        // 2. Non-empty, bounded reply
        Assert.NotNull(result.Reply);
        Assert.NotEmpty(result.Reply);
        Assert.True(result.Reply.Length <= scenario.MaxReplyLength,
            $"Reply length {result.Reply.Length} exceeds max {scenario.MaxReplyLength}");

        // 3. Reply-quality grounding: contains acceptable signal
        var hasAcceptableSignal = ContainsAcceptableReplySignal(result.Reply, scenario.AcceptableReplies);
        Assert.True(hasAcceptableSignal,
            $"Reply missing acceptable tokens from: {string.Join(", ", scenario.AcceptableReplies)}");

        // 4. Forbidden-behavior checks in both move and reply (stricter)
        var hasForbiddenBehavior = ContainsForbiddenBehavior(result.Move, result.Reply, scenario.ForbiddenBehaviors);
        Assert.False(hasForbiddenBehavior,
            $"Forbidden behaviors found in move or reply: {string.Join(", ", scenario.ForbiddenBehaviors)}");

        // 5. Forbidden reply patterns
        var hasForbiddenPattern = scenario.ForbiddenReplyPatterns.Any(pattern =>
            result.Reply.Contains(pattern, System.StringComparison.OrdinalIgnoreCase));
        Assert.False(hasForbiddenPattern,
            $"Reply contains forbidden patterns: {string.Join(", ", scenario.ForbiddenReplyPatterns)}");
    }

    /// <summary>
    /// Checks if result.Move matches the expected move family, accounting for allowed synonyms.
    /// </summary>
    private static bool MatchesExpectedMoveFamily(string resultMove, string expectedFamily, List<string> allowedSynonyms)
    {
        if (resultMove.Equals(expectedFamily, System.StringComparison.OrdinalIgnoreCase))
            return true;

        return allowedSynonyms != null && allowedSynonyms.Any(syn =>
            resultMove.Equals(syn, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if result.Reply contains at least one acceptable token or phrase.
    /// Grounds reply quality against the scenario's domain-specific acceptable list.
    /// </summary>
    private static bool ContainsAcceptableReplySignal(string reply, List<string> acceptableReplies)
    {
        if (string.IsNullOrEmpty(reply) || acceptableReplies == null || acceptableReplies.Count == 0)
            return false;

        var replyLower = reply.ToLower();
        return acceptableReplies.Any(token =>
            replyLower.Contains(token.ToLower(), System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if result.Move or result.Reply contains any forbidden behavior.
    /// Scans both move label and reply text for stricter forbidden-behavior enforcement.
    /// This catches patterns like "generic_praise" in replies, not just move names.
    /// </summary>
    private static bool ContainsForbiddenBehavior(string move, string reply, List<string> forbiddenBehaviors)
    {
        if (forbiddenBehaviors == null || forbiddenBehaviors.Count == 0)
            return false;

        var moveLower = move.ToLower();
        var replyLower = (reply ?? string.Empty).ToLower();

        return forbiddenBehaviors.Any(forbidden =>
        {
            var forbiddenLower = forbidden.ToLower();
            // Check both move and reply for forbidden behavior patterns
            return moveLower.Contains(forbiddenLower, System.StringComparison.OrdinalIgnoreCase) ||
                   replyLower.Contains(forbiddenLower, System.StringComparison.OrdinalIgnoreCase);
        });
    }

    public static IEnumerable<object[]> GetGoldenScenarios()
    {
        foreach (var scenario in GoldenScenarioDataset.GetAllScenarios())
        {
            yield return new object[] { scenario };
        }
    }
}