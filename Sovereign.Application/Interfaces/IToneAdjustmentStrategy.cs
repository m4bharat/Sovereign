
using Sovereign.Domain.Enums;
using Sovereign.Domain.ValueObjects;

namespace Sovereign.Application.Interfaces;

public interface IToneAdjustmentStrategy
{
    ToneVector Adjust(
        ToneVector baseVector,
        RelationshipRole role,
        StrategicGoal goal);
}
