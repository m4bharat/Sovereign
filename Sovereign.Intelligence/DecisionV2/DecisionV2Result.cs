namespace Sovereign.Intelligence.DecisionV2;

public sealed class DecisionV2Result
{
    public string Strategy { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Reply { get; set; } = string.Empty;
}
