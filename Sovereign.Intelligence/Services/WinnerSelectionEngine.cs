using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class WinnerSelectionEngine : IWinnerSelectionEngine
{
    public WinnerSelectionResult SelectBest(
        IReadOnlyList<CandidateScore> scores,
        SocialSituation situation,
        MessageContext context)
    {
        var winner = SelectWinnerScore(scores, situation, context);
        return new WinnerSelectionResult
        {
            Winner = winner.Candidate,
            Alternatives = BuildAlternatives(scores, winner, context)
        };
    }

    public WinnerSelectionResult SelectBest(
        IReadOnlyList<CandidateScore> scores,
        MessageContext context)
    {
        return SelectBest(scores, new SocialSituation { Type = context.SituationType ?? string.Empty }, context);
    }

    private static CandidateScore SelectWinnerScore(
        IReadOnlyList<CandidateScore> scores,
        SocialSituation situation,
        MessageContext context)
    {
        if (scores == null || scores.Count == 0)
            return BuildNoReplyFallback("No candidates were available.");

        var mustReply = MustReplyForSurface(context);
        var qualified = scores
            .Where(IsQualified)
            .Where(score => !HasEmptyRequiredReply(score))
            .OrderByDescending(score => score.ComputedTotal)
            .ToList();

        if (qualified.Count == 0)
            return BuildFallback(scores, mustReply);

        if (mustReply)
        {
            var bestNonNoReply = qualified
                .Where(score => !IsNoReply(score))
                .OrderByDescending(score => score.ComputedTotal)
                .FirstOrDefault();

            if (bestNonNoReply is not null && bestNonNoReply.ComputedTotal >= 0.45)
            {
                qualified = qualified.Where(score => !IsNoReply(score)).ToList();
            }
        }

        if (qualified.Count == 0)
            return BuildFallback(scores, mustReply);

        var top = qualified[0];
        if (qualified.Count == 1)
            return top;

        var runnerUp = qualified[1];
        var gap = Math.Abs(top.ComputedTotal - runnerUp.ComputedTotal);
        if (gap < 0.08 && !string.IsNullOrWhiteSpace(context.Message))
        {
            return CompareByDraftPreference(top, runnerUp) <= 0 ? top : runnerUp;
        }

        return top;
    }

    private static IReadOnlyList<SocialMoveCandidate> BuildAlternatives(
        IReadOnlyList<CandidateScore> scores,
        CandidateScore winner,
        MessageContext context)
    {
        return scores
            .Where(score => !ReferenceEquals(score, winner))
            .Where(IsQualified)
            .Where(score => !HasEmptyRequiredReply(score))
            .Where(score => !MustReplyForSurface(context) || !IsNoReply(score))
            .OrderByDescending(score => score.ComputedTotal)
            .Select(score => score.Candidate)
            .Take(3)
            .ToArray();
    }

    private static CandidateScore BuildFallback(IReadOnlyList<CandidateScore> scores, bool mustReply)
    {
        var bestNonNoReply = scores
            .Where(score => !IsNoReply(score))
            .Where(score => !HasEmptyRequiredReply(score))
            .OrderByDescending(score => score.ComputedTotal)
            .FirstOrDefault();

        if (bestNonNoReply is not null)
            return bestNonNoReply;

        if (!mustReply)
            return BuildNoReplyFallback("No candidate met the minimum threshold.");

        return new CandidateScore
        {
            Candidate = new SocialMoveCandidate
            {
                Move = "rewrite_user_intent",
                Reply = "Thanks, I appreciate it.",
                Rationale = "Safe rewrite fallback because the required-reply surface had no qualified winner."
            },
            ComputedTotal = 0.45
        };
    }

    private static CandidateScore BuildNoReplyFallback(string rationale)
    {
        return new CandidateScore
        {
            Candidate = new SocialMoveCandidate
            {
                Move = "no_reply",
                Reply = string.Empty,
                Rationale = rationale
            },
            ComputedTotal = 0.0
        };
    }

    private static bool IsQualified(CandidateScore score)
    {
        return score is not null &&
               string.IsNullOrWhiteSpace(score.DisqualifiedReason) &&
               score.ComputedTotal > 0.0 &&
               score.HallucinationPenalty < 0.45 &&
               score.GenericPenalty < 0.60;
    }

    private static bool HasEmptyRequiredReply(CandidateScore score)
    {
        return !IsNoReply(score) && string.IsNullOrWhiteSpace(score.Candidate.Reply);
    }

    private static bool IsNoReply(CandidateScore score)
    {
        return string.Equals(score.Candidate.Move, "no_reply", StringComparison.OrdinalIgnoreCase);
    }

    private static int CompareByDraftPreference(CandidateScore left, CandidateScore right)
    {
        return GetDraftPriority(left.Candidate.Move).CompareTo(GetDraftPriority(right.Candidate.Move));
    }

    private static int GetDraftPriority(string? move)
    {
        var normalized = (move ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "rewrite_user_intent" or "rewrite" or "polish" or "improve_draft" => 0,
            "helpful_reply" or "respond_helpfully" or "reply" or "answer_supportively" or "add_insight" or "add_specific_insight" or "acknowledge" or "appreciate" or "praise" or "congratulate" => 1,
            "draft_post" or "compose_post" or "create_post" => 2,
            "no_reply" => 3,
            _ => 2
        };
    }

    private static bool MustReplyForSurface(MessageContext context)
    {
        var surface = (context.Surface ?? string.Empty).Trim().ToLowerInvariant();
        var hasMessage = !string.IsNullOrWhiteSpace(context.Message);
        var hasSourceText = !string.IsNullOrWhiteSpace(context.SourceText);
        var allowNoReply = context.InteractionMetadata is not null &&
                           context.InteractionMetadata.TryGetValue("allow_no_reply", out var raw) &&
                           bool.TryParse(raw, out var parsed) &&
                           parsed;

        return surface switch
        {
            "feed_reply" => hasMessage && hasSourceText,
            "start_post" => hasMessage,
            "messaging_chat" => hasMessage && !allowNoReply,
            _ => false
        };
    }
}
