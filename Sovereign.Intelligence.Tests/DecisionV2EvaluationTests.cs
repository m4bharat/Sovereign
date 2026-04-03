// DecisionV2EvaluationTests.cs
// This file contains regression tests for DecisionV2 using the golden scenario dataset.

using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Sovereign.Intelligence.Evaluation;
using Sovereign.Intelligence.Services;

namespace Sovereign.Intelligence.Tests
{
    public class DecisionV2EvaluationTests
    {
        [Fact]
        public async Task DecisionV2_ShouldPassGoldenScenarios()
        {
            // Arrange
            var scenarios = GoldenScenarioDataset.GetScenarios();
            var decisionEngine = new DecisionEngineV2();

            foreach (var scenario in scenarios)
            {
                // Act
                var result = await decisionEngine.MakeDecisionAsync(scenario.InputPayload);

                // Assert
                Assert.Equal(scenario.ExpectedMoveFamily, result.MoveFamily);
                Assert.DoesNotContain(result.Behavior, scenario.ForbiddenBehaviors);

                if (scenario.ShouldReply)
                {
                    Assert.Contains(result.Reply, scenario.AcceptableReplies);
                }
                else
                {
                    Assert.Null(result.Reply);
                }
            }
        }
    }
}