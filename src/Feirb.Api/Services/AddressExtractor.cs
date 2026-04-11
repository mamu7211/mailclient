using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Shared.Mail;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Feirb.Api.Services;

public interface IAddressExtractor
{
    Task CaptureAsync(
        FeirbDbContext db,
        Guid userId,
        IEnumerable<InternetAddressList> addressLists,
        bool markAsKnown,
        CancellationToken cancellationToken = default);
}

public class AddressExtractor : IAddressExtractor
{
    public async Task CaptureAsync(
        FeirbDbContext db,
        Guid userId,
        IEnumerable<InternetAddressList> addressLists,
        bool markAsKnown,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(addressLists);

        var parsed = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var list in addressLists)
        {
            if (list is null)
                continue;
            foreach (var addr in list.Mailboxes)
            {
                if (string.IsNullOrWhiteSpace(addr.Address))
                    continue;
                var normalized = EmailNormalizer.Normalize(addr.Address);
                if (normalized.Length == 0)
                    continue;
                parsed.TryAdd(normalized, string.IsNullOrWhiteSpace(addr.Name) ? addr.Address : addr.Name);
            }
        }

        if (parsed.Count == 0)
            return;

        var keys = parsed.Keys.ToList();
        var existing = await db.Addresses
            .Where(a => a.UserId == userId && keys.Contains(a.NormalizedEmail))
            .ToDictionaryAsync(a => a.NormalizedEmail, cancellationToken);

        // Include pending adds from the change tracker so we don't insert duplicates
        // when CaptureAsync is called multiple times in the same SaveChanges batch.
        foreach (var local in db.Addresses.Local)
        {
            if (local.UserId == userId && !existing.ContainsKey(local.NormalizedEmail))
                existing[local.NormalizedEmail] = local;
        }

        var now = DateTime.UtcNow;

        foreach (var (normalized, displayName) in parsed)
        {
            if (existing.TryGetValue(normalized, out var row))
            {
                row.LastSeenAt = now;
                row.SeenCount++;
                if (markAsKnown && row.IsUnknown)
                    row.IsUnknown = false;
            }
            else
            {
                db.Addresses.Add(new Address
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NormalizedEmail = normalized,
                    DisplayName = displayName,
                    IsUnknown = !markAsKnown,
                    IsBlocked = false,
                    FirstSeenAt = now,
                    LastSeenAt = now,
                    SeenCount = 1,
                });
            }
        }
    }
}
