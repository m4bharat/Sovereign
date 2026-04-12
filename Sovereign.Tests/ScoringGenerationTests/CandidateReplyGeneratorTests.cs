using System.Linq;
using Xunit;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Services;
using Sovereign.Intelligence.Models;

namespace Sovereign.Tests.ScoringGenerationTests
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

        // Generate_ShouldSetRequiresPolishToTrue removed per user instruction - logic changed
    }
}