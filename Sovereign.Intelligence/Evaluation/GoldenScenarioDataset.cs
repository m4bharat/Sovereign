// GoldenScenarioDataset.cs
// This file defines the structure and implementation for the golden scenario dataset.

using System.Collections.Generic;

namespace Sovereign.Intelligence.Evaluation
{
    public class GoldenScenario
    {
        public string CaseName { get; set; }
        public string InputPayload { get; set; }
        public string ExpectedMoveFamily { get; set; }
        public List<string> ForbiddenBehaviors { get; set; }
        public List<string> AcceptableReplies { get; set; }
        public bool ShouldReply { get; set; }
    }

    public static class GoldenScenarioDataset
    {
        public static List<GoldenScenario> GetScenarios()
        {
            return new List<GoldenScenario>
            {
                new GoldenScenario
                {
                    CaseName = "Milestone Post",
                    InputPayload = "I just got promoted to Senior Engineer!",
                    ExpectedMoveFamily = "Congratulate",
                    ForbiddenBehaviors = new List<string> { "Ignore", "Criticize" },
                    AcceptableReplies = new List<string> { "Congratulations! Well deserved.", "Amazing news! Congrats!" },
                    ShouldReply = true
                },
                new GoldenScenario
                {
                    CaseName = "Hiring Post",
                    InputPayload = "We are hiring for a Software Engineer role at our company.",
                    ExpectedMoveFamily = "Amplify",
                    ForbiddenBehaviors = new List<string> { "Ignore", "Criticize" },
                    AcceptableReplies = new List<string> { "This looks like a great opportunity!", "Exciting role, sharing with my network." },
                    ShouldReply = true
                }
                // Add more scenarios here...
            };
        }
    }
}