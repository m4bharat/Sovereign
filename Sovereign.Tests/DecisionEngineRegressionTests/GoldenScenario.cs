using Sovereign.Domain.DTOs;
using Sovereign.Intelligence.DecisionV2;

namespace Sovereign.Tests.DecisionEngineRegressionTests;

public sealed class GoldenScenario
{
    public string Name { get; init; } = string.Empty;
    public bool ShouldReply { get; init; }
    public string ExpectedMoveFamily { get; init; } = string.Empty;
    public List<string> AcceptableReplies { get; init; } = [];
    public List<string> ForbiddenPhrases { get; init; } = [];
    public string Surface { get; init; } = string.Empty;
    public string SourceText { get; init; } = string.Empty;
    public string UserDraft { get; init; } = string.Empty;
    public string SourceAuthor { get; init; } = string.Empty;
    public string SourceTitle { get; init; } = string.Empty;
    public string ExpectedSituationType { get; init; } = string.Empty;
    public DecisionV2Input InputPayload { get; init; } = new();
}
