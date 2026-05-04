namespace Feirb.Shared.Mail;

/// <summary>
/// Response for the on-demand single-mail classify endpoint
/// (<c>POST /api/mail/messages/{id}/classify</c>).
/// </summary>
/// <param name="Success">
/// True when the classifier produced a result (even an empty list of labels).
/// False when an error occurred (e.g. AI service unavailable, malformed LLM response).
/// </param>
/// <param name="Labels">
/// Recognized label names. Empty list means no labels apply or classifier was skipped
/// (no rules/labels configured).
/// </param>
/// <param name="Applied">
/// True when the classify action was committed to the database (only possible with
/// <c>dryRun=false</c>). A classification marker is persisted regardless of matches,
/// so <c>Applied</c> may be true even if <see cref="Labels"/> is empty — callers
/// should not assume a label change occurred.
/// </param>
/// <param name="Error">Error message when <see cref="Success"/> is false; otherwise null.</param>
/// <param name="Prompt">
/// Materialized prompt sent to the LLM. Populated for <c>dryRun=true</c> only — null in apply mode.
/// </param>
/// <param name="RawResponse">
/// Verbatim text returned by the LLM, before parsing. Populated for <c>dryRun=true</c> only.
/// </param>
public record ClassifyMessageResponse(
    bool Success,
    IReadOnlyList<string> Labels,
    bool Applied,
    string? Error,
    ClassifyPrompt? Prompt,
    string? RawResponse);

/// <summary>System + user prompt sent to the LLM.</summary>
public record ClassifyPrompt(string System, string User);
