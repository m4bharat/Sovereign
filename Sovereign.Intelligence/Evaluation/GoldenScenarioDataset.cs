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
        public required List<string> AllowedMoveSynonyms { get; init; }
        public required List<string> ForbiddenBehaviors { get; init; }
        public required List<string> AcceptableReplies { get; init; }
        public required List<string> ForbiddenReplyPatterns { get; init; }
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
                AllowedMoveSynonyms = new List<string> { "congratulate_encourage" },
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_praise" },
                AcceptableReplies = new List<string> { "Congrats", "Congratulations", "promotion", "achievement" },
                ForbiddenReplyPatterns = new List<string> { "great post", "well said", "nice work" },
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
                AllowedMoveSynonyms = new List<string> { "congratulate" },
                ForbiddenBehaviors = new List<string> { "congratulate_encourage", "challenge" },
                AcceptableReplies = new List<string> { "Congratulations", "well-deserved", "leadership" },
                ForbiddenReplyPatterns = new List<string> { "great job", "awesome", "fantastic" },
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
                AllowedMoveSynonyms = new List<string> { "answer_supportively", "ask_relevant_question" },
                ForbiddenBehaviors = new List<string> { "no_reply", "empty_praise" },
                AcceptableReplies = new List<string> { "AI", "healthcare", "opportunity", "challenge", "perspective" },
                ForbiddenReplyPatterns = new List<string> { "great post", "interesting", "nice" },
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
                AllowedMoveSynonyms = new List<string> { "answer_supportively" },
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_advice" },
                AcceptableReplies = new List<string> { "balance", "work", "life", "advice", "experience" },
                ForbiddenReplyPatterns = new List<string> { "great question", "good luck", "hang in there" },
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
                AllowedMoveSynonyms = new List<string>(),
                ForbiddenBehaviors = new List<string> { "praise", "engage" },
                AcceptableReplies = new List<string>(),
                ForbiddenReplyPatterns = new List<string>(),
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
                AllowedMoveSynonyms = new List<string> { "appreciate" },
                ForbiddenBehaviors = new List<string> { "no_reply", "congratulate_encourage" },
                AcceptableReplies = new List<string> { "impressive", "congratulations", "marathon" },
                ForbiddenReplyPatterns = new List<string> { "great job", "awesome work", "fantastic" },
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
                AllowedMoveSynonyms = new List<string> { "answer_supportively", "ask_relevant_question" },
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_advice" },
                AcceptableReplies = new List<string> { "career", "change", "experience", "support", "thoughts" },
                ForbiddenReplyPatterns = new List<string> { "good luck", "hang in there", "keep going" },
                ShouldReply = true,
                MaxReplyLength = 400
            };

            // Additional scenarios to expand to 25
            yield return new GoldenScenario
            {
                CaseName = "Recruiter Hiring Post - Express Interest",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "recruiter1",
                    Message = "We're hiring for a senior developer role",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Tech Recruiter",
                    SourceText = "Exciting opportunity! We're looking for a Senior .NET Developer to join our team. DM me if interested!",
                    RelationshipRole = "Professional",
                    LastInteractionDays = 60,
                    TotalInteractions = 3,
                    ReciprocityScore = 0.5,
                    MomentumScore = 0.4,
                    PowerDifferential = 0.3,
                    EmotionalTemperature = 0.6,
                    RecentRelationshipSummary = "Professional networking, occasional job discussions",
                    RelevantMemories = new List<string> { "shared resume", "discussed job opportunities" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "express_interest",
                AllowedMoveSynonyms = new List<string> { "ask_details" },
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_praise" },
                AcceptableReplies = new List<string> { "interested", "developer", "opportunity", "DM" },
                ForbiddenReplyPatterns = new List<string> { "great post", "good luck", "nice" },
                ShouldReply = true,
                MaxReplyLength = 150
            };

            yield return new GoldenScenario
            {
                CaseName = "Celebratory Post from High-Status Founder - Defer",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "founder1",
                    Message = "Congratulations on the funding round",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Famous Founder",
                    SourceText = "Thrilled to announce our Series A funding of $10M! Thanks to our amazing team.",
                    RelationshipRole = "Acquaintance",
                    LastInteractionDays = 90,
                    TotalInteractions = 1,
                    ReciprocityScore = 0.2,
                    MomentumScore = 0.1,
                    PowerDifferential = 0.9,
                    EmotionalTemperature = 0.8,
                    RecentRelationshipSummary = "Distant professional connection",
                    RelevantMemories = new List<string>(),
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "defer",
                AllowedMoveSynonyms = new List<string> { "congratulate" },
                ForbiddenBehaviors = new List<string> { "congratulate_encourage", "challenge" },
                AcceptableReplies = new List<string> { "congratulations", "funding", "success" },
                ForbiddenReplyPatterns = new List<string> { "great job", "awesome", "fantastic" },
                ShouldReply = true,
                MaxReplyLength = 100
            };

            yield return new GoldenScenario
            {
                CaseName = "Educational Thread - Add Insight",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "expert1",
                    Message = "Interesting perspective on machine learning",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "ML Expert",
                    SourceText = "The future of ML is in explainable AI. What do you think about current approaches?",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 45,
                    TotalInteractions = 4,
                    ReciprocityScore = 0.6,
                    MomentumScore = 0.5,
                    PowerDifferential = 0.1,
                    EmotionalTemperature = 0.7,
                    RecentRelationshipSummary = "Professional discussions on tech topics",
                    RelevantMemories = new List<string> { "discussed AI trends" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "add_insight",
                AllowedMoveSynonyms = new List<string> { "ask_relevant_question" },
                ForbiddenBehaviors = new List<string> { "no_reply", "empty_praise" },
                AcceptableReplies = new List<string> { "ML", "AI", "explainable", "perspective", "approach" },
                ForbiddenReplyPatterns = new List<string> { "great post", "interesting", "nice insight" },
                ShouldReply = true,
                MaxReplyLength = 350
            };

            yield return new GoldenScenario
            {
                CaseName = "Cold Reconnect - Light Touch",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "oldcontact1",
                    Message = "It's been a while, hope you're well",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Old Colleague",
                    SourceText = "Reflecting on my career journey. It's been 5 years since I left the company.",
                    RelationshipRole = "FormerColleague",
                    LastInteractionDays = 365 * 2,
                    TotalInteractions = 10,
                    ReciprocityScore = 0.3,
                    MomentumScore = 0.2,
                    PowerDifferential = 0.0,
                    EmotionalTemperature = 0.4,
                    RecentRelationshipSummary = "Past professional relationship, lapsed",
                    RelevantMemories = new List<string> { "worked together", "positive experiences" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "light_touch",
                AllowedMoveSynonyms = new List<string> { "appreciate" },
                ForbiddenBehaviors = new List<string> { "no_reply", "deep_engagement" },
                AcceptableReplies = new List<string> { "career", "journey", "reflection", "hope" },
                ForbiddenReplyPatterns = new List<string> { "great to hear", "good luck", "keep in touch" },
                ShouldReply = true,
                MaxReplyLength = 200
            };

            yield return new GoldenScenario
            {
                CaseName = "DM-Worthy Case - Direct Message",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "closecontact1",
                    Message = "Let's discuss this privately",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Close Contact",
                    SourceText = "Sharing some personal challenges I'm facing. Would love to hear your thoughts.",
                    RelationshipRole = "ClosePeer",
                    LastInteractionDays = 1,
                    TotalInteractions = 30,
                    ReciprocityScore = 0.95,
                    MomentumScore = 0.9,
                    PowerDifferential = 0.0,
                    EmotionalTemperature = 0.3,
                    RecentRelationshipSummary = "Strong personal and professional relationship",
                    RelevantMemories = new List<string> { "shared personal stories", "supported each other" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "direct_message",
                AllowedMoveSynonyms = new List<string> { "engage_privately" },
                ForbiddenBehaviors = new List<string> { "no_reply", "public_reply" },
                AcceptableReplies = new List<string> { "DM", "message", "privately", "discuss" },
                ForbiddenReplyPatterns = new List<string> { "hang in there", "good luck", "keep going" },
                ShouldReply = true,
                MaxReplyLength = 100
            };

            yield return new GoldenScenario
            {
                CaseName = "Explicit Reply Later Case - No Reply",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "busycontact1",
                    Message = "I'll reply later when I have time",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Busy Executive",
                    SourceText = "Quick update: We're launching a new product next month. More details soon.",
                    RelationshipRole = "Superior",
                    LastInteractionDays = 5,
                    TotalInteractions = 20,
                    ReciprocityScore = 0.7,
                    MomentumScore = 0.6,
                    PowerDifferential = 0.7,
                    EmotionalTemperature = 0.5,
                    RecentRelationshipSummary = "Professional relationship with busy schedule",
                    RelevantMemories = new List<string> { "discussed product launches", "respects time" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "no_reply",
                AllowedMoveSynonyms = new List<string>(),
                ForbiddenBehaviors = new List<string> { "premature_reply" },
                AcceptableReplies = new List<string>(),
                ForbiddenReplyPatterns = new List<string>(),
                ShouldReply = false,
                MaxReplyLength = 0
            };

            yield return new GoldenScenario
            {
                CaseName = "Negative Sensitive Post - Restraint",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "sensitivecontact1",
                    Message = "This is difficult to comment on",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Colleague",
                    SourceText = "Going through a tough time with health issues. Appreciate your thoughts but please be sensitive.",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 10,
                    TotalInteractions = 15,
                    ReciprocityScore = 0.8,
                    MomentumScore = 0.7,
                    PowerDifferential = 0.0,
                    EmotionalTemperature = 0.2,
                    RecentRelationshipSummary = "Supportive relationship, aware of sensitivities",
                    RelevantMemories = new List<string> { "supported during difficult times" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "no_reply",
                AllowedMoveSynonyms = new List<string>(),
                ForbiddenBehaviors = new List<string> { "insensitive_reply", "generic_support" },
                AcceptableReplies = new List<string>(),
                ForbiddenReplyPatterns = new List<string>(),
                ShouldReply = false,
                MaxReplyLength = 0
            };

            yield return new GoldenScenario
            {
                CaseName = "Weak-Tie Humblebrag - Light Touch",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "humblebrag1",
                    Message = "Impressive achievement",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Acquaintance",
                    SourceText = "Just got featured in Forbes 30 Under 30! Grateful for the journey.",
                    RelationshipRole = "WeakTie",
                    LastInteractionDays = 200,
                    TotalInteractions = 1,
                    ReciprocityScore = 0.1,
                    MomentumScore = 0.2,
                    PowerDifferential = 0.4,
                    EmotionalTemperature = 0.6,
                    RecentRelationshipSummary = "Minimal connection",
                    RelevantMemories = new List<string>(),
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "light_touch",
                AllowedMoveSynonyms = new List<string> { "congratulate" },
                ForbiddenBehaviors = new List<string> { "no_reply", "over_enthusiastic" },
                AcceptableReplies = new List<string> { "congratulations", "Forbes", "achievement" },
                ForbiddenReplyPatterns = new List<string> { "awesome", "fantastic", "amazing" },
                ShouldReply = true,
                MaxReplyLength = 80
            };

            yield return new GoldenScenario
            {
                CaseName = "Relationship Preservation - Engage",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "preserve1",
                    Message = "Let's keep the connection strong",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Valued Contact",
                    SourceText = "Reflecting on the past year and grateful for all the connections made.",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 30,
                    TotalInteractions = 12,
                    ReciprocityScore = 0.6,
                    MomentumScore = 0.5,
                    PowerDifferential = 0.1,
                    EmotionalTemperature = 0.5,
                    RecentRelationshipSummary = "Valuable professional relationship to maintain",
                    RelevantMemories = new List<string> { "collaborated on projects", "mutual respect" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "engage",
                AllowedMoveSynonyms = new List<string> { "appreciate" },
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_praise" },
                AcceptableReplies = new List<string> { "grateful", "connections", "year", "reflection" },
                ForbiddenReplyPatterns = new List<string> { "great post", "nice", "good to see" },
                ShouldReply = true,
                MaxReplyLength = 250
            };

            yield return new GoldenScenario
            {
                CaseName = "Low-Context Compose - Appreciate",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "lowcontext1",
                    Message = "Thanks for sharing",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Generic Contact",
                    SourceText = "Here's a quick update on our quarterly results.",
                    RelationshipRole = "WeakTie",
                    LastInteractionDays = 120,
                    TotalInteractions = 3,
                    ReciprocityScore = 0.3,
                    MomentumScore = 0.3,
                    PowerDifferential = 0.2,
                    EmotionalTemperature = 0.4,
                    RecentRelationshipSummary = "Occasional professional updates",
                    RelevantMemories = new List<string>(),
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "appreciate",
                AllowedMoveSynonyms = new List<string> { "light_touch" },
                ForbiddenBehaviors = new List<string> { "no_reply", "deep_engagement" },
                AcceptableReplies = new List<string> { "thanks", "sharing", "update", "results" },
                ForbiddenReplyPatterns = new List<string> { "great job", "awesome", "fantastic" },
                ShouldReply = true,
                MaxReplyLength = 100
            };

            yield return new GoldenScenario
            {
                CaseName = "Personal Update - Support",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "personal1",
                    Message = "How are you doing?",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Friend",
                    SourceText = "Just got engaged! Can't wait to celebrate with everyone.",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 7,
                    TotalInteractions = 18,
                    ReciprocityScore = 0.8,
                    MomentumScore = 0.7,
                    PowerDifferential = 0.0,
                    EmotionalTemperature = 0.9,
                    RecentRelationshipSummary = "Friendly relationship with personal elements",
                    RelevantMemories = new List<string> { "celebrated milestones together" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "congratulate",
                AllowedMoveSynonyms = new List<string> { "congratulate_encourage" },
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_praise" },
                AcceptableReplies = new List<string> { "congratulations", "engaged", "celebrate", "excited" },
                ForbiddenReplyPatterns = new List<string> { "great news", "awesome", "fantastic" },
                ShouldReply = true,
                MaxReplyLength = 150
            };

            yield return new GoldenScenario
            {
                CaseName = "Industry News - Comment",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "news1",
                    Message = "Interesting development",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Industry Expert",
                    SourceText = "Breaking: Major merger announced in tech sector today.",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 20,
                    TotalInteractions = 6,
                    ReciprocityScore = 0.5,
                    MomentumScore = 0.4,
                    PowerDifferential = 0.2,
                    EmotionalTemperature = 0.6,
                    RecentRelationshipSummary = "Professional networking, shared interests",
                    RelevantMemories = new List<string> { "discussed industry trends" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "add_insight",
                AllowedMoveSynonyms = new List<string> { "ask_relevant_question" },
                ForbiddenBehaviors = new List<string> { "no_reply", "empty_comment" },
                AcceptableReplies = new List<string> { "merger", "tech", "sector", "development" },
                ForbiddenReplyPatterns = new List<string> { "interesting", "nice", "good to know" },
                ShouldReply = true,
                MaxReplyLength = 200
            };

            yield return new GoldenScenario
            {
                CaseName = "Job Search Post - Encourage",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "jobseeker1",
                    Message = "Best of luck",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Former Intern",
                    SourceText = "Excited to announce I'm starting my job search! Open to opportunities in software engineering.",
                    RelationshipRole = "Mentor",
                    LastInteractionDays = 60,
                    TotalInteractions = 8,
                    ReciprocityScore = 0.7,
                    MomentumScore = 0.6,
                    PowerDifferential = -0.3,
                    EmotionalTemperature = 0.7,
                    RecentRelationshipSummary = "Mentoring relationship, supportive",
                    RelevantMemories = new List<string> { "guided career development", "provided advice" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "encourage",
                AllowedMoveSynonyms = new List<string> { "congratulate" },
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_advice" },
                AcceptableReplies = new List<string> { "excited", "job", "search", "opportunities", "engineering" },
                ForbiddenReplyPatterns = new List<string> { "good luck", "hang in there", "keep going" },
                ShouldReply = true,
                MaxReplyLength = 180
            };

            yield return new GoldenScenario
            {
                CaseName = "Controversial Opinion - No Reply",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "controversial1",
                    Message = "This might spark debate",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Debate Lover",
                    SourceText = "I think remote work is overrated. Office collaboration is essential for innovation.",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 90,
                    TotalInteractions = 4,
                    ReciprocityScore = 0.3,
                    MomentumScore = 0.2,
                    PowerDifferential = 0.1,
                    EmotionalTemperature = 0.8,
                    RecentRelationshipSummary = "Occasional interactions, differing opinions",
                    RelevantMemories = new List<string> { "disagreed on work topics" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "no_reply",
                AllowedMoveSynonyms = new List<string>(),
                ForbiddenBehaviors = new List<string> { "engage_debate", "confrontational_reply" },
                AcceptableReplies = new List<string>(),
                ForbiddenReplyPatterns = new List<string>(),
                ShouldReply = false,
                MaxReplyLength = 0
            };

            yield return new GoldenScenario
            {
                CaseName = "Group Announcement - Acknowledge",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "group1",
                    Message = "Noted",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Team Lead",
                    SourceText = "Team update: We're implementing new agile processes starting next sprint.",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 14,
                    TotalInteractions = 22,
                    ReciprocityScore = 0.6,
                    MomentumScore = 0.5,
                    PowerDifferential = 0.2,
                    EmotionalTemperature = 0.4,
                    RecentRelationshipSummary = "Team collaboration, professional updates",
                    RelevantMemories = new List<string> { "worked on agile projects" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "acknowledge",
                AllowedMoveSynonyms = new List<string> { "appreciate" },
                ForbiddenBehaviors = new List<string> { "no_reply", "unnecessary_comment" },
                AcceptableReplies = new List<string> { "update", "agile", "processes", "sprint" },
                ForbiddenReplyPatterns = new List<string> { "great", "good to know", "interesting" },
                ShouldReply = true,
                MaxReplyLength = 120
            };

            yield return new GoldenScenario
            {
                CaseName = "Holiday Greeting - Respond",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "holiday1",
                    Message = "Happy holidays",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Colleague",
                    SourceText = "Wishing everyone a joyful holiday season and a prosperous new year!",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 30,
                    TotalInteractions = 10,
                    ReciprocityScore = 0.5,
                    MomentumScore = 0.4,
                    PowerDifferential = 0.0,
                    EmotionalTemperature = 0.6,
                    RecentRelationshipSummary = "Friendly professional relationship",
                    RelevantMemories = new List<string> { "exchanged holiday greetings" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "respond",
                AllowedMoveSynonyms = new List<string> { "appreciate" },
                ForbiddenBehaviors = new List<string> { "no_reply", "overly_personal" },
                AcceptableReplies = new List<string> { "happy", "holidays", "joyful", "prosperous", "year" },
                ForbiddenReplyPatterns = new List<string> { "same to you", "you too", "likewise" },
                ShouldReply = true,
                MaxReplyLength = 100
            };

            yield return new GoldenScenario
            {
                CaseName = "Achievement Share - Praise",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "achiever1",
                    Message = "Well done",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Team Member",
                    SourceText = "Proud to share that our project won the innovation award!",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 5,
                    TotalInteractions = 25,
                    ReciprocityScore = 0.8,
                    MomentumScore = 0.7,
                    PowerDifferential = 0.0,
                    EmotionalTemperature = 0.8,
                    RecentRelationshipSummary = "Collaborative team relationship",
                    RelevantMemories = new List<string> { "worked on the project together" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "praise",
                AllowedMoveSynonyms = new List<string> { "congratulate" },
                ForbiddenBehaviors = new List<string> { "no_reply", "generic_praise" },
                AcceptableReplies = new List<string> { "proud", "project", "award", "innovation" },
                ForbiddenReplyPatterns = new List<string> { "great job", "awesome", "fantastic" },
                ShouldReply = true,
                MaxReplyLength = 140
            };

            yield return new GoldenScenario
            {
                CaseName = "Educational Post - Framed Question",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "educator1",
                    Message = "Interesting perspective",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "Professor Smith",
                    SourceText = "Programs that bridge learning to production are crucial for graduates. The transition from theory to real client work is where most growth happens.",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 14,
                    TotalInteractions = 8,
                    ReciprocityScore = 0.6,
                    MomentumScore = 0.5,
                    PowerDifferential = 0.1,
                    EmotionalTemperature = 0.7,
                    RecentRelationshipSummary = "Professional respect, shared interests in education",
                    RelevantMemories = new List<string> { "discussed career development" },
                    AllowNoReply = true,
                    RequestAlternatives = true
                },
                ExpectedMoveFamily = "ask_relevant_question",
                AllowedMoveSynonyms = new List<string> { "add_insight" },
                ForbiddenBehaviors = new List<string> { "no_reply", "bare_question" },
                AcceptableReplies = new List<string> { "transition", "graduates", "client work", "hardest part", "real-world" },
                ForbiddenReplyPatterns = new List<string> { "what do you think", "can you share more", "what's the biggest challenge" },
                ShouldReply = true,
                MaxReplyLength = 250
            };

            yield return new GoldenScenario
            {
                CaseName = "Recruitment Post - Framed Question",
                InputPayload = new DecisionV2Input
                {
                    UserId = "user1",
                    ContactId = "recruiter1",
                    Message = "Good program",
                    Platform = "linkedin",
                    Surface = "post_compose",
                    SourceAuthor = "HR Director",
                    SourceText = "Our graduate program has placed 95% of participants in full-time roles within 3 months. Career changers also benefit greatly.",
                    RelationshipRole = "Peer",
                    LastInteractionDays = 21,
                    TotalInteractions = 12,
                    ReciprocityScore = 0.7,
                    MomentumScore = 0.6,
                    PowerDifferential = 0.2,
                    EmotionalTemperature = 0.8,
                    RecentRelationshipSummary = "Professional networking, career discussions",
                    RelevantMemories = new List<string> { "shared job search experiences" },
                    AllowNoReply = true,
                    RequestAlternatives = false
                },
                ExpectedMoveFamily = "ask_relevant_question",
                AllowedMoveSynonyms = new List<string> { "appreciate" },
                ForbiddenBehaviors = new List<string> { "no_reply", "bare_question" },
                AcceptableReplies = new List<string> { "programs", "graduates", "career changers", "ramp successfully", "hardest balance" },
                ForbiddenReplyPatterns = new List<string> { "what do you think", "can you share more", "what's the biggest challenge" },
                ShouldReply = true,
                MaxReplyLength = 280
            };
        }
    }
}