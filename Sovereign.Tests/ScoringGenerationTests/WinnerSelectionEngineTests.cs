using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;
using Xunit;

namespace Sovereign.Tests.ScoringGenerationTests;

public class WinnerSelectionEngineTests
{
    private readonly WinnerSelectionEngine _engine = new();

    [Fact]
    public void SelectBest_ShouldAvoidNoReply_WhenRequiredSurfaceHasUsableAlternative()
    {
        var result = _engine.SelectBest(
            [
                new CandidateScore
                {
                    Candidate = new SocialMoveCandidate { Move = "no_reply", Reply = string.Empty },
                    ComputedTotal = 0.05
                },
                new CandidateScore
                {
                    Candidate = new SocialMoveCandidate { Move = "helpful_reply", Reply = "Thank you, I really appreciate it." },
                    ComputedTotal = 0.46
                }
            ],
            new SocialSituation { Type = "direct_message" },
            CreateContext("messaging_chat", "thank you really appreciate it", allowNoReply: false));

        Assert.Equal("helpful_reply", result.Winner.Move);
    }

    [Fact]
    public void SelectBest_ShouldSkipEmptyReply_WhenMoveIsNotNoReply()
    {
        var result = _engine.SelectBest(
            [
                new CandidateScore
                {
                    Candidate = new SocialMoveCandidate { Move = "rewrite_user_intent", Reply = string.Empty },
                    ComputedTotal = 0.80
                },
                new CandidateScore
                {
                    Candidate = new SocialMoveCandidate { Move = "respond_helpfully", Reply = "Thanks for reaching out. Happy to hear more." },
                    ComputedTotal = 0.60
                }
            ],
            new SocialSituation { Type = "direct_message" },
            CreateContext("messaging_chat", "reply", allowNoReply: false));

        Assert.Equal("respond_helpfully", result.Winner.Move);
    }

    [Fact]
    public void SelectBest_ShouldPreferRewrite_WhenGapIsSmallAndDraftExists()
    {
        var result = _engine.SelectBest(
            [
                new CandidateScore
                {
                    Candidate = new SocialMoveCandidate { Move = "helpful_reply", Reply = "Thank you, I appreciate it." },
                    ComputedTotal = 0.66
                },
                new CandidateScore
                {
                    Candidate = new SocialMoveCandidate { Move = "rewrite_user_intent", Reply = "Thank you, I really appreciate it." },
                    ComputedTotal = 0.62
                }
            ],
            new SocialSituation { Type = "rewrite_direct_message" },
            CreateContext("messaging_chat", "thank you really appreciate it", allowNoReply: false));

        Assert.Equal("rewrite_user_intent", result.Winner.Move);
    }

    private static MessageContext CreateContext(string surface, string message, bool allowNoReply)
    {
        return new MessageContext
        {
            Surface = surface,
            InteractionMode = surface == "messaging_chat" ? "chat" : "reply",
            Message = message,
            SourceText = "Thanks for making the intro yesterday.",
            InteractionMetadata = new Dictionary<string, string>
            {
                ["allow_no_reply"] = allowNoReply.ToString()
            }
        };
    }
}
