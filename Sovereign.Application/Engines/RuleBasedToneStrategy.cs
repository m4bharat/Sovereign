
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Enums;
using Sovereign.Domain.ValueObjects;

namespace Sovereign.Application.Engines;

public sealed class RuleBasedToneStrategy : IToneAdjustmentStrategy
{
    public ToneVector Adjust(
        ToneVector baseVector,
        RelationshipRole role,
        StrategicGoal goal)
    {
        double warmth = 0;
        double assertiveness = 0;

        if (role == RelationshipRole.Investor)
            assertiveness += 0.2;

        if (goal == StrategicGoal.Negotiate)
            assertiveness += 0.3;

        if (goal == StrategicGoal.Reconnect)
            warmth += 0.4;

        return baseVector.Adjust(warmth, assertiveness, 0, 0, 0, 0);
    }
}
