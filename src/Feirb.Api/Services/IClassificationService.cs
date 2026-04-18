using Feirb.Api.Data.Entities;

namespace Feirb.Api.Services;

public interface IClassificationService
{
    Task<ClassificationServiceResult> ClassifyAsync(CachedMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Like <see cref="ClassifyAsync"/> but also returns the prompt and the raw LLM
    /// response for diagnostics/preview purposes. Used by the on-demand single-mail
    /// classification endpoint.
    /// </summary>
    Task<ClassificationDetailedResult> ClassifyDetailedAsync(CachedMessage message, CancellationToken cancellationToken = default);
}

public record ClassificationServiceResult(bool Success, string? Result, string? Error)
{
    public static ClassificationServiceResult Skipped { get; } = new(true, null, null) { IsSkipped = true };

    public bool IsSkipped { get; init; }
}

/// <summary>Outcome of a detailed classification including the LLM prompt and raw response.</summary>
public record ClassificationDetailedResult(
    bool Success,
    string? Result,
    string? Error,
    ClassificationPrompt? Prompt,
    string? RawResponse)
{
    public static ClassificationDetailedResult Skipped { get; } =
        new(true, null, null, null, null) { IsSkipped = true };

    public bool IsSkipped { get; init; }
}

/// <summary>Materialized system + user prompt sent to the LLM.</summary>
public record ClassificationPrompt(string System, string User);
