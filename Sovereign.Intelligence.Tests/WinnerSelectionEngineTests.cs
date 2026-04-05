using System.Collections.Generic;
using Xunit;
using Sovereign.Intelligence.Services;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Tests;

public class WinnerSelectionEngineTests
{
    private readonly WinnerSelectionEngine _engine = new();

    [Fact]
    public void SelectBest_ShouldReturnNoReply_WhenAllScoresBelowThreshold()
    {
        // Arrange
        var scores = new List<CandidateScore>
        {
            new CandidateScore { Candidate = new SocialMoveCandidate { Move = "praise" }, ComputedTotal = 0.4, Tone = 0.3, HallucinationPenalty = 0.2 },
            new CandidateScore { Candidate = new SocialMoveCandidate { Move = "congratulate" }, ComputedTotal = 0.44, Tone = 0.3, HallucinationPenalty = 0.2 }
        };

        // Act
        var result = _engine.SelectBest(scores, new MessageContext());

        // Assert
        Assert.Equal("no_reply", result.Winner.Move);
        Assert.Contains("No candidate met the minimum threshold", result.Winner.Rationale);
    }

    [Fact]
    public void SelectBest_ShouldReturnReply_WhenScoreJustAboveRelaxedThreshold()
    {
        // Arrange
        var scores = new List<CandidateScore>
        {
            new CandidateScore { Candidate = new SocialMoveCandidate { Move = "praise" }, ComputedTotal = 0.46, Tone = 0.3, HallucinationPenalty = 0.1 }
        };

        // Act
        var result = _engine.SelectBest(scores, new MessageContext());

        // Assert
        Assert.Equal("praise", result.Winner.Move);
    }

    [Fact]
    public void SelectBest_ShouldReturnTopCandidateAndAlternatives()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "congratulate", Reply = "Congrats!" },
            new SocialMoveCandidate { Move = "praise", Reply = "Great job!" },
            new SocialMoveCandidate { Move = "add_insight", Reply = "That's impressive!" }
        };

        var scores = new List<CandidateScore>
        {
            new CandidateScore { Candidate = candidates[0], ComputedTotal = 0.9, Tone = 0.8, HallucinationPenalty = 0.1 },
            new CandidateScore { Candidate = candidates[1], ComputedTotal = 0.8, Tone = 0.7, HallucinationPenalty = 0.1 },
            new CandidateScore { Candidate = candidates[2], ComputedTotal = 0.7, Tone = 0.6, HallucinationPenalty = 0.1 }
        };

        // Act
        var result = _engine.SelectBest(scores, new MessageContext());

        // Assert
        Assert.Equal("congratulate", result.Winner.Move);
        Assert.Equal(2, result.Alternatives.Count);
        Assert.Contains(result.Alternatives, a => a.Move == "praise");
        Assert.Contains(result.Alternatives, a => a.Move == "add_insight");
    }

    [Fact]
    public void SelectBest_ShouldPreferLowerRiskCandidateInTies()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "challenge", Reply = "Interesting perspective, but have you considered..." },
            new SocialMoveCandidate { Move = "add_insight", Reply = "That's a great point, and I'd add that..." }
        };

        var scores = new List<CandidateScore>
        {
            new CandidateScore { Candidate = candidates[0], ComputedTotal = 0.85, Tone = 0.8, HallucinationPenalty = 0.1, RiskAdjustedValue = 0.6 },
            new CandidateScore { Candidate = candidates[1], ComputedTotal = 0.85, Tone = 0.8, HallucinationPenalty = 0.1, RiskAdjustedValue = 0.8 }
        };

        // Act
        var result = _engine.SelectBest(scores, new MessageContext());

        // Assert
        Assert.Equal("add_insight", result.Winner.Move);
    }

    [Fact]
    public void SelectBest_ShouldFilterOutHallucinatedCandidates()
    {
        // Arrange
        var scores = new List<CandidateScore>
        {
            new CandidateScore { Candidate = new SocialMoveCandidate { Move = "congratulate" }, ComputedTotal = 0.8, Tone = 0.8, HallucinationPenalty = 0.5 },
            new CandidateScore { Candidate = new SocialMoveCandidate { Move = "praise" }, ComputedTotal = 0.7, Tone = 0.7, HallucinationPenalty = 0.1 }
        };

        // Act
        var result = _engine.SelectBest(scores, new MessageContext());

        // Assert
        Assert.Equal("praise", result.Winner.Move);
    }

    [Fact]
    public void SelectBest_ShouldFilterOutLowToneCandidates()
    {
        // Arrange
        var scores = new List<CandidateScore>
        {
            new CandidateScore { Candidate = new SocialMoveCandidate { Move = "congratulate" }, ComputedTotal = 0.8, Tone = 0.1, HallucinationPenalty = 0.1 },
            new CandidateScore { Candidate = new SocialMoveCandidate { Move = "praise" }, ComputedTotal = 0.7, Tone = 0.8, HallucinationPenalty = 0.1 }
        };

        // Act
        var result = _engine.SelectBest(scores, new MessageContext());

        // Assert
        Assert.Equal("praise", result.Winner.Move);
    }
}