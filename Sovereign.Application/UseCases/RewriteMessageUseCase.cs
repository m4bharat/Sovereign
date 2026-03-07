using Sovereign.Application.DTOs;
using Sovereign.Application.Services;

namespace Sovereign.Application.UseCases;

public sealed class RewriteMessageUseCase
{
    private readonly ToneCalibrationService _toneCalibrationService;

    public RewriteMessageUseCase(ToneCalibrationService toneCalibrationService)
    {
        _toneCalibrationService = toneCalibrationService;
    }

    public Task<RewriteMessageResponse> ExecuteAsync(RewriteMessageRequest request, CancellationToken ct = default)
        => _toneCalibrationService.RewriteAsync(request, ct);
}
