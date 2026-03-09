namespace Sovereign.Intelligence.Configuration;

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434/api";
    public string Model { get; set; } = "qwen3-vl:4b";
    public string SystemPrompt { get; set; } = "You are a strict JSON decision engine.";
    public bool Stream { get; set; } = false;
}