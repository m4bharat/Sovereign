using System.Text.RegularExpressions;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class CandidateScoringEngine : ICandidateScoringEngine
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","a","an","and","or","but","if","then","than","that","this","those","these","to","for","from","of","in","on","at","with",
        "is","are","was","were","be","been","being","as","by","it","its","into","about","your","you","their","they","them","we","our",
        "i","me","my","mine","his","her","hers","he","she","him","not","just","more","most","very","really","truly","new","role"
    };

    public IReadOnlyList<CandidateScore> Score(
        IReadOnlyList<SocialMoveCandidate> candidates,
        SocialSituation situation,
        MessageContext context,
        RelationshipAnalysis relationshipAnalysis)
    {
        return candidates.Select(candidate => new CandidateScore
        {
            Candidate = candidate,
            Relevance = ScoreRelevance(context, candidate.Reply),
            SocialFit = ScoreSocialFit(situation, candidate.Move),
            Specificity = ScoreSpecificity(context, candidate.Reply),
            HallucinationPenalty = ScoreHallucinationPenalty(context, candidate.Reply),
            Tone = ScoreTone(candidate.Reply),
            Brevity = ScoreBrevity(candidate.Reply),
            RelationshipFit = ScoreRelationshipFit(relationshipAnalysis, candidate.Move),
            RiskAdjustedValue = ScoreRiskAdjustedValue(relationshipAnalysis, candidate.Move),
            TimingFit = ScoreTimingFit(relationshipAnalysis)
        }).ToArray();
    }

    private static double ScoreRelevance(MessageContext context, string reply)
    {
        var source = string.Join(" ",
            context.SourceText ?? string.Empty,
            context.SourceAuthor ?? string.Empty,
            context.SourceTitle ?? string.Empty,
            context.Message ?? string.Empty);

        var sourceTokens = Tokenize(source);
        var replyTokens = Tokenize(reply);

        if (replyTokens.Count == 0)
        {
            return 0.0;
        }

        var overlap = replyTokens.Count(token => sourceTokens.Contains(token));
        var ratio = overlap / (double)replyTokens.Count;

        return Math.Clamp(ratio * 2.0, 0.0, 1.0);
    }

    private static double ScoreSocialFit(SocialSituation situation, string move)
    {
        return situation.Type switch
        {
            "milestone" when move is "congratulate" or "congratulate_encourage" or "appreciate_journey" => 0.95,
            "educational" when move is "appreciate" or "add_insight" or "ask_relevant_question" => 0.92,
            "opinion" when move is "agree" or "add_nuance" or "ask_relevant_question" => 0.90,
            "question" when move is "answer_supportively" or "ask_relevant_question" => 0.90,
            _ when move is "appreciate" or "encourage" => 0.72,
            _ => 0.55
        };
    }

    private static double ScoreSpecificity(MessageContext context, string reply)
    {
        var score = 0.0;

        if (!string.IsNullOrWhiteSpace(context.SourceAuthor) &&
            reply.Contains(context.SourceAuthor, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.35;
        }

        var sourceKeywords = Tokenize(context.SourceText ?? string.Empty)
            .Take(12)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var replyTokens = Tokenize(reply);
        var overlap = replyTokens.Count(token => sourceKeywords.Contains(token));

        score += Math.Min(0.65, overlap * 0.12);

        return Math.Clamp(score, 0.0, 1.0);
    }

    private static double ScoreHallucinationPenalty(MessageContext context, string reply)
    {
        var source = string.Join(" ",
            context.SourceText ?? string.Empty,
            context.SourceAuthor ?? string.Empty,
            context.SourceTitle ?? string.Empty,
            context.Message ?? string.Empty);

        var suspiciousTerms = Regex.Matches(reply, @"\b[A-Z][a-zA-Z]{2,}\b")
            .Select(m => m.Value)
            .Where(term => !StopWords.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(term => !source.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Count();

        return suspiciousTerms switch
        {
            0 => 0.0,
            1 => 0.08,
            2 => 0.25,
            _ => 0.40
        };
    }

    private static double ScoreTone(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return 0.0;
        }

        if (IsMetaFeedback(reply))
        {
            return 0.10;
        }

        if (reply.Length > 280)
        {
            return 0.45;
        }

        if (reply.Contains("Congratulations", StringComparison.OrdinalIgnoreCase) ||
            reply.Contains("Appreciate", StringComparison.OrdinalIgnoreCase) ||
            reply.Contains("Strong point", StringComparison.OrdinalIgnoreCase) ||
            reply.Contains("Really clear", StringComparison.OrdinalIgnoreCase))
        {
            return 0.85;
        }

        return 0.70;
    }

    private static double ScoreBrevity(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return 0.0;
        }

        return reply.Length switch
        {
            <= 40 => 0.50,
            <= 180 => 0.95,
            <= 260 => 0.80,
            <= 360 => 0.55,
            _ => 0.30
        };
    }

    private static bool IsMetaFeedback(string reply)
    {
        var patterns = new[]
        {
            "this is a strong start",
            "you could improve",
            "consider adding",
            "make it more engaging",
            "you should add",
            "try to include",
            "to improve this post",
            "for linkedin, could you",
            "this post would be stronger",
            "add a specific example"
        };

        return patterns.Any(pattern => reply.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> Tokenize(string text)
    {
        return Regex.Matches(text.ToLowerInvariant(), "[a-z0-9]+")
            .Select(m => m.Value)
            .Where(token => token.Length > 2 && !StopWords.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static double ScoreRelationshipFit(RelationshipAnalysis analysis, string move)
    {
        if (analysis.PowerDifferential > 0.7 && move == "defer")
        {
            return 0.95;
        }

        if (analysis.MomentumScore < 0.3 && move == "reconnect")
        {
            return 0.90;
        }

        if (analysis.ReciprocityScore < 0.5 && move == "light_acknowledgment")
        {
            return 0.85;
        }

        return 0.70; // Default relationship fit score
    }

    private static double ScoreRiskAdjustedValue(RelationshipAnalysis analysis, string move)
    {
        var baseValue = move switch
        {
            "congratulate" => 0.85,
            "appreciate" => 0.80,
            "ask_relevant_question" => 0.75,
            _ => 0.60
        };

        var riskPenalty = analysis.RiskScore * 0.20;
        return Math.Clamp(baseValue - riskPenalty, 0.0, 1.0);
    }

    private static double ScoreTimingFit(RelationshipAnalysis analysis)
    {
        return analysis.ReplyUrgencyHint > 0.8 ? 0.90 : 0.70;
    }
}
