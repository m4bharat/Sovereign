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
                InteractionMode = !string.IsNullOrWhiteSpace(request.SourceText) ? "reply" :
                    !string.IsNullOrWhiteSpace(request.ParentContextText) ? "chat" : "compose"
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
        Assert.Equal(scenario.ShouldReply, result.ShouldReply);

        if (scenario.ShouldReply)
        {
            Assert.NotNull(result.Reply);
            Assert.NotEmpty(result.Reply);
            Assert.True(result.Reply.Length <= scenario.MaxReplyLength);

            // Check that move matches expected family or allowed synonyms
            Assert.True(scenario.ExpectedMoveFamily == result.Move || scenario.AllowedMoveSynonyms.Contains(result.Move),
                $"Move '{result.Move}' should match expected '{scenario.ExpectedMoveFamily}' or synonyms: {string.Join(", ", scenario.AllowedMoveSynonyms)}");

            // Check that reply contains at least one acceptable token
            var replyLower = result.Reply.ToLower();
            var hasAcceptableToken = scenario.AcceptableReplies.Any(token => replyLower.Contains(token.ToLower()));
            Assert.True(hasAcceptableToken, $"Reply should contain at least one acceptable token from: {string.Join(", ", scenario.AcceptableReplies)}");

            // Check that reply does not contain forbidden patterns
            var hasForbiddenPattern = scenario.ForbiddenReplyPatterns.Any(pattern => replyLower.Contains(pattern.ToLower()));
            Assert.False(hasForbiddenPattern, $"Reply should not contain forbidden patterns: {string.Join(", ", scenario.ForbiddenReplyPatterns)}");

            // Check that reply does not contain forbidden behaviors
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
}