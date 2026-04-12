using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Sovereign.Domain.Models;
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

    [Fact]
    public void Score_ShouldPenalizeWeakCtaFormFillReplies()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate
            {
                Move = "answer_supportively",
                Reply = "Great question! I'm currently a junior developer, and the next skill I'm tackling is Kubernetes — excited to get some hands-on experience with clusters.",
                RequiresPolish = false
            },
            new SocialMoveCandidate
            {
                Move = "answer_supportively",
                Reply = "I'm currently coming from the developer side, and Kubernetes is the next area I'm focusing on. It feels like that's where DevOps concepts start becoming operational instead of theoretical.",
                RequiresPolish = false
            }
        };

        var situation = new SocialSituation { Type = "question" };
        var context = new MessageContext
        {
            Message = "Where are you right now and what are you learning next?",
            SourceText = "This is a CTA post — comment below where you are right now and what you're learning next.",
            ParentContextText = string.Empty,
            NearbyContextText = string.Empty
        };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.7, ReplyUrgencyHint = 0.5 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);
        var weakScore = scores.First(s => s.Candidate.Reply.StartsWith("Great question", StringComparison.OrdinalIgnoreCase));
        var strongScore = scores.First(s => s.Candidate.Reply.Contains("operational"));

        // Assert
        Assert.True(strongScore.Total > weakScore.Total, "Positioned CTA participation should score higher than weak form-fill replies.");
        Assert.True(weakScore.ParticipationWithoutPositionPenalty > 0.35, "Weak CTA reply should receive a substantial participation penalty.");
        Assert.True(strongScore.PositioningStrength > weakScore.PositioningStrength, "Strong CTA reply should have noticeably better positioning strength.");
    }

    [Fact]
    public void Score_ShouldPenalizeGenericPraiseReplies()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "praise", Reply = "Great job!", RequiresPolish = false },
            new SocialMoveCandidate { Move = "praise", Reply = "Awesome work!", RequiresPolish = false },
            new SocialMoveCandidate { Move = "praise", Reply = "Well done!", RequiresPolish = false }
        };

        var situation = new SocialSituation { Type = "milestone" };
        var context = new MessageContext
        {
            Message = "I completed the project",
            SourceText = "Just finished a major project at work!"
        };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.8 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        foreach (var score in scores)
        {
            Assert.True(score.GenericPenalty > 0.0, $"Generic reply '{score.Candidate.Reply}' should receive a penalty");
            Assert.True(score.Total < 0.5, $"Generic reply should have low total score due to penalty");
        }
    }

    [Fact]
    public void Score_ShouldPenalizeShortFillerReplies()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "acknowledge", Reply = "Nice", RequiresPolish = false },
            new SocialMoveCandidate { Move = "acknowledge", Reply = "Cool", RequiresPolish = false },
            new SocialMoveCandidate { Move = "acknowledge", Reply = "Ok", RequiresPolish = false }
        };

        var situation = new SocialSituation { Type = "opinion" };
        var context = new MessageContext
        {
            Message = "What do you think about this approach?",
            SourceText = "I'm considering a new approach to this problem."
        };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.6 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        foreach (var score in scores)
        {
            Assert.True(score.GenericPenalty > 0.0, $"Short filler reply '{score.Candidate.Reply}' should receive a penalty");
            Assert.True(score.Total < 0.3, $"Short filler reply should have very low total score");
        }
    }

    [Fact]
    public void Score_ShouldPreferActualAnswerOverGenericPraise_OnCtaPosts()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate
            {
                Move = "answer_question",
                Reply = "Great question! I'd recommend starting with the fundamentals and building up from there.",
                RequiresPolish = false
            },
            new SocialMoveCandidate
            {
                Move = "answer_question",
                Reply = "In my experience, the key is to focus on practical application rather than theory.",
                RequiresPolish = false
            }
        };

        var situation = new SocialSituation { Type = "cta_or_question" };
        var context = new MessageContext
        {
            Message = "How should I approach learning this technology?",
            SourceText = "I'm looking to learn a new technology. What approach would you recommend? Any advice on getting started?"
        };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.8 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        var genericScore = scores.First(s => s.Candidate.Reply.StartsWith("Great question", StringComparison.OrdinalIgnoreCase));
        var concreteScore = scores.First(s => s.Candidate.Reply.Contains("experience"));

        Assert.True(concreteScore.Total > genericScore.Total, "Concrete answer should score higher than generic praise on CTA posts.");
        Assert.True(genericScore.CtaParticipationPenalty > 0.0, "Generic praise should receive CTA participation penalty.");
        Assert.True(concreteScore.CtaParticipationPenalty < genericScore.CtaParticipationPenalty, "Concrete answer should have lower CTA penalty.");
    }

    [Fact]
    public void Score_ShouldPreferConcreteOpinionOverGenericEngagement_OnCtaPosts()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate
            {
                Move = "add_specific_insight",
                Reply = "I'd suggest considering the tradeoffs between monolithic and microservices architectures for your use case.",
                RequiresPolish = false
            },
            new SocialMoveCandidate
            {
                Move = "light_touch_question",
                Reply = "That's an interesting perspective. What challenges have you encountered?",
                RequiresPolish = false
            }
        };

        var situation = new SocialSituation { Type = "cta_or_question" };
        var context = new MessageContext
        {
            Message = "What do you think about this architecture decision?",
            SourceText = "We're deciding between monolithic and microservices. What are your thoughts on the tradeoffs?"
        };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.7 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        var concreteScore = scores.First(s => s.Candidate.Reply.Contains("tradeoffs"));
        var genericScore = scores.First(s => s.Candidate.Reply.Contains("interesting perspective"));

        Assert.True(concreteScore.Total > genericScore.Total, "Concrete opinion should score higher than generic engagement on CTA posts.");
        Assert.True(concreteScore.InsightDepth > genericScore.InsightDepth, "Concrete reply should have higher insight depth.");
    }

    [Fact]
    public void Score_ShouldPenalize_CommentStyleReply_OnChatSurface()
    {
        // Arrange
        var candidates = new List<SocialMoveCandidate>
        {
            new SocialMoveCandidate { Move = "respond_helpfully", Reply = "Great post! Loved it — check out my profile for more.", RequiresPolish = false },
            new SocialMoveCandidate { Move = "respond_helpfully", Reply = "Hey — congrats! Happy to help, let me know if you want resources.", RequiresPolish = false }
        };

        var situation = new SocialSituation { Type = "direct_message" };
        var context = new MessageContext
        {
            Message = "Hey, can you share more about X?",
            InteractionMode = "chat",
            SourceText = "Private chat message"
        };
        var relationship = new RelationshipAnalysis { ReciprocityScore = 0.7 };

        // Act
        var scores = _engine.Score(candidates, situation, context, relationship);

        // Assert
        var commentStyle = scores.First(s => s.Candidate.Reply.Contains("check out my profile"));
        var natural = scores.First(s => s.Candidate.Reply.Contains("happy to help"));

        Assert.True(natural.Total > commentStyle.Total, "Natural DM-style reply should score higher than comment-style broadcast in chat.");
        Assert.True(commentStyle.ChatStyleMismatchPenalty > 0.0, "Comment-style reply should receive a chat-style mismatch penalty.");
    }

    [Fact]
    public void Score_ShouldPrefer_RewriteUserIntent_ForRewriteDirectMessage()
    {
        var context = new MessageContext
        {
            InteractionMode = "chat",
            Message = "wish him thank you",
            InteractionMetadata = new System.Collections.Generic.Dictionary<string, string>
            {
                ["rewrite_intent"] = "True"
            }
        };

        var situation = new SocialSituation { Type = "rewrite_direct_message" };

        var rewrite = new SocialMoveCandidate
        {
            Move = "rewrite_user_intent",
            Reply = "Thanks so much — really appreciate it."
        };

        var generic = new SocialMoveCandidate
        {
            Move = "respond_helpfully",
            Reply = "Great point. Thanks for sharing."
        };

        var scores = _engine.Score(new[] { rewrite, generic }, situation, context, new RelationshipAnalysis());

        Assert.Equal("rewrite_user_intent", scores.OrderByDescending(x => x.Total).First().Candidate.Move);
    }

    [Fact]
    public void Score_ShouldDetect_WordBoundaryBasedChatPraise()
    {
        var candidates = new List<SocialMoveCandidate>
    {
        new() { Move = "respond_helpfully", Reply = "Nice work", RequiresPolish = false },
        new() { Move = "respond_helpfully", Reply = "Thanks so much — really appreciate it.", RequiresPolish = false }
    };

        var context = new MessageContext
        {
            InteractionMode = "chat",
            Message = "Wish him thank you"
        };

        var scores = _engine.Score(
            candidates,
            new SocialSituation { Type = "direct_message" },
            context,
            new RelationshipAnalysis());

        var praise = scores.First(s => s.Candidate.Reply == "Nice work");
        var dm = scores.First(s => s.Candidate.Reply.Contains("really appreciate it"));

        Assert.True(praise.ChatStyleMismatchPenalty > 0.0);
        Assert.True(dm.Total > praise.Total);
    }
}