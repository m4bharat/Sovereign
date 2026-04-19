using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class CandidateScoringEngine : ICandidateScoringEngine
{
    public IReadOnlyList<CandidateScore> Score(
        IReadOnlyList<SocialMoveCandidate> candidates,
        SocialSituation situation,
        MessageContext context,
        RelationshipAnalysis relationshipAnalysis)
    {
        return candidates.Select(c =>
        {
            var score = new CandidateScore
            {
                Candidate = c,
                Relevance = ScoreRelevance(context, c.Reply),
                Specificity = ScoreSpecificity(context, c.Reply),
                GenericPenalty = IsGeneric(c.Reply) ? 0.5 : 0.0
            };

            score.ComputedTotal =
                (0.5 * score.Relevance) +
                (0.4 * score.Specificity) -
                score.GenericPenalty;

            return score;
        }).ToList();
    }

    private double ScoreRelevance(MessageContext context, string reply)
    {
        if (string.IsNullOrWhiteSpace(reply) || string.IsNullOrWhiteSpace(context.SourceText))
            return 0;

        return context.SourceText.Split(' ')
            .Count(w => reply.Contains(w, StringComparison.OrdinalIgnoreCase)) / 10.0;
    }

    private double ScoreSpecificity(MessageContext context, string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0;

        return reply.Length > 50 ? 1 : 0.5;
    }

    private bool IsGeneric(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return true;

        var generic = new[] { "great post", "well said", "nice" };
        return generic.Any(g => reply.ToLower().Contains(g));
    }
}
