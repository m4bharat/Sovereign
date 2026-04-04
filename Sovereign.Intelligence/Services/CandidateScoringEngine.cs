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

    private static readonly string[] InsightSignals =
    {
        "trade-off", "tradeoff", "second-order", "second order",
        "constraint", "constraints", "system-level", "system level",
        "coupling", "bottleneck", "latency", "coordination overhead",
        "feasibility", "execution", "scale", "cost", "infra",
        "architecture", "operational", "downstream", "failure mode",
        "systemic", "surface-level", "reframe", "blindness"
    };

    private static readonly string[] ReframeSignals =
    {
        "this is a classic case of",
        "the gap is",
        "the missing piece is",
        "what breaks down is",
        "the real constraint is",
        "what looks simple",
        "the difference between",
        "you see this a lot when"
    };

    private static readonly string[] GenericPraisePhrases =
    {
        "great post",
        "well said",
        "thanks for sharing",
        "nice breakdown",
        "great breakdown",
        "good point",
        "so true",
        "totally agree",
        "great perspective",
        "love this",
        "very insightful",
        "important reminder",
        "really drives home",
        "makes the connection obvious",
        "clear breakdown"
    };

    private static double Clamp01(double value) => Math.Clamp(value, 0.0, 1.0);

    public IReadOnlyList<CandidateScore> Score(
        IReadOnlyList<SocialMoveCandidate> candidates,
        SocialSituation situation,
        MessageContext context,
        RelationshipAnalysis relationshipAnalysis)
    {
        return candidates.Select(candidate =>
        {
            var score = new CandidateScore
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
                TimingFit = ScoreTimingFit(relationshipAnalysis, candidate.Move),
                InsightDepth = CalculateInsightDepth(candidate, context),
                GenericPraisePenalty = CalculateGenericPraisePenalty(candidate, context),
                EngagementCost = CalculateEngagementCost(candidate, relationshipAnalysis)
            };

            if (IsDisqualifiedAsGenericPraise(candidate, context, score))
            {
                score.Total = 0.0;
            }

            return score;
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
            "milestone" when move is "congratulate" or "congratulate_encourage" or "appreciate_journey" => 0.96,
            "educational" when move is "appreciate" or "add_insight" or "ask_relevant_question" => 0.92,
            "opinion" when move is "agree" or "add_nuance" or "add_insight" or "ask_relevant_question" => 0.94,
            "question" when move is "answer_supportively" => 0.98,
            "question" when move is "ask_relevant_question" => 0.92,
            "update" when move == "acknowledge" || move == "appreciate" => 0.88,
            "direct_message" when move == "direct_message" || move == "engage_privately" || move == "ask_details" => 0.94,
            "celebratory" when move == "defer" || move == "congratulate" || move == "praise" => 0.94,
            "achievement" when move == "light_touch" || move == "praise" || move == "congratulate" => 0.92,
            "reflection" when move == "engage" => 0.88,
            _ when move == "appreciate" || move == "encourage" => 0.72,
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

        var allowedCapitalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Congratulations", "Congrats", "Thanks", "Thank", "Thankyou", "Well",
            "Best", "Good", "Amazing", "Outstanding", "Strong", "Happy", "Prosperous"
        };

        var suspiciousTerms = Regex.Matches(reply, @"\b[A-Z][a-zA-Z]{2,}\b")
            .Select(m => m.Value)
            .Where(term => !StopWords.Contains(term))
            .Where(term => !allowedCapitalized.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(term => !source.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Count();

        return suspiciousTerms switch
        {
            0 => 0.0,
            1 => 0.06,
            2 => 0.18,
            _ => 0.30
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

        if (analysis.MomentumScore > 0.75 && move == "congratulate_encourage")
        {
            return 0.92;
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

    private static double ScoreTimingFit(RelationshipAnalysis analysis, string move)
    {
        if (string.Equals(move, "follow_up", StringComparison.OrdinalIgnoreCase))
        {
            return analysis.ReplyUrgencyHint > 0.8 ? 0.70 : 0.55;
        }

        if (string.Equals(move, "congratulate", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(move, "congratulate_encourage", StringComparison.OrdinalIgnoreCase))
        {
            return analysis.ReplyUrgencyHint > 0.8 ? 0.95 : 0.75;
        }

        return analysis.ReplyUrgencyHint > 0.8 ? 0.90 : 0.70;
    }

    /// <summary>
    /// Scores the depth of insight in a reply.
    /// High scores indicate system-level thinking, constraints, reframing, or substantial extensions beyond praise.
    /// </summary>
    private static double CalculateInsightDepth(SocialMoveCandidate candidate, MessageContext context)
    {
        var reply = (candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        double score = 0.0;

        var insightKeywordHits = InsightSignals.Count(s => reply.Contains(s));
        score += Math.Min(0.45, insightKeywordHits * 0.08);

        var reframeHits = ReframeSignals.Count(s => reply.Contains(s));
        score += Math.Min(0.25, reframeHits * 0.12);

        if (reply.Contains("because") || reply.Contains("when") || reply.Contains("where"))
            score += 0.10;

        if (reply.Length > 80 && reply.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Length >= 12)
            score += 0.08;

        var source = $"{context.SourceTitle ?? string.Empty} {context.SourceText ?? string.Empty}".ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(source))
        {
            var sourceTokens = Tokenize(source);
            var overlap = sourceTokens.Count(t => reply.Contains(t, StringComparison.OrdinalIgnoreCase));
            if (overlap >= 2)
                score += 0.12;
        }

        if (IsMostlyPraise(reply))
            score -= 0.35;

        return Clamp01(score);
    }

    /// <summary>
    /// Scores generic praise penalty. Higher scores indicate more generic/empty praise.
    /// This penalty is applied to the total when generic praise patterns are detected.
    /// </summary>
    private static double CalculateGenericPraisePenalty(SocialMoveCandidate candidate, MessageContext context)
    {
        var reply = (candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        double penalty = 0.0;

        var phraseHits = GenericPraisePhrases.Count(p => reply.Contains(p));
        penalty += Math.Min(0.65, phraseHits * 0.18);

        if (IsMostlyPraise(reply))
            penalty += 0.25;

        var situation = (context.SituationType ?? string.Empty).ToLowerInvariant();
        if ((situation == "opinion" || situation == "educational") && phraseHits > 0)
            penalty += 0.20;

        if ((candidate.Move?.Contains("insight", StringComparison.OrdinalIgnoreCase) ?? false) &&
            CalculateInsightDepth(candidate, context) < 0.18)
        {
            penalty += 0.20;
        }

        return Clamp01(penalty);
    }

    /// <summary>
    /// Scores engagement cost penalty. Higher values indicate long, verbose replies on weak relationships.
    /// </summary>
    private static double CalculateEngagementCost(SocialMoveCandidate candidate, RelationshipAnalysis relationship)
    {
        var reply = candidate.Reply ?? string.Empty;
        var words = reply.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Length;

        double cost = 0.0;

        if (words > 45)
            cost += 0.20;
        if (words > 70)
            cost += 0.20;

        if (relationship != null)
        {
            if (relationship.ReciprocityScore < 0.35 && words > 35)
                cost += 0.20;

            if (relationship.PowerDifferential > 0.75 && words > 30)
                cost += 0.15;
        }

        return Clamp01(cost);
    }

    private bool IsDisqualifiedAsGenericPraise(
        SocialMoveCandidate candidate,
        MessageContext context,
        CandidateScore score)
    {
        var situation = (context.SituationType ?? string.Empty).ToLowerInvariant();

        var requiresInsight =
            situation == "opinion" ||
            situation == "educational" ||
            situation == "analysis";

        if (!requiresInsight)
            return false;

        return score.GenericPraisePenalty >= 0.45 &&
               score.InsightDepth <= 0.15 &&
               score.Specificity <= 0.20;
    }

    /// <summary>
    /// Detects if a reply is mostly praise with no insight signals.
    /// Returns true if reply contains 2+ praise tokens and no insight signals.
    /// </summary>
    private static bool IsMostlyPraise(string reply)
    {
        var cleaned = reply.Trim().ToLowerInvariant();
        var words = cleaned.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        if (words.Length <= 6 && GenericPraisePhrases.Any(p => cleaned.Contains(p)))
            return true;

        var praiseTokens = new[]
        {
            "great", "nice", "good", "insightful", "important",
            "clear", "well", "true", "love", "thanks"
        };

        var praiseCount = words.Count(w => praiseTokens.Contains(w.Trim('.', ',', '!', '?')));
        var conceptSignals = InsightSignals.Count(s => cleaned.Contains(s)) +
                             ReframeSignals.Count(s => cleaned.Contains(s));

        return praiseCount >= 2 && conceptSignals == 0;
    }
}

