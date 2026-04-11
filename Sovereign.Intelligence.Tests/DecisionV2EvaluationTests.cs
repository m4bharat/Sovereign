// DecisionV2EvaluationTests.cs
// This file contains regression tests for DecisionV2 orchestration.

using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Sovereign.Domain.DTOs;
using Sovereign.Domain.Services;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.DecisionV2;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Services;
using Microsoft.Extensions.Logging;

namespace Sovereign.Intelligence.Tests
{
    public class DecisionV2EvaluationTests
    {
        [Fact]
        public async Task DecideAsync_ShouldReturnNoReply_WhenWinnerMoveIsNoReply()
        {
            // Arrange
            var mockRelationshipEngine = new Mock<IRelationshipIntelligenceEngine>();
            mockRelationshipEngine.Setup(e => e.Analyze(It.IsAny<RelationshipContext>()))
                .Returns(new SocialInsight { OpportunityScore = 0.5, RiskScore = 0.5 });

            var mockSituationDetector = new Mock<ISocialSituationDetector>();
            mockSituationDetector.Setup(d => d.Detect(It.IsAny<MessageContext>()))
                .Returns(new SocialSituation { Type = "general" });

            var mockMovePlanner = new Mock<ISocialMovePlanner>();
            var moves = new List<SocialMoveCandidate>
            {
                new SocialMoveCandidate { Move = "no_reply", Rationale = "Test", Reply = "", RequiresPolish = false }
            };
            mockMovePlanner.Setup(p => p.Plan(It.IsAny<SocialSituation>(), It.IsAny<RelationshipAnalysis>()))
                .Returns(moves);

            var mockReplyGenerator = new Mock<ICandidateReplyGenerator>();
            mockReplyGenerator.Setup(g => g.Generate(It.IsAny<IReadOnlyList<SocialMoveCandidate>>(), It.IsAny<MessageContext>()))
                .Returns(moves);

            var mockScoringEngine = new Mock<ICandidateScoringEngine>();
            var scores = new List<CandidateScore>
            {
                new CandidateScore { Candidate = moves[0], ComputedTotal = 0.8 }
            };
            mockScoringEngine.Setup(s => s.Score(It.IsAny<IReadOnlyList<SocialMoveCandidate>>(), It.IsAny<SocialSituation>(), It.IsAny<MessageContext>(), It.IsAny<RelationshipAnalysis>()))
                .Returns(scores);

            var mockWinnerSelector = new Mock<IWinnerSelectionEngine>();
            mockWinnerSelector.Setup(w => w.SelectBest(It.IsAny<IReadOnlyList<CandidateScore>>(), It.IsAny<MessageContext>()))
                .Returns(new WinnerSelectionResult { Winner = moves[0], Alternatives = new List<SocialMoveCandidate>() });

            var mockAssembler = new Mock<IConversationContextAssembler>();
            mockAssembler.Setup(a => a.AssembleAsync(It.IsAny<AssembleAiContextRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MessageContext
                {
                    UserId = "user1",
                    ContactId = "contact1",
                    Message = "test",
                    InteractionMode = "compose",
                    Platform = "LinkedIn"
                });

            var mockLlmClient = new Mock<ILlmClient>();
            var mockLogger = new Mock<ILogger<DecisionEngineV2>>();

            var engine = new DecisionEngineV2(
                mockAssembler.Object,
                mockRelationshipEngine.Object,
                mockSituationDetector.Object,
                mockMovePlanner.Object,
                mockReplyGenerator.Object,
                mockScoringEngine.Object,
                mockWinnerSelector.Object,
                mockLlmClient.Object,
                mockLogger.Object);

            var input = new DecisionV2Input { UserId = "user1", ContactId = "contact1", Message = "test" };

            // Act
            var result = await engine.DecideAsync(input);

            // Assert
            Assert.False(result.ShouldReply);
            Assert.Equal("no_reply", result.Move);
        }

        [Fact]
        public async Task DecideAsync_ShouldPolishWinner_WhenRequiresPolishIsTrue()
        {
            // Arrange
            var mockRelationshipEngine = new Mock<IRelationshipIntelligenceEngine>();
            mockRelationshipEngine.Setup(e => e.Analyze(It.IsAny<RelationshipContext>()))
                .Returns(new SocialInsight { OpportunityScore = 0.5, RiskScore = 0.5 });

            var mockSituationDetector = new Mock<ISocialSituationDetector>();
            mockSituationDetector.Setup(d => d.Detect(It.IsAny<MessageContext>()))
                .Returns(new SocialSituation { Type = "general" });

            var mockMovePlanner = new Mock<ISocialMovePlanner>();
            var moves = new List<SocialMoveCandidate>
            {
                new SocialMoveCandidate { Move = "reply", Rationale = "Test", Reply = "original", RequiresPolish = true }
            };
            mockMovePlanner.Setup(p => p.Plan(It.IsAny<SocialSituation>(), It.IsAny<RelationshipAnalysis>()))
                .Returns(moves);

            var mockReplyGenerator = new Mock<ICandidateReplyGenerator>();
            mockReplyGenerator.Setup(g => g.Generate(It.IsAny<IReadOnlyList<SocialMoveCandidate>>(), It.IsAny<MessageContext>()))
                .Returns(moves);

            var mockScoringEngine = new Mock<ICandidateScoringEngine>();
            var scores = new List<CandidateScore>
            {
                new CandidateScore { Candidate = moves[0], ComputedTotal = 0.8 }
            };
            mockScoringEngine.Setup(s => s.Score(It.IsAny<IReadOnlyList<SocialMoveCandidate>>(), It.IsAny<SocialSituation>(), It.IsAny<MessageContext>(), It.IsAny<RelationshipAnalysis>()))
                .Returns(scores);

            var mockWinnerSelector = new Mock<IWinnerSelectionEngine>();
            mockWinnerSelector.Setup(w => w.SelectBest(It.IsAny<IReadOnlyList<CandidateScore>>(), It.IsAny<MessageContext>()))
                .Returns(new WinnerSelectionResult { Winner = moves[0], Alternatives = new List<SocialMoveCandidate>() });

            var mockAssembler = new Mock<IConversationContextAssembler>();
            mockAssembler.Setup(a => a.AssembleAsync(It.IsAny<AssembleAiContextRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MessageContext
                {
                    UserId = "user1",
                    ContactId = "contact1",
                    Message = "test",
                    InteractionMode = "compose",
                    Platform = "LinkedIn"
                });

            var mockLlmClient = new Mock<ILlmClient>();
            mockLlmClient.Setup(c => c.CompleteDecisionV2Async(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new DecisionV2Result { Reply = "polished reply", Confidence = 0.9, Rationale = "polished", Alternatives = new List<string>() });

            var mockLogger = new Mock<ILogger<DecisionEngineV2>>();

            var engine = new DecisionEngineV2(
                mockAssembler.Object,
                mockRelationshipEngine.Object,
                mockSituationDetector.Object,
                mockMovePlanner.Object,
                mockReplyGenerator.Object,
                mockScoringEngine.Object,
                mockWinnerSelector.Object,
                mockLlmClient.Object,
                mockLogger.Object);

            var input = new DecisionV2Input { UserId = "user1", ContactId = "contact1", Message = "test" };

            // Act
            var result = await engine.DecideAsync(input);

            // Assert
            Assert.True(result.ShouldReply);
            Assert.Equal("polished reply", result.Reply);
            Assert.Equal(0.9, result.Confidence);
        }
    }
}