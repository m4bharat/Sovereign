using Xunit;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Services;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Tests
{
    public class SocialSituationDetectorTests
    {
        [Fact]
        public void Detect_ShouldReturnMilestone_ForMilestoneText()
        {
            // Arrange
            var detector = new SocialSituationDetector();
            var context = new MessageContext
            {
                SourceText = "I'm excited to share my promotion!",
                InteractionMode = "reply"
            };

            // Act
            var situation = detector.Detect(context);

            // Assert
            Assert.Equal("milestone", situation.Type);
            Assert.True(situation.Confidence > 0.8);
        }

        [Fact]
        public void Detect_ShouldReturnCtaOrQuestion_ForQuestionText()
        {
            // Arrange
            var detector = new SocialSituationDetector();
            var context = new MessageContext
            {
                SourceText = "What do you think about this approach?",
                InteractionMode = "reply"
            };

            // Act
            var situation = detector.Detect(context);

            // Assert
            Assert.Equal("cta_or_question", situation.Type);
            Assert.Contains("input", situation.Summary, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Detect_ShouldReturn_RewriteDirectMessage_ForRoughChatIntent()
        {
            var detector = new SocialSituationDetector();
            var context = new MessageContext
            {
                InteractionMode = "chat",
                Message = "wish him thank you",
                InteractionMetadata = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["rewrite_intent"] = "True"
                }
            };

            var situation = detector.Detect(context);

            Assert.Equal("rewrite_direct_message", situation.Type);
        }
    }
}