using System;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sovereign.Domain.DTOs;
using Sovereign.Domain.Services;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.DecisionV2;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;
using Xunit;

namespace Sovereign.Intelligence.Tests;

public sealed class DecisionEngineV2ContextTests
{
    [Fact]
    public async Task DecideAsync_UsesAssemblerContext_InsteadOfHardcodedComposeMode()
    {
        var assembler = Substitute.For<IConversationContextAssembler>();
        var relationshipEngine = Substitute.For<IRelationshipIntelligenceEngine>();
        var situationDetector = Substitute.For<ISocialSituationDetector>();
        var movePlanner = Substitute.For<ISocialMovePlanner>();
        var candidateGenerator = Substitute.For<ICandidateReplyGenerator>();
        var scoring = Substitute.For<ICandidateScoringEngine>();
        var selector = Substitute.For<IWinnerSelectionEngine>();
        var llm = Substitute.For<ILlmClient>();

        assembler.AssembleAsync(Arg.Any<AssembleAiContextRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MessageContext
            {
                UserId = "u1",
                ContactId = "c1",
                Message = "nice",
                InteractionMode = "reply",
                SourceText = "We just shipped our v1",
                NearbyContextText = "comment 1\ncomment 2",
                Surface = "feed_reply",
                Platform = "linkedin"
            });

        relationshipEngine.Analyze(Arg.Any<RelationshipContext>())
            .Returns(new SocialInsight { RiskScore = 0.1, OpportunityScore = 0.7 });

        var situation = new SocialSituation { Type = "milestone_post" };
        situationDetector.Detect(Arg.Any<MessageContext>()).Returns(situation);

        var move = new SocialMoveCandidate
        {
            Move = "light_touch",
            Reply = "Congrats on shipping v1.",
            Rationale = "Specific acknowledgement",
            RequiresPolish = false,
            GenerationConfidence = 0.82
        };

        var candidateScore = new CandidateScore
        {
            Candidate = move,
            ComputedTotal = 0.8,
            Tone = 0.7,
            PositioningStrength = 0.6,
            InsightDepth = 0.5,
            Specificity = 0.4,
            CTAResponseQuality = 0.3,
            RiskAdjustedValue = 0.2,
            ParticipationWithoutPositionPenalty = 0.1,
            GenericPraisePenalty = 0.0,
            HallucinationPenalty = 0.0
        };

        movePlanner.Plan(Arg.Any<SocialSituation>(), Arg.Any<RelationshipAnalysis>())
            .Returns(new[] { move });

        candidateGenerator.Generate(Arg.Any<IReadOnlyList<SocialMoveCandidate>>(), Arg.Any<MessageContext>())
            .Returns(new[] { move });

        scoring.Score(
                Arg.Any<IReadOnlyList<SocialMoveCandidate>>(),
                Arg.Any<SocialSituation>(),
                Arg.Any<MessageContext>(),
                Arg.Any<RelationshipAnalysis>())
            .Returns(new[] { candidateScore });

        selector.SelectBest(Arg.Any<IReadOnlyList<CandidateScore>>(), Arg.Any<MessageContext>())
            .Returns(new WinnerSelectionResult { Winner = move, Alternatives = Array.Empty<SocialMoveCandidate>() });

        var sut = new DecisionEngineV2(
            assembler,
            relationshipEngine,
            situationDetector,
            movePlanner,
            candidateGenerator,
            scoring,
            selector,
            llm,
            NullLogger<DecisionEngineV2>.Instance);

        await sut.DecideAsync(new DecisionV2Input
        {
            UserId = "u1",
            ContactId = "c1",
            Message = "nice",
            SourceText = "We just shipped our v1",
            Surface = "feed_reply",
            Platform = "linkedin"
        });

        await assembler.Received(1).AssembleAsync(
            Arg.Is<AssembleAiContextRequest>(x =>
                x.UserId == "u1" &&
                x.ContactId == "c1" &&
                x.SourceText == "We just shipped our v1" &&
                x.Surface == "feed_reply"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DecideAsync_UsesChatMode_WhenSurfaceIsMessagingChat_EvenWithoutParentContext()
    {
        var assembler = Substitute.For<IConversationContextAssembler>();
        var relationshipEngine = Substitute.For<IRelationshipIntelligenceEngine>();
        var situationDetector = Substitute.For<ISocialSituationDetector>();
        var movePlanner = Substitute.For<ISocialMovePlanner>();
        var candidateGenerator = Substitute.For<ICandidateReplyGenerator>();
        var scoring = Substitute.For<ICandidateScoringEngine>();
        var selector = Substitute.For<IWinnerSelectionEngine>();
        var llm = Substitute.For<ILlmClient>();

        assembler.AssembleAsync(Arg.Any<AssembleAiContextRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MessageContext
            {
                UserId = "u1",
                ContactId = "c1",
                Message = "Wish him thank you",
                InteractionMode = "chat",
                Surface = "messaging_chat",
                Platform = "linkedin",
                SourceText = "",
                ParentContextText = "",
                NearbyContextText = ""
            });

        relationshipEngine.Analyze(Arg.Any<RelationshipContext>())
            .Returns(new SocialInsight { RiskScore = 0.1, OpportunityScore = 0.5 });

        var situation = new SocialSituation { Type = "direct_message" };
        situationDetector.Detect(Arg.Any<MessageContext>()).Returns(situation);

        var move = new SocialMoveCandidate
        {
            Move = "respond_helpfully",
            Reply = "Thanks so much — really appreciate it.",
            Rationale = "Natural DM gratitude rewrite",
            RequiresPolish = false,
            GenerationConfidence = 0.85
        };

        var candidateScore = new CandidateScore
        {
            Candidate = move,
            ComputedTotal = 0.85
        };

        movePlanner.Plan(Arg.Any<SocialSituation>(), Arg.Any<RelationshipAnalysis>())
            .Returns(new[] { move });

        candidateGenerator.Generate(Arg.Any<IReadOnlyList<SocialMoveCandidate>>(), Arg.Any<MessageContext>())
            .Returns(new[] { move });

        scoring.Score(
                Arg.Any<IReadOnlyList<SocialMoveCandidate>>(),
                Arg.Any<SocialSituation>(),
                Arg.Any<MessageContext>(),
                Arg.Any<RelationshipAnalysis>())
            .Returns(new[] { candidateScore });

        selector.SelectBest(Arg.Any<IReadOnlyList<CandidateScore>>(), Arg.Any<MessageContext>())
            .Returns(new WinnerSelectionResult { Winner = move, Alternatives = Array.Empty<SocialMoveCandidate>() });

        var sut = new DecisionEngineV2(
            assembler,
            relationshipEngine,
            situationDetector,
            movePlanner,
            candidateGenerator,
            scoring,
            selector,
            llm,
            NullLogger<DecisionEngineV2>.Instance);

        var result = await sut.DecideAsync(new DecisionV2Input
        {
            UserId = "u1",
            ContactId = "c1",
            Message = "Wish him thank you",
            Surface = "messaging_chat",
            Platform = "linkedin"
        });

        result.ShouldReply.Should().BeTrue();
        result.Reply.Should().NotBeNullOrWhiteSpace();
    }
}
