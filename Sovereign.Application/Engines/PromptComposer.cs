
using Sovereign.Domain.ValueObjects;

namespace Sovereign.Application.Engines;

public sealed class PromptComposer
{
    public string Compose(string userDraft, ToneVector tone)
    {
        return $"Rewrite the following message with calibrated tone:Warmth:{tone.Warmth}, Assertiveness:{tone.Assertiveness},Formality:{tone.Formality}, Directness:{tone.Directness},Deference:{tone.Deference}, EmotionalIntensity:{tone.EmotionalIntensity}Message:{userDraft}";
    }
}
