using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

public interface ISocialSituationDetector
{
    SocialSituation Detect(MessageContext context);
}
