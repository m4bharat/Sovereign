using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class SocialMovePlanner
{
    public IReadOnlyList<SocialMoveCandidate> Plan(SocialSituation situation)
    {
        return situation.Type switch
        {
            "milestone" => new[]
            {
                new SocialMoveCandidate { Move = "congratulate", Rationale = "Milestone and public celebration." },
                new SocialMoveCandidate { Move = "congratulate_encourage", Rationale = "Milestone plus forward momentum." },
                new SocialMoveCandidate { Move = "appreciate_journey", Rationale = "Recognize journey, gratitude, and transition." }
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
            _ => new[]
            {
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Safe generic social move." },
                new SocialMoveCandidate { Move = "encourage", Rationale = "Light positive reinforcement." }
            }
        };
    }
}
