using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

public interface IRelationshipIntelligenceEngine
{
    SocialInsight Analyze(RelationshipContext context);
}
