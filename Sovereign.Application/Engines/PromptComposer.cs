using Sovereign.Domain.Enums;
using Sovereign.Domain.ValueObjects;

namespace Sovereign.Application.Engines;

public sealed class PromptComposer
{
    public string Compose(string userDraft, ToneVector tone)
    {
        return $"Rewrite the following message with calibrated tone. Warmth:{tone.Warmth}; Assertiveness:{tone.Assertiveness}; Formality:{tone.Formality}; Directness:{tone.Directness}; Deference:{tone.Deference}; EmotionalIntensity:{tone.EmotionalIntensity}. Message: {userDraft}";
    }

    public string ComposeRewritePrompt(
        string draft,
        string stance,
        RelationshipRole role,
        StrategicGoal goal,
        PlatformType platform,
        ToneVector tone)
    {
        return $@"
You are Sovereign, a strategic communication engine.

Rewrite the message for the following stance:
- Stance: {stance}
- Role: {role}
- Goal: {goal}
- Platform: {platform}

Tone targets:
- Warmth: {tone.Warmth}
- Assertiveness: {tone.Assertiveness}
- Formality: {tone.Formality}
- Directness: {tone.Directness}
- Deference: {tone.Deference}
- EmotionalIntensity: {tone.EmotionalIntensity}

Rules:
- Preserve the user's intent.
- Keep the message concise and human.
- Do not explain your reasoning.
- Return only the rewritten message.

Original message:
{draft}
";
    }

    public string BuildFallbackRewrite(
        string draft,
        string stance,
        RelationshipRole role,
        StrategicGoal goal)
    {
        var normalized = draft.Trim().TrimEnd('.');

        return stance switch
        {
            "HighStatus" => goal switch
            {
                StrategicGoal.ScheduleMeeting => $"Would love to exchange notes if useful. Happy to connect this week.",
                StrategicGoal.Negotiate => $"Happy to discuss this further if alignment makes sense on your side.",
                _ => $"Wanted to reconnect briefly and continue the conversation when useful."
            },
            "WarmStrategic" => goal switch
            {
                StrategicGoal.ScheduleMeeting => $"Would be great to catch up sometime this week if you're open to it.",
                StrategicGoal.Reconnect => $"Wanted to check in and reconnect — hope things have been going well on your side.",
                _ => $"Thought I’d reach out and continue the conversation if helpful."
            },
            _ => goal switch
            {
                StrategicGoal.ScheduleMeeting => $"Are you available this week for a quick conversation?",
                StrategicGoal.Reconnect => $"Wanted to reconnect. Let me know if you’re open to a quick chat.",
                _ => $"Let me know if it makes sense to continue this discussion."
            }
        };
    }
}
