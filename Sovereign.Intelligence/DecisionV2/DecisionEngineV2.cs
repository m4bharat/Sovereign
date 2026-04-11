using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sovereign.Domain.DTOs;
using Sovereign.Domain.Services;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Engines;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Prompts;

namespace Sovereign.Intelligence.DecisionV2;

public sealed class DecisionEngineV2 : IDecisionEngineV2
{
    private readonly IConversationContextAssembler _contextAssembler;
    private readonly IRelationshipIntelligenceEngine _relationshipIntelligenceEngine;
    private readonly ISocialSituationDetector _socialSituationDetector;
    private readonly ISocialMovePlanner _socialMovePlanner;
    private readonly ICandidateReplyGenerator _candidateReplyGenerator;
    private readonly ICandidateScoringEngine _candidateScoringEngine;
    private readonly IWinnerSelectionEngine _winnerSelectionEngine;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<DecisionEngineV2> _logger;

    public DecisionEngineV2(
        IConversationContextAssembler contextAssembler,
        IRelationshipIntelligenceEngine relationshipIntelligenceEngine,
        ISocialSituationDetector socialSituationDetector,
        ISocialMovePlanner socialMovePlanner,
        ICandidateReplyGenerator candidateReplyGenerator,
        ICandidateScoringEngine candidateScoringEngine,
        IWinnerSelectionEngine winnerSelectionEngine,
        ILlmClient llmClient,
        ILogger<DecisionEngineV2> logger)
    {
        _contextAssembler = contextAssembler;
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
        var messageContext = await _contextAssembler.AssembleAsync(
            new AssembleAiContextRequest
            {
                UserId = input.UserId,
                ContactId = input.ContactId,
                Message = input.Message,
                RelationshipRole = input.RelationshipRole,
                Platform = input.Platform,
                Surface = input.Surface,
                CurrentUrl = input.CurrentUrl,
                SourceAuthor = input.SourceAuthor,
                SourceTitle = input.SourceTitle,
                SourceText = input.SourceText,
                ParentContextText = input.ParentContextText,
                NearbyContextText = input.NearbyContextText,
                InteractionMetadata = BuildInteractionMetadata(input)
            },
            cancellationToken);

        var relationshipContext = BuildRelationshipContext(input);

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

        messageContext = MergeDerivedSignals(messageContext, relationshipAnalysis);

        var situation = _socialSituationDetector.Detect(messageContext);
        var moveCandidates = _socialMovePlanner.Plan(situation, relationshipAnalysis);
        var replyCandidates = _candidateReplyGenerator.Generate(moveCandidates, messageContext);
        var scoredCandidates = _candidateScoringEngine.Score(
            replyCandidates,
            situation,
            messageContext,
            relationshipAnalysis);

        var winnerSelection = _winnerSelectionEngine.SelectBest(scoredCandidates, messageContext);
        var winner = winnerSelection.Winner;

        if (ShouldSkipReply(winner, input.AllowNoReply))
        {
            return BuildDecisionResult(winner, winnerSelection.Alternatives, allowNoReply: true);
        }

        var polishedWinner = await TryPolishWinner(winner, messageContext, cancellationToken);
        return BuildDecisionResult(polishedWinner, winnerSelection.Alternatives);
    }

    private static Dictionary<string, string> BuildInteractionMetadata(DecisionV2Input input)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["surface"] = input.Surface ?? string.Empty,
            ["platform"] = input.Platform ?? string.Empty,
            ["allow_no_reply"] = input.AllowNoReply.ToString(),
            ["request_alternatives"] = input.RequestAlternatives.ToString()
        };
    }

    private static MessageContext MergeDerivedSignals(
        MessageContext context,
        RelationshipAnalysis analysis)
    {
        return new MessageContext
        {
            UserId = context.UserId,
            ContactId = context.ContactId,
            Message = context.Message,
            RelationshipRole = context.RelationshipRole,
            RecentSummary = context.RecentSummary,
            LastTopicSummary = context.LastTopicSummary,
            RelevantMemories = context.RelevantMemories,
            LastInteractionDays = context.LastInteractionDays,
            TotalInteractions = context.TotalInteractions,
            RecentRelationshipSummary = context.RecentRelationshipSummary,
            Platform = context.Platform,
            RecentMessages = context.RecentMessages,
            MemoryFacts = context.MemoryFacts,
            Surface = context.Surface,
            CurrentUrl = context.CurrentUrl,
            SourceAuthor = context.SourceAuthor,
            SourceTitle = context.SourceTitle,
            SourceText = context.SourceText,
            ParentContextText = context.ParentContextText,
            NearbyContextText = context.NearbyContextText,
            InteractionMode = context.InteractionMode,
            InteractionMetadata = context.InteractionMetadata,

            RiskScore = analysis.RiskScore,
            OpportunityScore = analysis.OpportunityScore,
            DesiredTone = InferDesiredTone(context, analysis),
            SituationType = context.SituationType
        };
    }

    private static string InferDesiredTone(MessageContext context, RelationshipAnalysis analysis)
    {
        if (context.InteractionMode == "chat") return "natural_direct";
        if (context.InteractionMode == "reply" && analysis.PowerDifferential > 0.6) return "respectful_specific";
        if (analysis.RiskScore > 0.6) return "warm_low_pressure";
        if (analysis.OpportunityScore > 0.6) return "engaged_specific";
        return "concise_human";
    }

    private static bool ShouldSkipReply(SocialMoveCandidate winner, bool allowNoReply)
    {
        if (!allowNoReply) return false;
        return winner.Move == "no_reply" || winner.RiskScore > 0.8;
    }

    private static RelationshipContext BuildRelationshipContext(DecisionV2Input input)
    {
        return new RelationshipContext
        {
            UserId = input.UserId,
            ContactId = input.ContactId,
            ReciprocityScore = input.ReciprocityScore,
            MomentumScore = input.MomentumScore,
            PowerDifferential = input.PowerDifferential,
            EmotionalTemperature = input.EmotionalTemperature,
            RecentRelationshipSummary = input.RecentRelationshipSummary,
            ReplyUrgencyHint = input.AllowNoReply ? 0.0 : 0.5
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

    private static DecisionV2Result BuildDecisionResult(
        SocialMoveCandidate winner,
        IReadOnlyList<SocialMoveCandidate> alternatives,
        bool allowNoReply = false)
    {
        var suppressReply = allowNoReply && winner.Move == "no_reply";

        return new DecisionV2Result
        {
            Move = winner.Move,
            Rationale = winner.Rationale,
            ShouldReply = !suppressReply,
            Reply = suppressReply ? string.Empty : winner.Reply,
            Confidence = winner.GenerationConfidence,
            Alternatives = alternatives.Select(a => a.Reply).ToList(),
            RelationshipEffect = winner.RelationshipEffect,
            RiskScore = winner.RiskScore,
            OpportunityScore = winner.OpportunityScore,
            SituationType = winner.SituationType,
            Tone = winner.Tone
        };
    }
}
