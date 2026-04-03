namespace Sovereign.Intelligence.Interfaces;

using Sovereign.Intelligence.Models;

public interface IRelationshipIntelligenceEngine
{
    SocialInsight Analyze(RelationshipContext context);
}
