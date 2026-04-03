using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Prompts;
using Sovereign.Intelligence.Services;
using Sovereign.Intelligence.Engines;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.DecisionV2;

public sealed class DecisionEngineV2 : IDecisionEngineV2
{
    private readonly IRelationshipIntelligenceEngine _relationshipIntelligenceEngine;
    private readonly ISocialSituationDetector _socialSituationDetector;
    private readonly ISocialMovePlanner _socialMovePlanner;
    private readonly ICandidateReplyGenerator _candidateReplyGenerator;
    private readonly ICandidateScoringEngine _candidateScoringEngine;
    private readonly IWinnerSelectionEngine _winnerSelectionEngine;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<DecisionEngineV2> _logger;

    public DecisionEngineV2(
        IRelationshipIntelligenceEngine relationshipIntelligenceEngine,
        ISocialSituationDetector socialSituationDetector,
        ISocialMovePlanner socialMovePlanner,
        ICandidateReplyGenerator candidateReplyGenerator,
        ICandidateScoringEngine candidateScoringEngine,
        IWinnerSelectionEngine winnerSelectionEngine,
        ILlmClient llmClient,
        ILogger<DecisionEngineV2> logger)
    {
        _relationshipIntelligenceEngine = relationshipIntelligenceEngine;
        _socialSituationDetector = socialSituationDetector;
        _socialMovePlanner = socialMovePlanner;
        _candidateReplyGenerator = candidateReplyGenerator;
        _candidateScoringEngine = candidateScoringEngine;
        _winnerSelectionEngine = winnerSelectionEngine;
        _llmClient = llmClient;
        _logger = logger;
    }

    public async Task<DecisionV2Result> DecideAsync(
        DecisionV2Input input,
        CancellationToken cancellationToken = default)
    {
        var messageContext = BuildMessageContext(input);
        var relationshipContext = BuildRelationshipContext(input);

        // Analyze relationship context
        var relationshipInsight = _relationshipIntelligenceEngine.Analyze(relationshipContext);
        var relationshipAnalysis = new RelationshipAnalysis
        {
            ReciprocityScore = relationshipContext.ReciprocityScore,
            MomentumScore = relationshipContext.MomentumScore,
            PowerDifferential = relationshipContext.PowerDifferential,
            EmotionalTemperature = relationshipContext.EmotionalTemperature,
            OpportunityScore = relationshipInsight.OpportunityScore,
            RiskScore = relationshipInsight.RiskScore,
            ReplyUrgencyHint = relationshipContext.ReplyUrgencyHint
        };

        var situation = _socialSituationDetector.Detect(messageContext);
        var moveCandidates = _socialMovePlanner.Plan(situation, relationshipAnalysis);
        var replyCandidates = _candidateReplyGenerator.Generate(moveCandidates, messageContext);
        var scoredCandidates = _candidateScoringEngine.Score(replyCandidates, situation, messageContext, relationshipAnalysis);
        var winnerSelection = _winnerSelectionEngine.SelectBest(scoredCandidates);
        var winner = winnerSelection.Winner;

        if (ShouldSkipReply(winner))
        {
            return BuildDecisionResult(winner, winnerSelection.Alternatives, allowNoReply: true);
        }

        var polishedWinner = await TryPolishWinner(winner, messageContext, cancellationToken);
        return BuildDecisionResult(polishedWinner, winnerSelection.Alternatives);
    }

    private MessageContext BuildMessageContext(DecisionV2Input input)
    {
        // Map DecisionV2Input to MessageContext
        return new MessageContext
        {
            UserId = input.UserId,
            ContactId = input.ContactId,
            Message = input.Message,
            SourceText = input.SourceText,
            ParentContextText = input.ParentContextText,
            NearbyContextText = input.NearbyContextText,
            Platform = input.Platform,
            Surface = input.Surface,
            CurrentUrl = input.CurrentUrl,
            SourceAuthor = input.SourceAuthor,
            SourceTitle = input.SourceTitle,
            RelationshipRole = input.RelationshipRole,
            // Additional mappings - these may need to be added to DecisionV2Input
            LastInteractionDays = 0, // Placeholder
            TotalInteractions = 0, // Placeholder
            RecentRelationshipSummary = string.Empty, // Placeholder
            RelevantMemories = string.Join("; ", input.RelevantMemories)
        };
    }

    private RelationshipContext BuildRelationshipContext(DecisionV2Input input)
    {
        // Build relationship context from input
        return new RelationshipContext
        {
            UserId = input.UserId,
            ContactId = input.ContactId,
            ReciprocityScore = input.ReciprocityScore,
            MomentumScore = input.MomentumScore,
            PowerDifferential = input.PowerDifferential,
            EmotionalTemperature = input.EmotionalTemperature,
            // ...additional mappings...
        };
    }

    private async Task<SocialMoveCandidate> TryPolishWinner(
        SocialMoveCandidate winner,
        MessageContext context,
        CancellationToken cancellationToken)
    {
        if (!winner.RequiresPolish)
        {
            return winner;
        }

        var prompt = new DecisionV2PromptBuilder().Build(winner, context);
        var polishedResult = await _llmClient.CompleteDecisionV2Async(prompt, cancellationToken);
        winner.Reply = polishedResult.Reply;
        winner.GenerationConfidence = polishedResult.Confidence;
        winner.Alternatives = polishedResult.Alternatives;
        return winner;
    }

    private DecisionV2Result BuildDecisionResult(SocialMoveCandidate winner, IReadOnlyList<SocialMoveCandidate> alternatives, bool allowNoReply = false)
    {
        return new DecisionV2Result
        {
            Move = winner.Move,
            Rationale = winner.Rationale,
            ShouldReply = !allowNoReply || winner.Move != "no_reply",
            Reply = winner.Reply,
            Confidence = winner.GenerationConfidence,
            Alternatives = alternatives.Select(a => a.Reply).ToList(),
            RelationshipEffect = winner.RelationshipEffect,
            RiskScore = winner.RiskScore,
            OpportunityScore = winner.OpportunityScore,
            // ...additional fields...
        };
    }

    private bool ShouldSkipReply(SocialMoveCandidate winner)
    {
        return winner.Move == "no_reply" || winner.RiskScore > 0.8;
    }
}