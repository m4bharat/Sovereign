using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class SocialMovePlanner : ISocialMovePlanner
{
    public IReadOnlyList<SocialMoveCandidate> Plan(SocialSituation situation, RelationshipAnalysis relationshipAnalysis)
    {
        var lowerType = situation.Type.ToLowerInvariant();

        var familyMapping = new Dictionary<string, (string[] primary, string[] backup)>
        {
            // Exact task mappings
            ["achievement_share"] = (new[] { "praise" }, new[] { "congratulate" }),
            ["personal_update"] = (new[] { "congratulate" }, new[] { "congratulate_encourage" }),
            ["industry_news"] = (new[] { "add_insight" }, new[] { "ask_relevant_question" }),
            ["group_announcement"] = (new[] { "acknowledge" }, new[] { "appreciate" }),
            ["holiday_greeting"] = (new[] { "respond" }, new[] { "appreciate" }),
            ["relationship_preservation"] = (new[] { "engage" }, new[] { "appreciate" }),
            ["job_search"] = (new[] { "encourage" }, new[] { "congratulate" }),
            ["cta_engagement"] = (new[] { "answer_supportively" }, new[] { "add_specific_insight" }),
            ["defer_no_reply"] = (new[] { "no_reply" }, Array.Empty<string>()),
            ["controversial_no_reply"] = (new[] { "no_reply" }, Array.Empty<string>()),

            // Surface / mode preserved
            ["compose_post"] = (new[] { "draft_post" }, new[] { "rewrite_user_intent" }),
            ["rewrite_feed_reply"] = (new[] { "rewrite_user_intent" }, new[] { "light_touch", "add_specific_insight" }),
            ["rewrite_direct_message"] = (new[] { "rewrite_user_intent" }, new[] { "respond_helpfully" }),
            ["direct_message"] = (new[] { "respond_helpfully" }, new[] { "rewrite_user_intent", "acknowledge_and_continue" }),

            // Legacy remaps
            ["milestone"] = (new[] { "praise" }, new[] { "congratulate", "congratulate_encourage" }),
            ["hiring"] = (new[] { "acknowledge" }, new[] { "appreciate", "offer_support" }),
            ["educational"] = (new[] { "add_insight" }, new[] { "ask_relevant_question", "appreciate" }),
            ["opinion"] = (new[] { "add_nuance" }, new[] { "ask_relevant_question", "agree" }),
            ["cta_or_question"] = (new[] { "answer_supportively" }, new[] { "add_specific_insight", "ask_relevant_question" }),
            ["question"] = (new[] { "answer_supportively" }, new[] { "ask_relevant_question", "appreciate" }),
            ["greeting"] = (new[] { "respond" }, new[] { "appreciate" }),
            ["low_signal"] = (new[] { "no_reply" }, Array.Empty<string>()),
            ["celebratory"] = (new[] { "congratulate" }, new[] { "praise", "appreciate" }),
            ["reflection"] = (new[] { "engage" }, new[] { "light_touch", "appreciate" }),
            ["sensitive"] = (new[] { "no_reply" }, Array.Empty<string>()),
            ["achievement"] = (new[] { "praise" }, new[] { "congratulate", "appreciate" }),
            ["news"] = (new[] { "add_insight" }, new[] { "ask_relevant_question", "appreciate" }),
            ["update"] = (new[] { "acknowledge" }, new[] { "appreciate" }),
            ["controversial"] = (new[] { "no_reply" }, Array.Empty<string>()),

            // Fallback
            ["default"] = (new[] { "engage" }, new[] { "appreciate" })
        };

        if (!familyMapping.TryGetValue(lowerType, out var mapping))
        {
            mapping = familyMapping["default"];
        }

        var primary = mapping.primary
            .Select(move => new SocialMoveCandidate
            {
                Move = move,
                Rationale = $"Primary for {situation.Type}: {move}"
            });

        var backup = mapping.backup
            .Select(move => new SocialMoveCandidate
            {
                Move = move,
                Rationale = $"Backup for {situation.Type}: {move}"
            });

        var moves = primary
            .Concat(backup)
            .GroupBy(m => m.Move, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToArray();

        // Optional relationship adjustment, but never override explicit no-reply flows
        var isExplicitNoReply =
            string.Equals(lowerType, "defer_no_reply", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lowerType, "controversial_no_reply", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lowerType, "low_signal", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lowerType, "sensitive", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lowerType, "controversial", StringComparison.OrdinalIgnoreCase);

        if (!isExplicitNoReply &&
            relationshipAnalysis.PowerDifferential > 0.7 &&
            !moves.Any(m => string.Equals(m.Move, "defer", StringComparison.OrdinalIgnoreCase)))
        {
            moves = moves
                .Append(new SocialMoveCandidate
                {
                    Move = "defer",
                    Rationale = "High power differential adjustment."
                })
                .ToArray();
        }

        return moves;
    }
}