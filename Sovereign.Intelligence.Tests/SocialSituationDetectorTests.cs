using Xunit;
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
        public void Detect_ShouldReturnQuestion_ForQuestionText()
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
            Assert.Equal("question", situation.Type);
        }
    }
}