using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;
using Xunit;

namespace Sovereign.Tests.ScoringGenerationTests;

public class CandidateReplyGeneratorTests
{
    private readonly CandidateReplyGenerator _generator = new();

    [Fact]
    public void Generate_ShouldIncludeSourceAnchor_ForFeedReply()
    {
        var result = _generator.Generate(
            [new SocialMoveCandidate { Move = "add_insight" }],
            new MessageContext
            {
                Surface = "feed_reply",
                InteractionMode = "reply",
                Message = "comment",
                SourceTitle = "Multi-model resilience",
                SourceText = "Multi model architecture reduces provider dependency and improves resilience."
            });

        var reply = Assert.Single(result).Reply;
        Assert.Contains("provider", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("great post", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_ShouldPreserveUserIntent_ForMessagingRewrite()
    {
        var result = _generator.Generate(
            [new SocialMoveCandidate { Move = "rewrite_user_intent" }],
            new MessageContext
            {
                Surface = "messaging_chat",
                InteractionMode = "chat",
                Message = "thank you really appreciate it"
            });

        var reply = Assert.Single(result).Reply;
        Assert.Contains("Thank", reply, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("appreciate", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("linkedin", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_ShouldCreateNonEmptyPost_ForStartPost()
    {
        var result = _generator.Generate(
            [new SocialMoveCandidate { Move = "draft_post" }],
            new MessageContext
            {
                Surface = "start_post",
                InteractionMode = "compose",
                Message = "Write a LinkedIn post on AI workflow evaluation and ownership"
            });

        var reply = Assert.Single(result).Reply;
        Assert.True(reply.Length >= 120);
        Assert.Contains("workflow", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_ShouldAvoidForcedCta_WhenNotRequested()
    {
        var result = _generator.Generate(
            [new SocialMoveCandidate { Move = "add_insight" }],
            new MessageContext
            {
                Surface = "feed_reply",
                InteractionMode = "reply",
                Message = "comment",
                SourceText = "Execution quality matters more than model hype."
            });

        var reply = Assert.Single(result).Reply;
        Assert.DoesNotContain("What do you think?", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Thoughts?", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Agree?", reply, StringComparison.OrdinalIgnoreCase);
    }
}
