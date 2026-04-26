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
using Sovereign.Intelligence.Services;

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
        _logger.LogInformation(
            "Deterministic situation detected: {SituationType} for surface {Surface}.",
            situation.Type,
            messageContext.Surface);

        if (ShouldUseAiClassifier(messageContext, situation))
        {
            var aiClassification = await _aiSituationClassifier.ClassifyAsync(
                messageContext,
                cancellationToken);

            if (aiClassification is not null &&
                aiClassification.Confidence >= 0.80)
            {
                _logger.LogInformation(
                    "AI situation override applied: {SituationType} (confidence {Confidence:F2}).",
                    aiClassification.SituationType,
                    aiClassification.Confidence);
                situation = new SocialSituation
                {
                    Type = aiClassification.SituationType,
                    Confidence = aiClassification.Confidence,
                    Summary = aiClassification.Rationale
                };
            }
            else if (aiClassification is not null)
            {
                _logger.LogInformation(
                    "AI situation classification ignored: {SituationType} (confidence {Confidence:F2}).",
                    aiClassification.SituationType,
                    aiClassification.Confidence);
            }
        }

        messageContext = ApplySituation(messageContext, situation);

        var moveCandidates = _socialMovePlanner.Plan(situation, relationshipAnalysis);
        var replyCandidates = _candidateReplyGenerator.Generate(moveCandidates, messageContext)
                             .Where(c => !string.IsNullOrWhiteSpace(c.Reply) || c.Move == "no_reply")
                             .ToList();

        if (MustReplyForSurface(messageContext))
        {
            replyCandidates = replyCandidates
                .Where(candidate => !string.Equals(candidate.Move, "no_reply", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (replyCandidates.Count == 0)
            {
                replyCandidates.Add(BuildRequiredSurfaceFallbackCandidate(messageContext));
            }
        }

        var scoredCandidates = _candidateScoringEngine.Score(
            replyCandidates,
            situation,
            messageContext,
            relationshipAnalysis);

        var winnerSelection = _winnerSelectionEngine.SelectBest(scoredCandidates, situation, messageContext)
                              ?? _winnerSelectionEngine.SelectBest(scoredCandidates, messageContext);

        if (winnerSelection?.Winner is null)
        {
            winnerSelection = new WinnerSelectionResult
            {
                Winner = scoredCandidates
                    .Select(score => score.Candidate)
                    .FirstOrDefault(candidate => candidate is not null)
                    ?? new SocialMoveCandidate
                    {
                        Move = "no_reply",
                        Reply = string.Empty,
                        Rationale = "No winner was returned."
                    },
                Alternatives = Array.Empty<SocialMoveCandidate>()
            };
        }

        var winner = winnerSelection.Winner;
        _logger.LogInformation(
            "Winner selected: move {Move} for situation {SituationType}.",
            winner.Move,
            situation.Type);


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
            _logger.LogInformation("AI insight expansion produced an improved comment for move {Move}.", winner.Move);
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

    private static bool MustReplyForSurface(MessageContext context)
    {
        var surface = (context.Surface ?? string.Empty).Trim().ToLowerInvariant();
        var hasMessage = !string.IsNullOrWhiteSpace(context.Message);
        var hasSourceText = !string.IsNullOrWhiteSpace(context.SourceText);
        var allowNoReply = TryGetAllowNoReply(context);

        return surface switch
        {
            "feed_reply" => hasMessage && hasSourceText,
            "start_post" => hasMessage,
            "messaging_chat" => hasMessage && !allowNoReply,
            _ => false
        };
    }

    private static bool TryGetAllowNoReply(MessageContext context)
    {
        return context.InteractionMetadata is not null &&
               context.InteractionMetadata.TryGetValue("allow_no_reply", out var raw) &&
               bool.TryParse(raw, out var parsed) &&
               parsed;
    }

    private static SocialMoveCandidate BuildRequiredSurfaceFallbackCandidate(MessageContext context)
    {
        var surface = (context.Surface ?? string.Empty).Trim().ToLowerInvariant();
        var reply = BuildRequiredSurfaceFallbackReply(context);
        var move = surface == "feed_reply" ? "helpful_reply" : "rewrite_user_intent";

        return new SocialMoveCandidate
        {
            Move = move,
            Reply = reply,
            ShortReply = reply,
            Rationale = "Safe required-surface fallback candidate.",
            GenerationConfidence = 0.55,
            RequiresPolish = true
        };
    }

    private static string BuildRequiredSurfaceFallbackReply(MessageContext context)
    {
        var surface = (context.Surface ?? string.Empty).Trim().ToLowerInvariant();
        var message = (context.Message ?? string.Empty).Trim();
        var sourceText = (context.SourceText ?? string.Empty).Trim();

        if (surface == "start_post")
        {
            return BuildSafeFallbackReply(
                new SocialMoveCandidate { Move = "rewrite_user_intent" },
                context,
                new SocialSituation { Type = "compose_post" });
        }

        if (surface == "messaging_chat")
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message.EndsWith(".") || message.EndsWith("!") || message.EndsWith("?")
                    ? char.ToUpperInvariant(message[0]) + message[1..]
                    : char.ToUpperInvariant(message[0]) + message[1..] + ".";
            }

            return BuildSafeChatFallback(context);
        }

        var anchor = ExtractFallbackAnchor(context);
        if (!string.IsNullOrWhiteSpace(anchor))
        {
            return $"This is a useful point on {anchor}. The part about {anchor} feels especially relevant.";
        }

        if (!string.IsNullOrWhiteSpace(sourceText))
        {
            return $"This is a useful point. The part about {sourceText.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "this"} feels especially relevant.";
        }

        return "This is a useful point. The practical implication feels especially relevant.";
    }

    private static string ExtractFallbackAnchor(MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceTitle ?? string.Empty,
            context.SourceText ?? string.Empty,
            context.SourceAuthor ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.Message ?? string.Empty);

        return Regex.Matches(source, @"[A-Za-z0-9][A-Za-z0-9'\-/+]{3,}")
            .Select(match => match.Value)
            .Where(token => !token.Equals("this", StringComparison.OrdinalIgnoreCase) &&
                            !token.Equals("that", StringComparison.OrdinalIgnoreCase) &&
                            !token.Equals("with", StringComparison.OrdinalIgnoreCase) &&
                            !token.Equals("your", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault() ?? string.Empty;
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

        var fallbackReply = winner.Reply ?? string.Empty;

        try
        {
            var prompt = BuildAiFirstPrompt(winner, context, situation);
            var result = await _llmClient.CompleteDecisionV2Async(prompt, cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.Reply))
            {
                var aiReply = result.Reply.Trim();

                if (!TryValidateFinalReply(aiReply, winner, context, situation, out var validationFailure))
                {
                    _logger.LogWarning(
                        "AI-generated reply failed validation: {ValidationFailure}. Falling back.",
                        validationFailure);
                    return RestoreOrFallbackWinner(winner, fallbackReply, context, situation, validationFailure);
                }

                winner.Reply = aiReply;
                winner.GenerationConfidence = Math.Max(winner.GenerationConfidence, result.Confidence);
                winner.Alternatives = result.Alternatives ?? winner.Alternatives;
                _logger.LogInformation("AI-first generation succeeded for move {Move}.", winner.Move);
            }
            else
            {
                _logger.LogInformation("AI-first generation returned empty content for move {Move}.", winner.Move);
            }

            if (TryValidateFinalReply(winner.Reply, winner, context, situation, out var deterministicValidationFailure))
                return winner;

            _logger.LogWarning(
                "Post-AI winner failed validation: {ValidationFailure}. Falling back.",
                deterministicValidationFailure);
            return RestoreOrFallbackWinner(winner, fallbackReply, context, situation, deterministicValidationFailure);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI-first generation failed. Falling back to deterministic candidate.");
            return RestoreOrFallbackWinner(winner, fallbackReply, context, situation, "ai generation failed");
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

    private SocialMoveCandidate RestoreOrFallbackWinner(
        SocialMoveCandidate winner,
        string fallbackReply,
        MessageContext context,
        SocialSituation situation,
        string reason)
    {
        if (TryValidateFinalReply(fallbackReply, winner, context, situation, out _))
        {
            winner.Reply = fallbackReply;
            _logger.LogWarning(
                "Using deterministic fallback reply after validation failure: {Reason}.",
                reason);
            return winner;
        }

        winner.Reply = BuildSafeFallbackReply(winner, context, situation);
        winner.GenerationConfidence = Math.Min(winner.GenerationConfidence, 0.55);
        _logger.LogWarning(
            "Using safe fallback reply after deterministic fallback also failed validation: {Reason}.",
            reason);
        return winner;
    }

    private static bool TryValidateFinalReply(
        string? reply,
        SocialMoveCandidate winner,
        MessageContext context,
        SocialSituation situation,
        out string failureReason)
    {
        var text = (reply ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            failureReason = "reply is empty";
            return false;
        }

        if (ContainsUnsupportedNumber(text, context))
        {
            failureReason = "reply contains unsupported numeric claims";
            return false;
        }

        if (ContainsForbiddenGenericPhrase(text))
        {
            failureReason = "reply contains banned generic phrasing";
            return false;
        }

        if (ContainsUnsupportedClaimMarkers(text, context))
        {
            failureReason = "feed reply introduces unsupported factual framing";
            return false;
        }

        if (ContainsWrongAuthorName(text, context))
        {
            failureReason = "reply appears to use the wrong author name";
            return false;
        }

        if (IsThoughtLeadershipMove(winner.Move) &&
            string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase) &&
            IsTooDerivative(text, context))
        {
            failureReason = "thought-leadership reply is too derivative";
            return false;
        }

        if (IsChatReplyInvalid(text, context))
        {
            failureReason = "chat reply includes command or public-comment artifacts";
            return false;
        }

        if (IsComposeReplyInvalid(text, context, situation))
        {
            failureReason = "compose output is too short or comment-like";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private static bool ContainsForbiddenGenericPhrase(string reply)
    {
        var banned = new[]
        {
            "great post",
            "well said",
            "thanks for sharing",
            "your experience underscores",
            "you nailed",
            "what stayed with me",
            "point around",
            "spot on"
        };

        return banned.Any(phrase => reply.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsUnsupportedClaimMarkers(string reply, MessageContext context)
    {
        if (!string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase))
            return false;

        var source = string.Join(" ",
            context.SourceText ?? string.Empty,
            context.SourceTitle ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty);

        var unsupportedMarkers = new[]
        {
            "studies show",
            "research shows",
            "research says",
            "on average",
            "according to data",
            "according to research",
            "statistically",
            "survey shows"
        };

        return unsupportedMarkers.Any(marker =>
            reply.Contains(marker, StringComparison.OrdinalIgnoreCase) &&
            !source.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsWrongAuthorName(string reply, MessageContext context)
    {
        var author = (context.SourceAuthor ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(author))
            return false;

        var authorTokens = Regex.Matches(author, @"[A-Za-z][A-Za-z'-]{1,}")
            .Select(m => m.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (authorTokens.Count == 0)
            return false;

        var leadingName = Regex.Match(reply, @"^(?<name>[A-Z][a-z]+),");
        if (leadingName.Success && !authorTokens.Contains(leadingName.Groups["name"].Value))
            return true;

        var directAddress = Regex.Match(
            reply,
            @"\b(?:congratulations|congrats|thank you|thanks|hi|hello)\s+(?<name>[A-Z][a-z]+)\b");

        return directAddress.Success && !authorTokens.Contains(directAddress.Groups["name"].Value);
    }

    private static bool IsThoughtLeadershipMove(string? move)
    {
        var value = (move ?? string.Empty).Trim().ToLowerInvariant();
        return value is "add_insight" or "add_specific_insight" or "add_nuance" or "engage";
    }

    private static bool IsTooDerivative(string reply, MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceText ?? string.Empty,
            context.SourceTitle ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty).ToLowerInvariant();

        var sourceTokens = Regex.Matches(source, @"[a-z0-9][a-z0-9'-]{2,}")
            .Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var replyTokens = Regex.Matches(reply.ToLowerInvariant(), @"[a-z0-9][a-z0-9'-]{2,}")
            .Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (replyTokens.Count == 0)
            return true;

        var overlap = replyTokens.Count(sourceTokens.Contains);
        return (double)overlap / replyTokens.Count > 0.55;
    }

    private static bool IsChatReplyInvalid(string reply, MessageContext context)
    {
        if (!string.Equals(context.InteractionMode, "chat", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(context.Surface, "messaging_chat", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (reply.Contains("linkedin", StringComparison.OrdinalIgnoreCase) ||
            reply.Contains("point around", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var trimmed = reply.TrimStart();
        return trimmed.StartsWith("reply", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("comment", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsComposeReplyInvalid(string reply, MessageContext context, SocialSituation situation)
    {
        if (!string.Equals(situation.Type, "compose_post", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (reply.Length < 120)
            return true;

        var lower = reply.ToLowerInvariant();
        if (lower.StartsWith("great post") ||
            lower.StartsWith("well said") ||
            lower.StartsWith("thanks for sharing"))
        {
            return true;
        }

        var sentenceCount = Regex.Matches(reply, @"[.!?]").Count;
        return sentenceCount < 2;
    }

    private static string BuildSafeFallbackReply(
        SocialMoveCandidate winner,
        MessageContext context,
        SocialSituation situation)
    {
        if (string.Equals(situation.Type, "compose_post", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase))
        {
            return "AI is no longer just a technology trend. It is becoming a business discipline that changes how teams operate, decide, and deliver.\n\nThe real shift is not model quality alone. It is the ability to apply AI inside real workflows with clear ownership, measurable outcomes, and feedback loops that improve over time.\n\nThe teams that win will be the ones that move beyond experimentation and turn AI into repeatable execution.\n\nHow are you thinking about AI adoption this year?\n\n#AI #ArtificialIntelligence #Leadership";
        }

        if (string.Equals(context.InteractionMode, "chat", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.Surface, "messaging_chat", StringComparison.OrdinalIgnoreCase))
        {
            return BuildSafeChatFallback(context);
        }

        if (IsThoughtLeadershipMove(winner.Move))
            return "The operational challenge usually appears after the first implementation - that is where portability, evaluation, and governance start to matter more than the initial model choice.";

        if (string.Equals(winner.Move, "congratulate", StringComparison.OrdinalIgnoreCase))
        {
            var author = (context.SourceAuthor ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(author))
                return $"Congratulations {author} - exciting milestone and a strong step forward. Wishing you continued growth in this new chapter.";

            return "Congratulations on the milestone - this is an exciting step forward, and I am wishing you continued growth in this new chapter.";
        }

        return "Appreciate the update here. The practical implication comes through clearly.";
    }

    private static string BuildSafeChatFallback(MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceText ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty);

        if (source.Contains("happy belated birthday", StringComparison.OrdinalIgnoreCase))
            return "Thank you so much! Really appreciate your wishes.";

        if (source.Contains("happy birthday", StringComparison.OrdinalIgnoreCase))
            return "Thank you so much! Really appreciate it.";

        return "Thanks so much - I really appreciate it.";
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
