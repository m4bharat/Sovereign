using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    private readonly IAiSituationClassifier _aiSituationClassifier;
    private readonly IAiInsightExpansionService _aiInsightExpansionService;
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
        IAiSituationClassifier aiSituationClassifier,
        IAiInsightExpansionService aiInsightExpansionService,
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
        _aiSituationClassifier = aiSituationClassifier;
        _aiInsightExpansionService = aiInsightExpansionService;
        _socialMovePlanner = socialMovePlanner;
        _candidateReplyGenerator = candidateReplyGenerator;
        _candidateScoringEngine = candidateScoringEngine;
        _winnerSelectionEngine = winnerSelectionEngine;
        _llmClient = llmClient;
        _logger = logger;
    }

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
        : this(
            contextAssembler,
            relationshipIntelligenceEngine,
            socialSituationDetector,
            new NullAiSituationClassifier(),
            new NullAiInsightExpansionService(),
            socialMovePlanner,
            candidateReplyGenerator,
            candidateScoringEngine,
            winnerSelectionEngine,
            llmClient,
            logger)
    {
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

        if (ShouldUseAiClassifier(messageContext, situation))
        {
            var aiClassification = await _aiSituationClassifier.ClassifyAsync(
                messageContext,
                cancellationToken);

            if (aiClassification is not null &&
                aiClassification.Confidence >= 0.80)
            {
                situation = new SocialSituation
                {
                    Type = aiClassification.SituationType,
                    Confidence = aiClassification.Confidence,
                    Summary = aiClassification.Rationale
                };
            }
        }

        messageContext = ApplySituation(messageContext, situation);

        var moveCandidates = _socialMovePlanner.Plan(situation, relationshipAnalysis);
        var replyCandidates = _candidateReplyGenerator.Generate(moveCandidates, messageContext)
                             .Where(c => !string.IsNullOrWhiteSpace(c.Reply) || c.Move == "no_reply")
                             .ToList();

        var scoredCandidates = _candidateScoringEngine.Score(
            replyCandidates,
            situation,
            messageContext,
            relationshipAnalysis);

        var winnerSelection = _winnerSelectionEngine.SelectBest(scoredCandidates, situation, messageContext);
        var winner = winnerSelection.Winner;


        if (ShouldSkipReply(winner, input.AllowNoReply, messageContext))
        {
            return BuildDecisionResult(
             winner,
             winnerSelection.Alternatives,
             messageContext,
             situation,
             allowNoReply: true);
        }

        // If a no_reply candidate still wins for strong drafted surfaces,
        // fall back to the best non-no_reply candidate before polishing.
        if (string.Equals(winner.Move, "no_reply", StringComparison.OrdinalIgnoreCase))
        {
            var fallback = winnerSelection.Alternatives
                .FirstOrDefault(x => !string.Equals(x.Move, "no_reply", StringComparison.OrdinalIgnoreCase));

            if (fallback != null)
            {
              
                winner = fallback;
            }
        }

        var expandedInsight = await _aiInsightExpansionService.GenerateInsightCommentAsync(
            messageContext,
            winner,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(expandedInsight))
        {
            winner.Reply = expandedInsight;
        }

        var finalWinner = await TryGenerateFinalWithAi(
            winner,
            messageContext,
            situation,
            cancellationToken);


        return BuildDecisionResult(
                                finalWinner,
                                winnerSelection.Alternatives,
                                messageContext,
                                situation);
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

    private static MessageContext ApplySituation(MessageContext context, SocialSituation situation)
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
            RiskScore = context.RiskScore,
            OpportunityScore = context.OpportunityScore,
            DesiredTone = context.DesiredTone,
            SituationType = situation.Type
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

    private static bool ShouldUseAiClassifier(MessageContext context, SocialSituation situation)
    {
        var surface = context.Surface?.ToLowerInvariant() ?? string.Empty;
        var message = context.Message?.Trim().ToLowerInvariant() ?? string.Empty;

        if (surface is "feed_reply" or "messaging_chat" or "start_post")
            return true;

        if (message is "reply" or "make a comment" or "write comment" or "suggest reply")
            return true;

        if (situation.Type is "general" or "rewrite_feed_reply" or "rewrite_direct_message")
            return true;

        return false;
    }

    private static bool ShouldSkipReply(
        SocialMoveCandidate winner,
        bool allowNoReply,
        MessageContext context)
    {
        if (!allowNoReply)
            return false;

        var hasUserDraft = !string.IsNullOrWhiteSpace(context.Message);
        var isFeedReply = string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase);
        var hasSource = !string.IsNullOrWhiteSpace(context.SourceText);
        var isCompose = string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase);

        if (isFeedReply && hasUserDraft && hasSource)
            return false;

        if (isCompose && hasUserDraft)
            return false;

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

    private async Task<SocialMoveCandidate> TryGenerateFinalWithAi(
        SocialMoveCandidate winner,
        MessageContext context,
        SocialSituation situation,
        CancellationToken cancellationToken)
    {
        if (string.Equals(winner.Move, "no_reply", StringComparison.OrdinalIgnoreCase))
            return winner;

        try
        {
            var prompt = BuildAiFirstPrompt(winner, context, situation);
            var result = await _llmClient.CompleteDecisionV2Async(prompt, cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.Reply))
            {
                var aiReply = result.Reply.Trim();

                if (ContainsUnsupportedNumber(aiReply, context))
                {
                    _logger.LogWarning("AI reply contained unsupported numeric claim. Falling back.");
                    return winner;
                }

                winner.Reply = aiReply;
                winner.GenerationConfidence = Math.Max(winner.GenerationConfidence, result.Confidence);
                winner.Alternatives = result.Alternatives ?? winner.Alternatives;
            }

            return winner;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI-first generation failed. Falling back to deterministic candidate.");
            return winner;
        }
    }

    private static string BuildAiFirstPrompt(
        SocialMoveCandidate winner,
        MessageContext context,
        SocialSituation situation)
    {
        var surface = context.Surface ?? string.Empty;
        var mode = context.InteractionMode ?? string.Empty;
        var message = context.Message ?? string.Empty;
        var sourceAuthor = context.SourceAuthor ?? string.Empty;
        var sourceTitle = context.SourceTitle ?? string.Empty;
        var sourceText = context.SourceText ?? string.Empty;
        var parent = context.ParentContextText ?? string.Empty;
        var nearby = context.NearbyContextText ?? string.Empty;

        return $$"""
You are Sovereign, a social intelligence assistant for LinkedIn.

Return ONLY valid JSON matching:
{
  "move": "{{winner.Move}}",
  "reply": "final user-facing text",
  "confidence": 0.0,
  "alternatives": []
}

Context:
- Surface: {{surface}}
- Interaction mode: {{mode}}
- Situation type: {{situation.Type}}
- Selected move: {{winner.Move}}
- User instruction/message: {{message}}
- Source author: {{sourceAuthor}}
- Source title: {{sourceTitle}}
- Source text: {{sourceText}}
- Parent context: {{parent}}
- Nearby context: {{nearby}}

Rules:
1. If Surface is start_post or SituationType is compose_post:
   - Write a complete LinkedIn post.
   - Do NOT rewrite the instruction.
   - Do NOT say "the point around LinkedIn".
   - Use paragraphs.
   - Include a strong hook, body, and closing thought.
   - Hashtags are allowed.

2. If Surface is feed_reply:
   - Write a concise LinkedIn comment.
   - Do NOT invent facts, statistics, studies, or numbers.
   - Use only the provided source text/title/author.
   - If the user message is just "reply", treat it as a command, not content to rewrite.

3. If Surface is messaging_chat:
   - Write a natural chat response.
   - If the user message is "reply", respond to the latest message in SourceText/NearbyContextText.
   - Do NOT include "Especially around LinkedIn".
   - Keep it human and direct.

4. Never hallucinate names.
5. Never use the wrong person name.
6. Never mention hidden strategy or analysis.
7. Avoid generic phrases like "great post", "well said", "thanks for sharing".
8. Preserve the selected move family.
""";
    }

    private static bool ContainsUnsupportedNumber(string reply, MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceText ?? string.Empty,
            context.SourceTitle ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty);

        var numbers = Regex.Matches(reply ?? string.Empty, @"\b\d+(\.\d+)?%?\b")
            .Select(m => m.Value)
            .Distinct()
            .ToArray();

        if (numbers.Length == 0)
            return false;

        return numbers.Any(n => !source.Contains(n, StringComparison.OrdinalIgnoreCase));
    }

    private static DecisionV2Result BuildDecisionResult(
    SocialMoveCandidate winner,
    IReadOnlyList<SocialMoveCandidate> alternatives,
    MessageContext context,
    SocialSituation situation,
    bool allowNoReply = false)
    {
        var isNoReplyMove = string.Equals(winner.Move, "no_reply", StringComparison.OrdinalIgnoreCase);
        var suppressReply = allowNoReply && isNoReplyMove;

        return new DecisionV2Result
        {
            Move = winner.Move,
            Rationale = winner.Rationale,
            ShouldReply = !isNoReplyMove && !suppressReply,
            Reply = (!isNoReplyMove && !suppressReply) ? winner.Reply : string.Empty,
            Confidence = winner.GenerationConfidence,
            Alternatives = alternatives
                .Where(a => !string.IsNullOrWhiteSpace(a.Reply))
                .Select(a => a.Reply)
                .ToList(),

            RelationshipEffect = winner.RelationshipEffect,
            RiskScore = winner.RiskScore,
            OpportunityScore = winner.OpportunityScore,
            SituationType = situation.Type,
            Tone = context.DesiredTone ?? string.Empty
        };
    }

    private sealed class NullAiSituationClassifier : IAiSituationClassifier
    {
        public Task<AiSituationClassification?> ClassifyAsync(
            MessageContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult<AiSituationClassification?>(null);
    }

    private sealed class NullAiInsightExpansionService : IAiInsightExpansionService
    {
        public Task<string?> GenerateInsightCommentAsync(
            MessageContext context,
            SocialMoveCandidate candidate,
            CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);
    }
}
