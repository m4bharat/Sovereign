namespace Sovereign.API.Security;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "Sovereign";
    public string Audience { get; init; } = "Sovereign.Client";
    public string SecretKey { get; init; } = "replace-this-with-a-long-random-secret-key";
    public int ExpiryMinutes { get; init; } = 10080;
}
