// AcceptanceMetrics.cs
// This file defines the structure for evaluating DecisionV2 against acceptance metrics.

namespace Sovereign.Intelligence.Evaluation
{
    public class AcceptanceMetrics
    {
        public double CorrectMoveFamilyPercentage { get; set; }
        public double HallucinationFreePercentage { get; set; }
        public double SociallyAcceptableReplyPercentage { get; set; }
        public double OverlongMetaResponsesPercentage { get; set; }
        public double NoReplyDecisionCorrectPercentage { get; set; }

        public bool MeetsMVPBar()
        {
            return CorrectMoveFamilyPercentage >= 85 &&
                   HallucinationFreePercentage >= 98 &&
                   SociallyAcceptableReplyPercentage >= 95 &&
                   OverlongMetaResponsesPercentage <= 5 &&
                   NoReplyDecisionCorrectPercentage >= 80;
        }
    }
}