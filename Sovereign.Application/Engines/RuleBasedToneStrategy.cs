using Sovereign.Application.Interfaces;
using Sovereign.Domain.Enums;
using Sovereign.Domain.ValueObjects;

namespace Sovereign.Application.Engines;

public sealed class RuleBasedToneStrategy : IToneAdjustmentStrategy
{
    public ToneVector Adjust(ToneVector baseVector, RelationshipRole role)
    {
        var warmth = 0d;
        var assertiveness = 0d;
        var deference = 0d;

        switch (role)
        {
            case RelationshipRole.Investor:
            case RelationshipRole.HiringManager:
                assertiveness += 0.2d;
                deference += 0.1d;
                break;
            case RelationshipRole.Friend:
                warmth += 0.3d;
                break;
        }

        return baseVector.Adjust(warmth, assertiveness, 0, 0, deference, 0);
    }
}
