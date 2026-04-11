using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class SocialMovePlanner : ISocialMovePlanner
{
    public IReadOnlyList<SocialMoveCandidate> Plan(SocialSituation situation, RelationshipAnalysis relationshipAnalysis)
    {
        if (string.Equals(situation.Type, "rewrite_direct_message", StringComparison.OrdinalIgnoreCase))
        {
            return new List<SocialMoveCandidate>
            {
                new SocialMoveCandidate { Move = "rewrite_user_intent", Rationale = "Rewrite rough user intent into a natural direct message." },
                new SocialMoveCandidate { Move = "respond_helpfully", Rationale = "Produce a concise, helpful DM if the intent is already mostly clear." },
                new SocialMoveCandidate { Move = "acknowledge_and_continue", Rationale = "Use when the best reply is short and conversational." },
                new SocialMoveCandidate { Move = "no_reply", Rationale = "Skip only if the user input is too incomplete to rewrite safely." }
            };
        }

        var moves = situation.Type switch
        {
            "rewrite_feed_reply" => new[]
            {
                new SocialMoveCandidate { Move = "rewrite_user_intent", Rationale = "Rewrite the rough comment into a specific feed reply." },
                new SocialMoveCandidate { Move = "light_touch", Rationale = "Keep it concise but more grounded in the post." },
                new SocialMoveCandidate{ Move = "add_specific_insight", Rationale = "Add a specific interpretation tied to the source text." },
                new SocialMoveCandidate { Move = "no_reply", Rationale = "Skip only if the draft is unusable." }
            },

            "compose_post" => new[]
            {
                new SocialMoveCandidate { Move = "draft_post", Rationale = "Draft a standalone LinkedIn post from the user's prompt." },
                new SocialMoveCandidate { Move = "rewrite_user_intent", Rationale = "Turn the rough post idea into a clean post draft." },
                new SocialMoveCandidate { Move = "outline_post", Rationale = "Create a structured post if the prompt is still broad." }
            },
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
            "cta_or_question" => new[]
            {
                new SocialMoveCandidate { Move = "answer_question", Rationale = "Directly answers the CTA/question." },
                new SocialMoveCandidate { Move = "add_specific_insight", Rationale = "Adds a concrete, useful point." },
                new SocialMoveCandidate { Move = "light_touch_question", Rationale = "Engages with a brief but relevant follow-up." },
                new SocialMoveCandidate { Move = "no_reply", Rationale = "Skip only if confidence or relevance is weak." }
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
            "celebratory" => new[]
            {
                new SocialMoveCandidate { Move = "defer", Rationale = "High-status achievement, defer to avoid presumption." },
                new SocialMoveCandidate { Move = "congratulate", Rationale = "Safe congratulation for company success." },
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Acknowledge the achievement." }
            },
            "reflection" => new[]
            {
                new SocialMoveCandidate { Move = "light_touch", Rationale = "Reconnect without overwhelming." },
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Acknowledge the reflection." },
                new SocialMoveCandidate { Move = "engage", Rationale = "Deepen the connection." }
            },
            "sensitive" => new[]
            {
                new SocialMoveCandidate { Move = "no_reply", Rationale = "Sensitive topic requires restraint." }
            },
            "achievement" => new[]
            {
                new SocialMoveCandidate { Move = "light_touch", Rationale = "Acknowledge without over-enthusiasm." },
                new SocialMoveCandidate { Move = "praise", Rationale = "Praise success in a measured way." },
                new SocialMoveCandidate { Move = "congratulate", Rationale = "Congratulate the achievement." },
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Appreciate the accomplishment." }
            },
            "news" => new[]
            {
                new SocialMoveCandidate { Move = "add_insight", Rationale = "Add perspective to industry news." },
                new SocialMoveCandidate { Move = "ask_relevant_question", Rationale = "Engage thoughtfully." },
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Acknowledge the information." }
            },
            "job_search" => new[]
            {
                new SocialMoveCandidate { Move = "encourage", Rationale = "Support the job search." },
                new SocialMoveCandidate { Move = "congratulate", Rationale = "Congratulate the initiative." },
                new SocialMoveCandidate { Move = "offer_support", Rationale = "Offer help or connections." }
            },
            "update" => new[]
            {
                new SocialMoveCandidate { Move = "acknowledge", Rationale = "Acknowledge the update." },
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Appreciate the communication." }
            },
            "greeting" => new[]
            {
                new SocialMoveCandidate { Move = "respond", Rationale = "Respond to the greeting." },
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Acknowledge the well-wishes." }
            },
            "direct_message" => new[]
            {
                new SocialMoveCandidate { Move = "respond_helpfully", Rationale = "Respond naturally and directly in chat." },
                new SocialMoveCandidate { Move = "acknowledge_and_continue", Rationale = "Acknowledge briefly and keep the conversation moving." },
                new SocialMoveCandidate { Move = "rewrite_user_intent", Rationale = "Rewrite rough user wording into a natural DM." },
                new SocialMoveCandidate { Move = "no_reply", Rationale = "Skip only if the user intent is empty or clearly not answerable." }
            },
            "low_signal" => new[]
            {
                new SocialMoveCandidate { Move = "no_reply", Rationale = "Generic greeting or low-signal message does not merit a reply." }
            },
            "controversial" => new[]
            {
                new SocialMoveCandidate { Move = "no_reply", Rationale = "Avoid engagement on potentially controversial content." }
            },
            _ => new[]
            {
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Safe generic social move." },
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
