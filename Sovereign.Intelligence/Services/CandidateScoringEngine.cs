using System.Text.RegularExpressions;
using Sovereign.Domain.Models;
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

    private static readonly string[] CtaSignals =
    {
        "drop in the comments",
        "comment below",
        "let me know",
        "tell me",
        "where are you right now",
        "which skill",
        "what are you learning next",
        "what are you working on",
        "share in the comments",
        "comment your",
        "reply with",
        "what's your next step"
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

    private static readonly string[] GenericPhrases =
    {
        "great point",
        "great post",
        "well said",
        "so important",
        "thanks for sharing",
        "completely agree",
        "love this",
        "very insightful",
        "this is so true",
        "so true",
        "good one",
        "nice post"
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
                GenericPenalty = ComputeGenericPenalty(candidate.Reply, context),
                EngagementCost = CalculateEngagementCost(candidate, relationshipAnalysis),
                QuestionQuality = CalculateQuestionQuality(candidate, context),
                CTAResponseQuality = CalculateCTAResponseQuality(candidate, context),
                PositioningStrength = CalculatePositioningStrength(candidate, context),
                ParticipationWithoutPositionPenalty = CalculateParticipationWithoutPositionPenalty(candidate, context),
                CtaParticipationPenalty = ComputeCtaParticipationPenalty(candidate, context),
                ChatStyleMismatchPenalty = 0.0,
                ChatNaturalnessBoost = 0.0
            };
            candidate.GenericPenalty = score.GenericPenalty;

            // Compute chat-specific adjustments and include them in the score
            var chatStyleMismatch = ComputeChatStyleMismatchPenalty(candidate, context);
            var chatNaturalnessBoost = ComputeChatNaturalnessBoost(candidate, context);
            score.ChatStyleMismatchPenalty = chatStyleMismatch;
            score.ChatNaturalnessBoost = chatNaturalnessBoost;

            score.ComputedTotal = ComputeTotal(score, context);

            var specificityBoost = ComputeSpecificityBoost(candidate.Reply, context);
            score.ComputedTotal += specificityBoost;

            var rewriteIntentBoost = ComputeRewriteIntentBoost(candidate, context, situation);
            score.RewriteIntentBoost = rewriteIntentBoost;
            score.ComputedTotal += rewriteIntentBoost;

            if (IsDisqualifiedAsGenericPraise(candidate, context, score) ||
                IsDisqualifiedForCtaPost(candidate, context, score) ||
                (IsCtaEngagementPost(context) && !MeetsCtaThresholds(score)))
            {
                score.ComputedTotal = 0.0;
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

    private static double ComputeGenericPenalty(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0;

        var text = reply.Trim().ToLowerInvariant();

        var bannedPatterns = new[]
        {
            "great point",
            "great post",
            "well said",
            "so important",
            "thanks for sharing",
            "completely agree",
            "love this",
            "very insightful",
            "this is so true"
        };

        var penalty = 0.0;

        foreach (var pattern in bannedPatterns)
        {
            if (text.Contains(pattern))
            {
                penalty += 0.18;
            }
        }

        if (text.Length < 12)
        {
            penalty += 0.10;
        }

        return Math.Min(penalty, 0.45);
    }

    private static double ComputeChatStyleMismatchPenalty(
        SocialMoveCandidate candidate,
        MessageContext context)
    {
        if (!((context.InteractionMode ?? string.Empty).Equals("chat", StringComparison.OrdinalIgnoreCase)))
            return 0.0;

        var reply = (candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();

        // Heuristics: penalize broadcast/comment-like phrasing in chat
        if (reply.StartsWith("great post") || reply.Contains("thanks for sharing") || reply.Contains("check out"))
            return 0.40;

        if (reply.Length > 20 && reply.Length < 140 && Regex.IsMatch(reply, @"\b(great|nice|amazing|congrats|well done)\b"))
            return 0.25;

        return 0.0;
    }

    private static double ComputeChatNaturalnessBoost(
        SocialMoveCandidate candidate,
        MessageContext context)
    {

        if (!((context.InteractionMode ?? string.Empty).Equals("chat", StringComparison.OrdinalIgnoreCase)))
            return 0.0;

        var reply = (candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();

        if (Regex.IsMatch(reply, @"\b(i'm|i am|i'll|i'd|i've)\b") || reply.Contains("let me know") || reply.Contains("happy to"))
            return 0.20;

        if (reply.Length > 0 && reply.Length <= 200 && !Regex.IsMatch(reply, @"\b(as per|in conclusion|furthermore)\b"))
            return 0.05;

        if (Regex.IsMatch(reply, @"\b(i'm|i am|i'll|i'd|i've)\b") ||
                reply.Contains("let me know") ||
                reply.Contains("happy to") ||
                reply.Contains("appreciate it") ||
                reply.Contains("thanks so much") ||
                reply.Contains("sounds good") ||
                reply.Contains("sure"))
                    {
                        return 0.20;
                    }

        return 0.0;
    }

    private static double ComputeRewriteIntentBoost(
        SocialMoveCandidate candidate,
        MessageContext context,
        SocialSituation situation)
    {
        if (!string.Equals(situation.Type, "rewrite_direct_message", StringComparison.OrdinalIgnoreCase))
            return 0.0;

        if (string.Equals(candidate.Move, "rewrite_user_intent", StringComparison.OrdinalIgnoreCase))
            return 0.20;

        if (string.Equals(candidate.Move, "respond_helpfully", StringComparison.OrdinalIgnoreCase))
            return 0.08;

        return 0.0;
    }

    private static double ComputeGenericPenalty(
        string? reply,
        MessageContext context)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        var text = reply.Trim().ToLowerInvariant();
        var penalty = 0.0;

        foreach (var phrase in GenericPhrases)
        {
            if (text.Contains(phrase))
            {
                penalty += 0.18;
            }
        }

        // Very short praise-only replies are usually low value.
        if (text.Length < 20)
        {
            penalty += 0.04;
        }

        // If the reply does not reference anything specific while source text exists,
        // it is more likely to be generic filler.
        if (!string.IsNullOrWhiteSpace(context.SourceText))
        {
            var source = context.SourceText.ToLowerInvariant();

            var hasSpecificOverlap =
                source.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                      .Where(w => w.Length >= 5)
                      .Distinct()
                      .Any(w => text.Contains(w));

            if (!hasSpecificOverlap)
            {
                penalty += 0.10;
            }
        }

        return Math.Min(penalty, 0.45);
    }

    private static double ComputeSpecificityBoost(
        string? reply,
        MessageContext context)
    {
        if (string.IsNullOrWhiteSpace(reply) || string.IsNullOrWhiteSpace(context.SourceText))
            return 0.0;

        var text = reply.Trim().ToLowerInvariant();
        var source = context.SourceText.ToLowerInvariant();

        var overlapCount = source.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 5)
            .Distinct()
            .Count(w => text.Contains(w));

        if (overlapCount >= 3) return 0.12;
        if (overlapCount >= 2) return 0.08;
        if (overlapCount >= 1) return 0.04;

        return 0.0;
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

    private static bool LooksLikeCtaOrQuestion(MessageContext context)
    {
        var source = (context.SourceText ?? string.Empty).Trim().ToLowerInvariant();
        var draft = (context.Message ?? string.Empty).Trim().ToLowerInvariant();

        if (source.Contains("?") || draft.Contains("?"))
            return true;

        var ctaMarkers = new[]
        {
            "how ",
            "what ",
            "why ",
            "curious",
            "would love to hear",
            "thoughts?",
            "any advice",
            "looking for",
            "seeking",
            "can anyone",
            "has anyone",
            "what do you think",
            "would you do",
            "anyone else"
        };

        foreach (var marker in ctaMarkers)
        {
            if (source.Contains(marker) || draft.Contains(marker))
                return true;
        }

        return false;
    }

    private static double ComputeCtaParticipationPenalty(
        SocialMoveCandidate candidate,
        MessageContext context)
    {
        if (!LooksLikeCtaOrQuestion(context))
            return 0.0;

        var reply = (candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();

        var weakPatterns = new[]
        {
            "great question",
            "interesting question",
            "curious to hear",
            "following",
            "great point",
            "well said",
            "thanks for sharing"
        };

        var penalty = 0.0;

        foreach (var pattern in weakPatterns)
        {
            if (reply.Contains(pattern))
                penalty += 0.20;
        }

        var hasAnswerShape =
            reply.Contains("because") ||
            reply.Contains("i think") ||
            reply.Contains("in my experience") ||
            reply.Contains("one way") ||
            reply.Contains("the tradeoff") ||
            reply.Contains("for example");

        if (!hasAnswerShape)
            penalty += 0.12;

        return Math.Min(penalty, 0.45);
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

    private static double CalculateCTAResponseQuality(SocialMoveCandidate candidate, MessageContext context)
    {
        var reply = (candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        double score = 0.0;

        if (reply.Contains("i'm") || reply.Contains("i am") || reply.Contains("coming from") || reply.Contains("currently"))
            score += 0.12;

        var answersRole = reply.Contains("developer") ||
                          reply.Contains("sysadmin") ||
                          reply.Contains("fresher") ||
                          reply.Contains("cloud") ||
                          reply.Contains("engineer");

        var answersNextSkill = reply.Contains("kubernetes") ||
                               reply.Contains("terraform") ||
                               reply.Contains("docker") ||
                               reply.Contains("observability") ||
                               reply.Contains("sre") ||
                               reply.Contains("security") ||
                               reply.Contains("ci/cd") ||
                               reply.Contains("platform");

        if (answersRole)
            score += 0.10;
        if (answersNextSkill)
            score += 0.10;

        if (reply.Contains("because") || reply.Contains("feels like") || reply.Contains("where") || reply.Contains("so that"))
            score += 0.18;

        var conceptSignals = new[]
        {
            "orchestration", "reliability", "deployment", "production",
            "scale", "infra", "systems thinking", "delivery",
            "automation", "observability", "platform", "execution"
        };

        var conceptHits = conceptSignals.Count(c => reply.Contains(c));
        score += Math.Min(0.25, conceptHits * 0.07);

        if (reply.Contains(".") || reply.Contains("—") || reply.Contains(":"))
            score += 0.10;

        if (reply.Contains("excited to") || reply.Contains("looking forward to"))
            score -= 0.15;

        return Clamp01(score);
    }

    private static double CalculatePositioningStrength(SocialMoveCandidate candidate, MessageContext context)
    {
        var reply = (candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        double score = 0.0;

        var positioningSignals = new[]
        {
            "the inflection point",
            "where devops becomes",
            "where it starts becoming real",
            "beyond just code",
            "thinking in terms of",
            "from writing code to",
            "production reality",
            "real-world delivery",
            "operational",
            "systems thinking",
            "that’s the layer where",
            "the shift is",
            "the real challenge is"
        };

        var signalHits = positioningSignals.Count(s => reply.Contains(s));
        score += Math.Min(0.45, signalHits * 0.12);

        if (reply.Contains("instead of") || reply.Contains("rather than") || reply.Contains("not just"))
            score += 0.15;

        if (reply.Contains("focusing on") && (reply.Contains("because") || reply.Contains("where")))
            score += 0.15;

        var conceptNouns = new[]
        {
            "reliability", "orchestration", "deployment", "delivery",
            "production", "automation", "infrastructure", "systems", "operations"
        };

        var nounHits = conceptNouns.Count(n => reply.Contains(n));
        score += Math.Min(0.20, nounHits * 0.05);

        return Clamp01(score);
    }

    private static double CalculateParticipationWithoutPositionPenalty(SocialMoveCandidate candidate, MessageContext context)
    {
        var reply = (candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        double penalty = 0.0;

        var hasRoleAnswer =
            reply.Contains("i'm currently") ||
            reply.Contains("i am currently") ||
            reply.Contains("coming from") ||
            reply.Contains("i'm a") ||
            reply.Contains("i am a");

        var hasSkillAnswer =
            reply.Contains("next skill") ||
            reply.Contains("i'm learning") ||
            reply.Contains("i am learning") ||
            reply.Contains("i'm focusing on") ||
            reply.Contains("the next skill") ||
            reply.Contains("kubernetes") ||
            reply.Contains("terraform") ||
            reply.Contains("docker");

        var hasReasoning =
            reply.Contains("because") ||
            reply.Contains("feels like") ||
            reply.Contains("where") ||
            reply.Contains("the shift is") ||
            reply.Contains("that’s where") ||
            reply.Contains("that's where") ||
            reply.Contains("not just");

        var hasConcept =
            reply.Contains("orchestration") ||
            reply.Contains("reliability") ||
            reply.Contains("deployment") ||
            reply.Contains("production") ||
            reply.Contains("infra") ||
            reply.Contains("systems");

        if (hasRoleAnswer && hasSkillAnswer && !hasReasoning && !hasConcept)
            penalty += 0.50;

        if (reply.Contains("great question"))
            penalty += 0.20;

        if (reply.Contains("excited to") && !hasReasoning && !hasConcept)
            penalty += 0.25;

        var words = reply.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (words <= 18 && hasRoleAnswer && hasSkillAnswer && !hasReasoning && !hasConcept)
            penalty += 0.20;

        return Clamp01(penalty);
    }

    private static bool IsCtaEngagementPost(MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceTitle ?? string.Empty,
            context.SourceText ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty)
            .ToLowerInvariant();

        return CtaSignals.Any(signal => source.Contains(signal));
    }

    private static bool IsDisqualifiedForCtaPost(
        SocialMoveCandidate candidate,
        MessageContext context,
        CandidateScore score)
    {
        if (!IsCtaEngagementPost(context))
            return false;

        var reply = (candidate.Reply ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        if (score.ParticipationWithoutPositionPenalty >= 0.45 &&
            score.PositioningStrength <= 0.16 &&
            score.InsightDepth <= 0.16)
        {
            return true;
        }

        if (reply.StartsWith("Great question", StringComparison.OrdinalIgnoreCase) &&
            score.PositioningStrength <= 0.18 &&
            score.CTAResponseQuality <= 0.30)
        {
            return true;
        }

        return false;
    }

    private static bool MeetsCtaThresholds(CandidateScore score)
    {
        return score.CTAResponseQuality >= 0.24 &&
               (score.PositioningStrength >= 0.18 || score.InsightDepth >= 0.18) &&
               score.ParticipationWithoutPositionPenalty <= 0.40;
    }

    private static double ComputeTotal(CandidateScore score, MessageContext context)
    {
        if (IsCtaEngagementPost(context))
        {
            return (0.18 * score.Relevance) +
                   (0.12 * score.SocialFit) +
                   (0.12 * score.Specificity) +
                   (0.12 * score.InsightDepth) +
                   (0.10 * score.RelationshipFit) +
                   (0.12 * score.CTAResponseQuality) +
                   (0.10 * score.PositioningStrength) +
                   (0.05 * score.Tone) +
                   (0.03 * score.Brevity) +
                   (0.04 * score.TimingFit) -
                   (0.14 * score.HallucinationPenalty) -
                   (0.08 * score.GenericPraisePenalty) -
                   score.GenericPenalty -
                   (0.12 * score.ParticipationWithoutPositionPenalty) -
                   (0.08 * score.EngagementCost) -
                   score.CtaParticipationPenalty -
                   score.ChatStyleMismatchPenalty +
                   score.ChatNaturalnessBoost;
        }

        return (0.22 * score.Relevance) +
               (0.16 * score.SocialFit) +
               (0.16 * score.Specificity) +
               (0.16 * score.InsightDepth) +
               (0.10 * score.RelationshipFit) +
               (0.06 * score.QuestionQuality) +
               (0.06 * score.Tone) +
               (0.04 * score.Brevity) +
               (0.06 * score.TimingFit) -
               (0.18 * score.HallucinationPenalty) -
               (0.12 * score.GenericPraisePenalty) -
               score.GenericPenalty -
               (0.08 * score.EngagementCost) -
               score.CtaParticipationPenalty -
               score.ChatStyleMismatchPenalty +
               score.ChatNaturalnessBoost;
    }

    private static double CalculateQuestionQuality(SocialMoveCandidate candidate, MessageContext context)
    {
        var reply = (candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();
        if (!reply.Contains("?"))
            return 0.0;

        double score = 0.0;

        // Reward framing before question
        if (reply.Contains("\n\n") || reply.Split('?')[0].Split('.', StringSplitOptions.RemoveEmptyEntries).Length >= 1)
            score += 0.25;

        // Reward specific anchors
        var anchors = new[]
        {
            "client work", "live projects", "real-world", "ramp",
            "execution", "constraints", "trade-off", "graduates",
            "career-changers", "delivery", "mentorship"
        };

        var anchorHits = anchors.Count(a => reply.Contains(a));
        score += Math.Min(0.30, anchorHits * 0.08);

        // Reward sharp question shapes
        if (reply.Contains("what tends to") || reply.Contains("what pattern") || reply.Contains("what separates"))
            score += 0.20;

        // Penalize generic stems
        var genericStems = new[]
        {
            "what do you think",
            "can you share more",
            "would love to hear",
            "what has your experience been",
            "what's the biggest challenge"
        };

        var genericHits = genericStems.Count(g => reply.Contains(g));
        score -= Math.Min(0.35, genericHits * 0.15);

        // Penalize question-only replies on high-signal posts
        if (RequiresFraming(context) && IsBareQuestion(reply))
            score -= 0.30;

        return Clamp01(score);
    }

    private static bool IsBareQuestion(string reply)
    {
        var text = reply.Trim();
        if (!text.EndsWith("?"))
            return false;

        return !text.Contains(".") && !text.Contains("\n\n");
    }

    private static bool RequiresFraming(MessageContext context)
    {
        var situation = (context.SituationType ?? string.Empty).ToLowerInvariant();
        return situation == "educational" || situation == "opinion" || situation == "recruitment" || situation == "milestone" || situation == "opportunity";
    }
}

