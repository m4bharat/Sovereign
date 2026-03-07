namespace Sovereign.Domain.ValueObjects;

public sealed class ToneVector
{
    public ToneVector(
        double warmth,
        double assertiveness,
        double formality,
        double directness,
        double deference,
        double emotionalIntensity)
    {
        Warmth = warmth;
        Assertiveness = assertiveness;
        Formality = formality;
        Directness = directness;
        Deference = deference;
        EmotionalIntensity = emotionalIntensity;
    }

    public double Warmth { get; }
    public double Assertiveness { get; }
    public double Formality { get; }
    public double Directness { get; }
    public double Deference { get; }
    public double EmotionalIntensity { get; }

    public ToneVector Adjust(
        double warmthDelta,
        double assertivenessDelta,
        double formalityDelta,
        double directnessDelta,
        double deferenceDelta,
        double intensityDelta)
    {
        return new ToneVector(
            Warmth + warmthDelta,
            Assertiveness + assertivenessDelta,
            Formality + formalityDelta,
            Directness + directnessDelta,
            Deference + deferenceDelta,
            EmotionalIntensity + intensityDelta);
    }
}
