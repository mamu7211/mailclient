using Feirb.Api.Data.Entities;

namespace Feirb.Api.Services;

public class NoopClassificationService : IClassificationService
{
    // The Result must be a JSON array of label names to match the contract
    // ClassificationService.ParseAndValidateResponse establishes and that the classify
    // endpoint persists into ClassificationResult.Result. "[]" means "classified, no
    // labels apply" — the safest no-op outcome.
    private const string _emptyLabels = "[]";

    public Task<ClassificationServiceResult> ClassifyAsync(CachedMessage message, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ClassificationServiceResult(Success: true, Result: _emptyLabels, Error: null));

    public Task<ClassificationDetailedResult> ClassifyDetailedAsync(CachedMessage message, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ClassificationDetailedResult(
            Success: true,
            Result: _emptyLabels,
            Error: null,
            Prompt: null,
            RawResponse: null));
}
