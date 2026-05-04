using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Services;

/// <summary>
/// Shared logic for attaching label names to a <see cref="CachedMessage"/>.
/// Used by both <c>ClassificationJob</c> (batch path) and the on-demand classify endpoint.
/// Does not call <c>SaveChangesAsync</c> — callers are responsible for committing.
/// </summary>
internal static class MessageLabelApplier
{
    public static async Task ApplyAsync(
        FeirbDbContext db,
        CachedMessage message,
        IReadOnlyList<string> labelNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(labelNames);

        if (labelNames.Count == 0)
            return;

        var normalized = labelNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.ToLowerInvariant())
            .ToArray();

        if (normalized.Length == 0)
            return;

        var mailbox = message.Mailbox ?? await db.Mailboxes
            .AsNoTracking()
            .FirstAsync(m => m.Id == message.MailboxId, cancellationToken);

        var matchingLabels = await db.Labels
            .Where(l => l.UserId == mailbox.UserId && normalized.Contains(l.Name))
            .ToListAsync(cancellationToken);

        if (!db.Entry(message).Collection(m => m.Labels).IsLoaded)
        {
            await db.Entry(message).Collection(m => m.Labels).LoadAsync(cancellationToken);
        }

        foreach (var label in matchingLabels)
        {
            if (!message.Labels.Any(l => l.Id == label.Id))
            {
                message.Labels.Add(label);
            }
        }
    }
}
