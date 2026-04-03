using System.Threading.Tasks;
using Xunit;
using Sovereign.Intelligence.Services;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Tests
{
    public class SocialMovePlannerTests
    {
        [Fact]
        public async Task PlanMove_ShouldReturnValidMove()
        {
            // Arrange
            var planner = new SocialMovePlanner();
            var context = new RelationshipContext
            {
                // Initialize with test data
            };

            // Act
            var move = await planner.PlanMoveAsync(context);

            // Assert
            Assert.NotNull(move);
            Assert.Equal("ExpectedMove", move.MoveType);
        }
    }
}