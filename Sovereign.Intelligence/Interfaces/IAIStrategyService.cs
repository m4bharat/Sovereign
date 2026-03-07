using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

public interface IAIStrategyService
{
    StrategyResult GenerateStrategy(RelationshipContext context);
}
