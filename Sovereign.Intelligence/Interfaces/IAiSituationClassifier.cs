using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

public interface IAiSituationClassifier
{
    Task<AiSituationClassification?> ClassifyAsync(
        MessageContext context,
        CancellationToken cancellationToken);
}
