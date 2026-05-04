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

/// <summary>
/// Why a classification was skipped. Lets callers distinguish "user has no rules
/// configured" (a normal state, surface help UI) from "backend is down" (an error,
/// surface failure UI). <see cref="NotApplicable"/> means the result is not a skip.
/// </summary>
public enum ClassificationSkipReason
{
    NotApplicable,
    NoConfiguration,
    BackendUnavailable,
}

/// <summary>Outcome of a detailed classification including the LLM prompt and raw response.</summary>
public record ClassificationDetailedResult(
    bool Success,
    string? Result,
    string? Error,
    ClassificationPrompt? Prompt,
    string? RawResponse)
{
    /// <summary>Skipped because the user has no classification rules or labels configured.</summary>
    public static ClassificationDetailedResult Skipped { get; } =
        new(true, null, null, null, null) { IsSkipped = true, SkipReason = ClassificationSkipReason.NoConfiguration };

    /// <summary>Skipped because the LLM backend (Ollama) is unavailable or timed out.</summary>
    public static ClassificationDetailedResult BackendUnavailable(string? error = null) =>
        new(true, null, error, null, null) { IsSkipped = true, SkipReason = ClassificationSkipReason.BackendUnavailable };

    public bool IsSkipped { get; init; }

    public ClassificationSkipReason SkipReason { get; init; } = ClassificationSkipReason.NotApplicable;
}

/// <summary>Materialized system + user prompt sent to the LLM.</summary>
public record ClassificationPrompt(string System, string User);
