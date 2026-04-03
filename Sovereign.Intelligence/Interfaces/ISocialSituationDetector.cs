namespace Sovereign.Intelligence.Interfaces;

using Sovereign.Intelligence.Models;

public interface ISocialSituationDetector
{
    SocialSituation Detect(MessageContext context);
}
