using Microsoft.Extensions.Logging;
using Moq;
using Sovereign.Domain.DTOs;
using Sovereign.Domain.Models;
using Sovereign.Domain.Services;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.DecisionV2;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;
using Xunit;

namespace Sovereign.Tests.DecisionEngineRegressionTests;

public class DecisionV2RequiredReplyRegressionTests
{
    [Fact]
    public async Task DecideAsync_FeedReplyWithDraftAndSource_NeverReturnsNoReply()
    {
        var result = await CreateEngine().DecideAsync(CreateInput(
            surface: "feed_reply",
            message: "Congrats on this launch",
            sourceText: "We launched our workflow platform today after months of customer feedback.",
            allowNoReply: true));

        Assert.True(result.ShouldReply);
        Assert.NotEqual("no_reply", result.Move);
        Assert.False(string.IsNullOrWhiteSpace(result.Reply));
    }

    [Fact]
    public async Task DecideAsync_StartPostWithDraft_NeverReturnsNoReply()
    {
        var result = await CreateEngine().DecideAsync(CreateInput(
            surface: "start_post",
            message: "Write a LinkedIn post on AI evaluation and workflow ownership",
            sourceText: string.Empty,
            allowNoReply: true));

        Assert.True(result.ShouldReply);
        Assert.NotEqual("no_reply", result.Move);
        Assert.False(string.IsNullOrWhiteSpace(result.Reply));
    }

    [Fact]
    public async Task DecideAsync_MessagingChatWithAllowNoReplyFalse_NeverReturnsNoReply()
    {
        var result = await CreateEngine().DecideAsync(CreateInput(
            surface: "messaging_chat",
            message: "thank you really appreciate it",
            sourceText: "Thanks for making the intro yesterday.",
            allowNoReply: false));

        Assert.True(result.ShouldReply);
        Assert.NotEqual("no_reply", result.Move);
        Assert.Contains("appreciate", result.Reply, StringComparison.OrdinalIgnoreCase);
    }

    private static DecisionV2Input CreateInput(string surface, string message, string sourceText, bool allowNoReply)
    {
        return new DecisionV2Input
        {
            UserId = "user-001",
            ContactId = Guid.NewGuid().ToString("N"),
            Surface = surface,
            Platform = "linkedin",
            Message = message,
            SourceText = sourceText,
            ParentContextText = sourceText,
            CurrentUrl = "https://www.linkedin.com",
            RelationshipRole = "Peer",
            AllowNoReply = allowNoReply
        };
    }

    private static DecisionEngineV2 CreateEngine()
    {
        var mockRelationshipEngine = new Mock<IRelationshipIntelligenceEngine>();
        mockRelationshipEngine.Setup(engine => engine.Analyze(It.IsAny<RelationshipContext>()))
            .Returns(new SocialInsight { OpportunityScore = 0.5, RiskScore = 0.2 });

        var mockAssembler = new Mock<IConversationContextAssembler>();
        mockAssembler.Setup(assembler => assembler.AssembleAsync(It.IsAny<AssembleAiContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssembleAiContextRequest request, CancellationToken _) => new MessageContext
            {
                UserId = request.UserId,
                ContactId = request.ContactId,
                Message = request.Message,
                Surface = request.Surface,
                Platform = request.Platform,
                SourceText = request.SourceText,
                ParentContextText = request.ParentContextText,
                InteractionMode = request.Surface == "messaging_chat" ? "chat" : request.Surface == "start_post" ? "compose" : "reply",
                InteractionMetadata = request.InteractionMetadata
            });

        return new DecisionEngineV2(
            mockAssembler.Object,
            mockRelationshipEngine.Object,
            new SocialSituationDetector(),
            new SocialMovePlanner(),
            new CandidateReplyGenerator(),
            new CandidateScoringEngine(),
            new WinnerSelectionEngine(),
            new NoOpDecisionV2LlmClient(),
            new Mock<ILogger<DecisionEngineV2>>().Object);
    }

    private sealed class NoOpDecisionV2LlmClient : ILlmClient
    {
        public Task<string> CompleteAsync(string prompt, CancellationToken ct = default) => Task.FromResult("{}");

        public Task<DecisionV2Result> CompleteDecisionV2Async(string prompt, CancellationToken ct = default) =>
            Task.FromResult(new DecisionV2Result { Reply = string.Empty, Alternatives = [] });

        public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default) =>
            AsyncEnumerable.Empty<string>();
    }
}
