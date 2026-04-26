using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;
using Xunit;

namespace Sovereign.Tests.ScoringGenerationTests;

public class CandidateScoringEngineTests
{
    private readonly CandidateScoringEngine _engine = new();

    [Fact]
    public void Score_ShouldCapNoReply_WhenFeedReplyMustRespond()
    {
        var scores = _engine.Score(
            [
                new SocialMoveCandidate { Move = "no_reply", Reply = string.Empty },
                new SocialMoveCandidate { Move = "rewrite_user_intent", Reply = "Congratulations. The Director milestone feels well earned." }
            ],
            new SocialSituation { Type = "rewrite_feed_reply" },
            CreateContext("feed_reply", "Congrats on the promotion", "Happy to share that I was promoted to Director."),
            new RelationshipAnalysis());

        Assert.Equal(0.05, scores.Single(score => score.Candidate.Move == "no_reply").ComputedTotal, 3);
        Assert.True(scores.Single(score => score.Candidate.Move == "rewrite_user_intent").ComputedTotal >= 0.62);
    }

    [Fact]
    public void Score_ShouldPreferContextualReply_OverGenericReply()
    {
        var scores = _engine.Score(
            [
                new SocialMoveCandidate { Move = "add_insight", Reply = "Great post!" },
                new SocialMoveCandidate { Move = "add_insight", Reply = "The part about provider redundancy matters because orchestration quality determines whether multi model routing actually holds up." }
            ],
            new SocialSituation { Type = "industry_news" },
            CreateContext("feed_reply", "comment", "Multi model routing reduces provider dependency and improves resilience."),
            new RelationshipAnalysis());

        var generic = scores.Single(score => score.Candidate.Reply == "Great post!");
        var contextual = scores.Single(score => score.Candidate.Reply.Contains("provider redundancy", StringComparison.OrdinalIgnoreCase));

        Assert.True(contextual.ComputedTotal > generic.ComputedTotal);
        Assert.True(generic.GenericPenalty >= 0.30);
        Assert.True(contextual.ComputedTotal >= 0.60);
    }

    [Fact]
    public void Score_ShouldFloorRewrite_WhenDraftExists()
    {
        var scores = _engine.Score(
            [new SocialMoveCandidate { Move = "rewrite_user_intent", Reply = "Thank you, I really appreciate it." }],
            new SocialSituation { Type = "rewrite_direct_message" },
            CreateContext("messaging_chat", "thank you really appreciate it", "Thanks for making the intro"),
            new RelationshipAnalysis());

        Assert.True(scores.Single().ComputedTotal >= 0.62);
    }

    private static MessageContext CreateContext(string surface, string message, string sourceText)
    {
        return new MessageContext
        {
            Surface = surface,
            InteractionMode = surface == "messaging_chat" ? "chat" : surface == "start_post" ? "compose" : "reply",
            Message = message,
            SourceText = sourceText,
            ParentContextText = sourceText,
            InteractionMetadata = new Dictionary<string, string>
            {
                ["allow_no_reply"] = "false"
            }
        };
    }
}
