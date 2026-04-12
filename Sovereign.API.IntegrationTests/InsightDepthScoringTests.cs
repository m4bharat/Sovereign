using System;
using System.Collections.Generic;
using Xunit;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;

namespace Sovereign.API.IntegrationTests;

public class InsightDepthScoringTests
{
    private readonly CandidateScoringEngine _engine = new();

    [Fact]
    public void Score_PenalizesGenericPraise_OnOpinionPost()
    {
        // Arrange - create candidates: one with generic praise, one with insight
        var genericPraiseCandidate = new SocialMoveCandidate
        {
            Move = "appreciate",
            Reply = "Great post! Well said. I totally agree."
        };

        var insightCandidate = new SocialMoveCandidate
        {
            Move = "add_insight",
            Reply = "Interesting perspective. I'd add that this intersects at a key constraint: systems at scale face coordination tradeoffs that aren't obvious."
        };

        var candidates = new[] { genericPraiseCandidate, insightCandidate };

        var situation = new SocialSituation { Type = "opinion" };
        var context = new MessageContext
        {
            SourceText = "What's your take on distributed systems?"
        };
        var relationshipAnalysis = new RelationshipAnalysis { ReplyUrgencyHint = 0.5 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationshipAnalysis);

        // Assert - insight candidate should score higher than generic praise
        var genericScore = scores[0].Total;
        var insightScore = scores[1].Total;

        Assert.True(insightScore > genericScore,
            $"Insight candidate (score: {insightScore:F3}) should score higher than generic praise (score: {genericScore:F3})");
    }

    [Fact]
    public void Score_DetectsInsightDepth_WithSystemThinking()
    {
        // Arrange
        var insightfulReply = new SocialMoveCandidate
        {
            Move = "answer_supportively",
            Reply = "The tradeoff here is that at scale, you face a coordination constraint. The classic case of coupling growing as systems mature."
        };

        var candidates = new[] { insightfulReply };

        var situation = new SocialSituation { Type = "question" };
        var context = new MessageContext
        {
            SourceText = "How do you handle scaling?"
        };
        var relationshipAnalysis = new RelationshipAnalysis { ReplyUrgencyHint = 0.5 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationshipAnalysis);

        // Assert - InsightDepth should be positive (not zero)
        Assert.True(scores[0].InsightDepth > 0.0,
            $"InsightDepth should be > 0 for system-thinking reply, but got {scores[0].InsightDepth:F3}");
    }

    [Fact]
    public void Score_AppliesGenericPraisePenalty_ToTotal()
    {
        // Arrange
        var praiseCandidate = new SocialMoveCandidate
        {
            Move = "appreciate",
            Reply = "Great post! Very insightful and important reminder."
        };

        var candidates = new[] { praiseCandidate };

        var situation = new SocialSituation { Type = "educational" };
        var context = new MessageContext
        {
            SourceText = "Tips for better code reviews"
        };
        var relationshipAnalysis = new RelationshipAnalysis { ReplyUrgencyHint = 0.5 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationshipAnalysis);

        // Assert - GenericPraisePenalty should be positive, and Total should be reduced by it
        Assert.True(scores[0].GenericPraisePenalty > 0.0,
            $"GenericPraisePenalty should be > 0 for generic praise, but got {scores[0].GenericPraisePenalty:F3}");
        
        // Verify penalty was applied to Total (penalty is subtracted, so Total should be lower than it would be without penalty)
        Assert.True(scores[0].Total < 1.0,
            $"Total should be < 1.0 after penalty application, but got {scores[0].Total:F3}");
    }

    [Fact]
    public void Score_NoInsightDepth_ForPraiseOnlyReply()
    {
        // Arrange
        var praiseOnlyCandidate = new SocialMoveCandidate
        {
            Move = "praise",
            Reply = "Great! Nice work! So true!"
        };

        var candidates = new[] { praiseOnlyCandidate };

        var situation = new SocialSituation { Type = "achievement" };
        var context = new MessageContext
        {
            SourceText = "Announced achievement"
        };
        var relationshipAnalysis = new RelationshipAnalysis { ReplyUrgencyHint = 0.5 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationshipAnalysis);

        // Assert - InsightDepth should be low/penalized for praise-only reply
        Assert.True(scores[0].InsightDepth < 0.15,
            $"InsightDepth should be < 0.15 for praise-only reply, but got {scores[0].InsightDepth:F3}");
    }
}
