using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class SocialMovePlanner
{
    public IReadOnlyList<SocialMoveCandidate> Plan(SocialSituation situation, RelationshipAnalysis relationshipAnalysis)
    {
        var moves = situation.Type switch
        {
            "milestone" => new[]
            {
                new SocialMoveCandidate { Move = "congratulate", Rationale = "Milestone and public celebration." },
                new SocialMoveCandidate { Move = "congratulate_encourage", Rationale = "Milestone plus forward momentum." },
                new SocialMoveCandidate { Move = "appreciate_journey", Rationale = "Recognize journey, gratitude, and transition." }
            },
            "hiring" => new[]
            {
                new SocialMoveCandidate { Move = "express_interest", Rationale = "Show interest in the hiring opportunity." },
                new SocialMoveCandidate { Move = "amplify_signal", Rationale = "Help spread the word about the opportunity." },
                new SocialMoveCandidate { Move = "offer_support", Rationale = "Offer assistance or connections." }
            },
            "educational" => new[]
            {
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Acknowledge clarity and usefulness." },
                new SocialMoveCandidate { Move = "add_insight", Rationale = "Add signal to the post." },
                new SocialMoveCandidate { Move = "ask_relevant_question", Rationale = "Drive intelligent engagement." }
            },
            "opinion" => new[]
            {
                new SocialMoveCandidate { Move = "agree", Rationale = "Acknowledge the core stance." },
                new SocialMoveCandidate { Move = "add_nuance", Rationale = "Agree and add a useful layer." },
                new SocialMoveCandidate { Move = "ask_relevant_question", Rationale = "Open thoughtful discussion." }
            },
            "question" => new[]
            {
                new SocialMoveCandidate { Move = "answer_supportively", Rationale = "Respond to the question directly." },
                new SocialMoveCandidate { Move = "ask_relevant_question", Rationale = "Extend the discussion." },
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Acknowledge thoughtful framing." }
            },
            "personal_update" => new[]
            {
                new SocialMoveCandidate { Move = "acknowledge_update", Rationale = "Recognize the personal update." },
                new SocialMoveCandidate { Move = "express_interest", Rationale = "Show interest in the update." },
                new SocialMoveCandidate { Move = "encourage", Rationale = "Provide positive reinforcement." }
            },
            _ => new[]
            {
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Safe generic social move." },
                new SocialMoveCandidate { Move = "encourage", Rationale = "Light positive reinforcement." }
            }
        };

        // Adjust moves based on relationship analysis
        if (relationshipAnalysis.PowerDifferential > 0.7)
        {
            moves = moves.Append(new SocialMoveCandidate { Move = "defer", Rationale = "High power differential suggests deference." }).ToArray();
        }

        if (relationshipAnalysis.MomentumScore < 0.3)
        {
            moves = moves.Append(new SocialMoveCandidate { Move = "reconnect", Rationale = "Low momentum suggests a reconnect move." }).ToArray();
        }

        if (relationshipAnalysis.ReciprocityScore < 0.5)
        {
            moves = moves.Append(new SocialMoveCandidate { Move = "light_acknowledgment", Rationale = "Low reciprocity suggests a low-effort move." }).ToArray();
        }

        return moves;
    }
}
