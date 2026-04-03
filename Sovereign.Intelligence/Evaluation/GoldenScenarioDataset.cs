// GoldenScenarioDataset.cs
// This file defines the structure and implementation for the golden scenario dataset.

using System.Collections.Generic;
using Sovereign.Intelligence.DecisionV2;

namespace Sovereign.Intelligence.Evaluation
{
    public sealed class GoldenScenario
    {
        public required string CaseName { get; init; }
        public required DecisionV2Input InputPayload { get; init; }
        public required string ExpectedMoveFamily { get; init; }
        public required List<string> ForbiddenBehaviors { get; init; }
        public required List<string> AcceptableReplies { get; init; }
        public required bool ShouldReply { get; init; }
        public required int MaxReplyLength { get; init; }
    }

    public sealed class GoldenScenarioDataset
    {
        public static IEnumerable<GoldenScenario> GetAllScenarios()
        {
            yield return new GoldenScenario
            {
                CaseName = "Promotion Milestone - Warm Tie",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "contact1",
                    Message = "I just got promoted to Senior Engineer!",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "John Doe",
                    SourceText = "Excited to announce my promotion to Senior Engineer at TechCorp!",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 7,
                    TotalInteractions = 15,
                    ReciprocityScore = 0.8,
                    MomentumScore = 0.7,
                    PowerDifferential = 0.1,
                    EmotionalTemperature = 0.9,
                    RecentRelationshipSummary = "Regular professional interactions, positive momentum",
                    RelevantMemories = new List<string> { "congratulated on previous achievement", "discussed career goals" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "congratulate",
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_praise" },
                AcceptableReplies = new List<string> { "Congrats", "Congratulations", "promotion", "achievement" },
                ShouldReply = true,
                MaxReplyLength = 200
            };

            yield return new GoldenScenario
            {
                CaseName = "Promotion Milestone - High Power Differential",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "boss1",
                    Message = "I got promoted",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Big Boss",
                    SourceText = "Proud to share my promotion to VP level.",
                    RelationshipRole = "Superior",
                    LastInteractionDays = 2,
                    TotalInteractions = 50,
                    ReciprocityScore = 0.6,
                    MomentumScore = 0.8,
                    PowerDifferential = 0.8,
                    EmotionalTemperature = 0.8,
                    RecentRelationshipSummary = "Professional relationship with high power differential",
                    RelevantMemories = new List<string> { "reported to this person", "received guidance" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "defer",
                ForbiddenBehaviors = new List<string> { "congratulate_encourage", "challenge" },
                AcceptableReplies = new List<string> { "Congratulations", "well-deserved", "leadership" },
                ShouldReply = true,
                MaxReplyLength = 150
            };

            yield return new GoldenScenario
            {
                CaseName = "Opinion Post - Add Insight",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "contact2",
                    Message = "What's your take on AI in healthcare?",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Dr. Smith",
                    SourceText = "The integration of AI in healthcare presents both opportunities and challenges. What are your thoughts?",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 30,
                    TotalInteractions = 5,
                    ReciprocityScore = 0.4,
                    MomentumScore = 0.3,
                    PowerDifferential = 0.2,
                    EmotionalTemperature = 0.6,
                    RecentRelationshipSummary = "Occasional interactions, professional respect",
                    RelevantMemories = new List<string> { "discussed technology trends" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "add_insight",
                ForbiddenBehaviors = new List<string> { "no_reply", "empty_praise" },
                AcceptableReplies = new List<string> { "AI", "healthcare", "opportunity", "challenge", "perspective" },
                ShouldReply = true,
                MaxReplyLength = 300
            };

            yield return new GoldenScenario
            {
                CaseName = "Question Post - Answer",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "contact3",
                    Message = "How do you handle work-life balance?",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Sarah Johnson",
                    SourceText = "Struggling with work-life balance as a manager. Any advice?",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 14,
                    TotalInteractions = 8,
                    ReciprocityScore = 0.7,
                    MomentumScore = 0.5,
                    PowerDifferential = 0.0,
                    EmotionalTemperature = 0.4,
                    RecentRelationshipSummary = "Friendly professional relationship",
                    RelevantMemories = new List<string> { "shared similar experiences" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "answer",
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_advice" },
                AcceptableReplies = new List<string> { "balance", "work", "life", "advice", "experience" },
                ShouldReply = true,
                MaxReplyLength = 250
            };

            yield return new GoldenScenario
            {
                CaseName = "Low Signal Post - No Reply",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "contact4",
                    Message = "Good morning everyone",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Generic User",
                    SourceText = "Good morning! Have a great day.",
                    RelationshipRole = "WeakTie",
                    LastInteractionDays = 365,
                    TotalInteractions = 1,
                    ReciprocityScore = 0.1,
                    MomentumScore = 0.1,
                    PowerDifferential = 0.0,
                    EmotionalTemperature = 0.2,
                    RecentRelationshipSummary = "Minimal interaction history",
                    RelevantMemories = new List<string>(),
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "no_reply",
                ForbiddenBehaviors = new List<string> { "praise", "engage" },
                AcceptableReplies = new List<string>(),
                ShouldReply = false,
                MaxReplyLength = 0
            };

            yield return new GoldenScenario
            {
                CaseName = "Weak Tie Update - Light Touch",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "contact5",
                    Message = "Just finished a marathon",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Acquaintance",
                    SourceText = "Completed my first marathon today! 26.2 miles of pure determination.",
                    RelationshipRole = "WeakTie",
                    LastInteractionDays = 180,
                    TotalInteractions = 2,
                    ReciprocityScore = 0.2,
                    MomentumScore = 0.3,
                    PowerDifferential = 0.1,
                    EmotionalTemperature = 0.7,
                    RecentRelationshipSummary = "Rare interactions, positive when they occur",
                    RelevantMemories = new List<string>(),
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "light_touch",
                ForbiddenBehaviors = new List<string> { "no_reply", "congratulate_encourage" },
                AcceptableReplies = new List<string> { "impressive", "congratulations", "marathon" },
                ShouldReply = true,
                MaxReplyLength = 100
            };

            yield return new GoldenScenario
            {
                CaseName = "Warm Relationship - Direct Engagement",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "contact6",
                    Message = "Thinking about switching careers",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Close Colleague",
                    SourceText = "Considering a career change after 10 years in current role. Any thoughts?",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 3,
                    TotalInteractions = 25,
                    ReciprocityScore = 0.9,
                    MomentumScore = 0.8,
                    PowerDifferential = 0.0,
                    EmotionalTemperature = 0.5,
                    RecentRelationshipSummary = "Strong professional relationship with personal elements",
                    RelevantMemories = new List<string> { "discussed career aspirations", "shared personal challenges" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "engage",
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_advice" },
                AcceptableReplies = new List<string> { "career", "change", "experience", "support", "thoughts" },
                ShouldReply = true,
                MaxReplyLength = 400
            };
        }
    }
}