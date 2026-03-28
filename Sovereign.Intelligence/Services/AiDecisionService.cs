using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Parsers;
using Sovereign.Intelligence.Prompts;

namespace Sovereign.Intelligence.Services;

public sealed class AiDecisionService : IAiDecisionService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","a","an","and","or","but","if","then","than","that","this","those","these","to","for","from","of","in","on","at","with",
        "is","are","was","were","be","been","being","as","by","it","its","into","about","your","you","their","they","them","we","our",
        "i","me","my","mine","his","her","hers","he","she","him","not","just","more","most","very","really","truly","new","role"
    };

    private readonly ILlmClient _llmClient;
    private readonly AiDecisionPromptBuilder _promptBuilder;
    private readonly AiDecisionJsonParser _parser;
    private readonly ILogger<AiDecisionService> _logger;

    private readonly SocialSituationDetector _situationDetector = new();
    private readonly SocialMovePlanner _movePlanner = new();
    private readonly CandidateReplyGenerator _candidateReplyGenerator = new();
    private readonly CandidateScoringEngine _candidateScoringEngine = new();
    private readonly WinnerSelectionEngine _winnerSelectionEngine = new();

    public AiDecisionService(
        ILlmClient llmClient,
        AiDecisionPromptBuilder promptBuilder,
        AiDecisionJsonParser parser,
        ILogger<AiDecisionService> logger)
    {
        _llmClient = llmClient;
        _promptBuilder = promptBuilder;
        _parser = parser;
        _logger = logger;
    }

    public async Task<AiDecision> DecideAsync(MessageContext context, CancellationToken ct = default)
    {
        var prompt = _promptBuilder.Build(context);

        try
        {
            var raw = await _llmClient.CompleteAsync(prompt, ct);
            var parsed = _parser.Parse(raw);

            if (ShouldAcceptLlmReply(context, parsed))
            {
                _logger.LogInformation(
                    "Accepted LLM reply for user {UserId} contact {ContactId} with action {Action} and confidence {Confidence}.",
                    context.UserId,
                    context.ContactId,
                    parsed.Action,
                    parsed.Confidence);

                return parsed;
            }

            _logger.LogInformation(
                "LLM output rejected for user {UserId} contact {ContactId}; using move-scoring rescue path.",
                context.UserId,
                context.ContactId);

            return BuildDecisionFromScoredCandidates(context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AI decision failed for user {UserId} contact {ContactId}; using move-scoring rescue path.",
                context.UserId,
                context.ContactId);

            return BuildDecisionFromScoredCandidates(context);
        }
    }

    private AiDecision BuildDecisionFromScoredCandidates(MessageContext context)
    {
        var situation = _situationDetector.Detect(context);
        var plannedMoves = _movePlanner.Plan(situation);
        var generatedCandidates = _candidateReplyGenerator.Generate(context, plannedMoves, situation);
        var scoredCandidates = _candidateScoringEngine.Score(context, situation, generatedCandidates);
        var winner = _winnerSelectionEngine.SelectBest(scoredCandidates);

        var winningScore = scoredCandidates
            .Where(x => ReferenceEquals(x.Candidate, winner) || x.Candidate.Reply == winner.Reply)
            .OrderByDescending(x => x.Total)
            .Select(x => x.Total)
            .FirstOrDefault();

        _logger.LogInformation(
            "Move scoring selected move {Move} for user {UserId} contact {ContactId} situation {Situation} with score {Score}.",
            winner.Move,
            context.UserId,
            context.ContactId,
            situation.Type,
            winningScore);

        return new AiDecision
        {
            Action = AiDecision.ReplyAction,
            Reply = winner.Reply,
            Confidence = winningScore > 0 ? winningScore : 0.66
        };
    }

    private bool ShouldAcceptLlmReply(MessageContext context, AiDecision candidate)
    {
        if (!string.Equals(candidate.Action, AiDecision.ReplyAction, StringComparison.OrdinalIgnoreCase))
        {
            return !string.Equals(context.InteractionMode, "reply", StringComparison.OrdinalIgnoreCase);
        }

        if (string.IsNullOrWhiteSpace(candidate.Reply))
        {
            return false;
        }

        if (!string.Equals(context.InteractionMode, "reply", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsMetaFeedback(candidate.Reply))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.SourceText))
        {
            return true;
        }

        return IsGroundedReply(context, candidate.Reply);
    }

    private bool IsGroundedReply(MessageContext context, string reply)
    {
        var source = string.Join(" ",
            context.SourceText ?? string.Empty,
            context.SourceAuthor ?? string.Empty,
            context.SourceTitle ?? string.Empty,
            context.Message ?? string.Empty);

        var sourceTokens = Tokenize(source);
        var replyTokens = Tokenize(reply);

        if (replyTokens.Count == 0)
        {
            return false;
        }

        var overlap = replyTokens.Count(token => sourceTokens.Contains(token));
        var overlapRatio = overlap / (double)replyTokens.Count;

        var suspiciousTerms = ExtractSuspiciousCapitalizedTerms(reply)
            .Where(term => !ContainsTerm(source, term))
            .ToArray();

        var mentionsAuthor = !string.IsNullOrWhiteSpace(context.SourceAuthor) &&
                             ContainsTerm(reply, context.SourceAuthor);

        if (suspiciousTerms.Length >= 2)
        {
            return false;
        }

        if (overlapRatio >= 0.18)
        {
            return true;
        }

        if (mentionsAuthor && overlapRatio >= 0.10)
        {
            return true;
        }

        return false;
    }

    private static bool IsMetaFeedback(string reply)
    {
        var patterns = new[]
        {
            "this is a strong start",
            "you could improve",
            "consider adding",
            "make it more engaging",
            "you should add",
            "try to include",
            "to improve this post",
            "for linkedin, could you",
            "this post would be stronger",
            "add a specific example"
        };

        return patterns.Any(pattern =>
            reply.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> Tokenize(string text)
    {
        return Regex.Matches(text.ToLowerInvariant(), "[a-z0-9]+")
            .Select(m => m.Value)
            .Where(token => token.Length > 2 && !StopWords.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExtractSuspiciousCapitalizedTerms(string text)
    {
        return Regex.Matches(text, "\b[A-Z][a-zA-Z]{2,}\b")
            .Select(m => m.Value)
            .Where(term => !StopWords.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsTerm(string haystack, string needle)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(needle))
        {
            return false;
        }

        return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }
}
