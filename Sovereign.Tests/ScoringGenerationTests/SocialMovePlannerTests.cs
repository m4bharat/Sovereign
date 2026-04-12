using Xunit;
using Sovereign.Intelligence.Services;
using Sovereign.Intelligence.Models;

namespace Sovereign.Tests.ScoringGenerationTests
{
    public class SocialMovePlannerTests
    {
        [Fact]
        public void Plan_ShouldReturnMovesForMilestoneSituation()
        {
            // Arrange
            var planner = new SocialMovePlanner();
            var situation = new SocialSituation { Type = "milestone" };
            var analysis = new RelationshipAnalysis { ReciprocityScore = 0.5 };

            // Act
            var moves = planner.Plan(situation, analysis);

            // Assert
            Assert.Contains(moves, m => m.Move == "congratulate");
            Assert.Contains(moves, m => m.Move == "congratulate_encourage");
        }

        [Fact]
        public void Plan_ShouldAddDeferMove_WhenHighPowerDifferential()
        {
            // Arrange
            var planner = new SocialMovePlanner();
            var situation = new SocialSituation { Type = "general" };
            var analysis = new RelationshipAnalysis { PowerDifferential = 0.8 };

            // Act
            var moves = planner.Plan(situation, analysis);

            // Assert
            Assert.Contains(moves, m => m.Move == "defer");
        }

        [Fact]
        public void Plan_ShouldIncludeChatSpecificMoves_ForDirectMessageSituation()
        {
            var planner = new SocialMovePlanner();
            var situation = new SocialSituation { Type = "direct_message" };
            var analysis = new RelationshipAnalysis { ReciprocityScore = 0.8 };

            var moves = planner.Plan(situation, analysis);

            Assert.Contains(moves, m => m.Move == "respond_helpfully");
            Assert.Contains(moves, m => m.Move == "acknowledge_and_continue");
            Assert.Contains(moves, m => m.Move == "rewrite_user_intent");
        }
    }
}