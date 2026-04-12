using System.Linq;
using Xunit;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Services;
using Sovereign.Intelligence.Models;

namespace Sovereign.API.IntegrationTests
{
    public class CandidateReplyGeneratorTests
    {
        [Fact]
        public void Generate_ShouldReturnCandidatesWithReplies()
        {
            // Arrange
            var generator = new CandidateReplyGenerator();
            var moveCandidates = new[]
            {
                new SocialMoveCandidate { Move = "congratulate", Rationale = "Test" },
                new SocialMoveCandidate { Move = "appreciate", Rationale = "Test" }
            };
            var context = new MessageContext
            {
                SourceAuthor = "John Doe",
                SourceText = "I got promoted!"
            };

            // Act
            var result = generator.Generate(moveCandidates, context);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.NotNull(c.Reply));
            Assert.Contains("Congratulations", result.First().Reply);
        }

        [Fact]
        public void Generate_ShouldSetRequiresPolishToTrue()
        {
            // Arrange
            var generator = new CandidateReplyGenerator();
            var moveCandidates = new[]
            {
                new SocialMoveCandidate { Move = "reply", Rationale = "Test" }
            };
            var context = new MessageContext();

            // Act
            var result = generator.Generate(moveCandidates, context);

            // Assert
            Assert.True(result.First().RequiresPolish);
        }
    }
}