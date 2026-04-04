using System;
using System.Collections.Generic;
using Xunit;
using Sovereign.Intelligence.Services;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Tests;

public class CandidateScoringEngineTests
{
    private readonly CandidateScoringEngine _engine = new();

    [Fact]
    public void Score_ShouldPreferSpecificReplyOverGeneric()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "congratulate", Reply = "Congratulations on your promotion!", RequiresPolish = false },
            new SocialMoveCandidate { Move = "praise", Reply = "Great job!", RequiresPolish = false }
        };

        var situation = new SocialSituation { Type = "milestone" };
        var context = new MessageContext
        {
            Message = "I got promoted to senior engineer",
            SourceText = "Excited to share that I was promoted to senior engineer today!"
        };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.8 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        var congratulateScore = scores.First(s => s.Candidate.Move == "congratulate");
        var praiseScore = scores.First(s => s.Candidate.Move == "praise");
        Assert.True(congratulateScore.Total > praiseScore.Total, "Specific reply should score higher than generic");
    }

    [Fact]
    public void Score_ShouldPreferInsightOverGenericPraise_OnOpinionPosts()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate
            {
                Move = "appreciate",
                Reply = "Thanks for the clear breakdown — it really drives home how essential context is.",
                RequiresPolish = false
            },
            new SocialMoveCandidate
            {
                Move = "add_insight",
                Reply = "This is a classic systems problem: ideas that sound simple collapse when they hit infrastructure, cost, and coordination constraints.",
                RequiresPolish = false
            }
        };

        var situation = new SocialSituation { Type = "opinion" };
        var context = new MessageContext
        {
            Message = "What do you think about systemic risk in platform engineering?",
            SourceText = "When systems grow, coordination cost and hidden constraints can become the real failure mode."
        };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.7, ReplyUrgencyHint = 0.5 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        var genericScore = scores.First(s => s.Candidate.Move == "appreciate");
        var insightScore = scores.First(s => s.Candidate.Move == "add_insight");

        Assert.True(insightScore.Total > genericScore.Total,
            "Insightful reply should score higher than generic praise on opinion posts.");
        Assert.True(genericScore.GenericPraisePenalty >= 0.30,
            "Generic praise should receive a meaningful penalty.");
        Assert.True(insightScore.InsightDepth > genericScore.InsightDepth,
            "Insight reply should have higher InsightDepth than generic praise.");
    }

    [Fact]
    public void Score_ShouldPenalizeHallucinatedSpecifics()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "congratulate", Reply = "Congratulations on your Nobel Prize win!", RequiresPolish = false },
            new SocialMoveCandidate { Move = "congratulate", Reply = "Congratulations on your achievement!", RequiresPolish = false }
        };

        var situation = new SocialSituation { Type = "milestone" };
        var context = new MessageContext
        {
            Message = "I finished my project",
            SourceText = "Just wrapped up a big project at work."
        };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.5 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        Assert.True(scores[1].Total > scores[0].Total, "Grounded reply should beat hallucinated");
        Assert.True(scores[0].HallucinationPenalty > scores[1].HallucinationPenalty);
    }

    [Fact]
    public void Score_ShouldPreferSaferMoveOverHighRisk()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "congratulate", Reply = "Congrats!", RequiresPolish = false },
            new SocialMoveCandidate { Move = "challenge", Reply = "That seems risky, are you sure?", RequiresPolish = false }
        };

        var situation = new SocialSituation { Type = "milestone" };
        var context = new MessageContext { Message = "I got a new job" };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.3 }; // Weak tie

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        Assert.True(scores[0].Total > scores[1].Total, "Safer move should score higher for weak ties");
        Assert.True(scores[0].RiskAdjustedValue > scores[1].RiskAdjustedValue);
    }

    [Fact]
    public void Score_ShouldPenalizeTimingMisalignedMoves()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "congratulate", Reply = "Congrats!", RequiresPolish = false },
            new SocialMoveCandidate { Move = "follow_up", Reply = "How did it go?", RequiresPolish = false }
        };

        var situation = new SocialSituation { Type = "milestone" };
        var context = new MessageContext { Message = "I just got promoted" };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.9, ReplyUrgencyHint = 0.9 }; // Recent interaction

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        Assert.True(scores[0].TimingFit > scores[1].TimingFit, "Congratulate should have better timing fit for fresh milestone");
    }

    [Fact]
    public void Score_ShouldBoostRelationshipAwareMoves()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "congratulate_encourage", Reply = "Congrats! Keep pushing forward.", RequiresPolish = false },
            new SocialMoveCandidate { Move = "congratulate", Reply = "Congrats!", RequiresPolish = false }
        };

        var situation = new SocialSituation { Type = "milestone" };
        var context = new MessageContext { Message = "I achieved my goal" };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.9, MomentumScore = 0.8, ReplyUrgencyHint = 0.9 }; // Strong, supportive relationship

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        Assert.True(scores[0].Total > scores[1].Total, "Relationship-aware move should score higher");
        Assert.True(scores[0].RelationshipFit > scores[1].RelationshipFit);
    }
}