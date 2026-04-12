using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Resources;
using Feirb.Shared.AddressBook;
using Feirb.Shared.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Feirb.Api.Endpoints;

public static class AddressBookEndpoints
{
    private const int _defaultPageSize = 25;
    private const int _maxPageSize = 100;
    private const int _autocompleteLimit = 10;

    public static RouteGroupBuilder MapAddressBookEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/contacts", ListContactsAsync);
        group.MapGet("/contacts/{id:guid}", GetContactAsync);
        group.MapPost("/contacts", CreateContactAsync);
        group.MapPut("/contacts/{id:guid}", UpdateContactAsync);
        group.MapDelete("/contacts/{id:guid}", DeleteContactAsync);

        group.MapGet("/addresses", ListAddressesAsync);
        group.MapGet("/addresses/{id:guid}", GetAddressAsync);
        group.MapPut("/addresses/{id:guid}", UpdateAddressAsync);
        group.MapDelete("/addresses/{id:guid}", DeleteAddressAsync);
        group.MapPost("/addresses/{id:guid}/promote", PromoteAddressAsync);
        group.MapPost("/addresses/{id:guid}/link", LinkAddressAsync);
        group.MapPost("/addresses/{id:guid}/unlink", UnlinkAddressAsync);

        return group;
    }

    // --- Contacts ---

    private static async Task<IResult> ListContactsAsync(
        HttpContext httpContext,
        FeirbDbContext db,
        int? page,
        int? pageSize,
        string? q)
    {
        var userId = GetCurrentUserId(httpContext);
        var (pageNum, size) = NormalizePaging(page, pageSize);

        var query = db.Contacts.AsNoTracking().Where(c => c.UserId == userId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            // ToLower+Contains is portable across providers (InMemory tests + PostgreSQL).
            // Could be upgraded to EF.Functions.ILike for better index usage on large datasets.
            var term = q.Trim().ToLower();
            query = query.Where(c => c.DisplayName.ToLower().Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.DisplayName)
            .Skip((pageNum - 1) * size)
            .Take(size)
            .Select(c => new ContactListItem(
                c.Id,
                c.DisplayName,
                c.Addresses.Count,
                c.Addresses.OrderBy(a => a.NormalizedEmail).Select(a => a.NormalizedEmail).FirstOrDefault(),
                c.UpdatedAt))
            .ToListAsync();

        return Results.Ok(new PagedResult<ContactListItem>(items, total, pageNum, size));
    }

    private static async Task<IResult> GetContactAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var contact = await db.Contacts
            .AsNoTracking()
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (contact is null)
            return Results.NotFound(new { message = localizer["ContactNotFound"].Value });

        return Results.Ok(ToContactResponse(contact));
    }

    private static async Task<IResult> CreateContactAsync(
        CreateContactRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return Results.BadRequest(new { message = localizer["ContactDisplayNameRequired"].Value });

        var now = DateTime.UtcNow;
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = request.DisplayName.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Contacts.Add(contact);

        if (request.Emails is { Count: > 0 })
        {
            foreach (var rawEmail in request.Emails)
            {
                var normalized = EmailNormalizer.Normalize(rawEmail);
                if (normalized.Length == 0)
                    continue;

                var existing = await db.Addresses
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.NormalizedEmail == normalized);

                if (existing is null)
                {
                    db.Addresses.Add(new Address
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ContactId = contact.Id,
                        NormalizedEmail = normalized,
                        DisplayName = request.DisplayName.Trim(),
                        Status = AddressStatus.Known,
                        FirstSeenAt = now,
                        LastSeenAt = now,
                        SeenCount = 0,
                    });
                }
                else
                {
                    existing.ContactId = contact.Id;
                    if (existing.Status == AddressStatus.Unknown)
                        existing.Status = AddressStatus.Known;
                }
            }
        }

        await db.SaveChangesAsync();

        var created = await db.Contacts.AsNoTracking()
            .Include(c => c.Addresses)
            .FirstAsync(c => c.Id == contact.Id);

        return Results.Created($"/api/address-book/contacts/{contact.Id}", ToContactResponse(created));
    }

    private static async Task<IResult> UpdateContactAsync(
        Guid id,
        UpdateContactRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var contact = await db.Contacts
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (contact is null)
            return Results.NotFound(new { message = localizer["ContactNotFound"].Value });

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return Results.BadRequest(new { message = localizer["ContactDisplayNameRequired"].Value });

        contact.DisplayName = request.DisplayName.Trim();
        contact.Notes = request.Notes?.Trim();
        contact.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(ToContactResponse(contact));
    }

    private static async Task<IResult> DeleteContactAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var contact = await db.Contacts
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (contact is null)
            return Results.NotFound(new { message = localizer["ContactNotFound"].Value });

        // Explicitly orphan linked addresses (do not rely on DB cascade semantics)
        foreach (var addr in contact.Addresses)
            addr.ContactId = null;

        db.Contacts.Remove(contact);
        await db.SaveChangesAsync();
        return Results.Ok(new { message = localizer["ContactDeleted"].Value });
    }

    // --- Addresses ---

    private static async Task<IResult> ListAddressesAsync(
        HttpContext httpContext,
        FeirbDbContext db,
        int? page,
        int? pageSize,
        string? q)
    {
        var userId = GetCurrentUserId(httpContext);
        var (pageNum, size) = NormalizePaging(page, pageSize);

        var query = db.Addresses.AsNoTracking().Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(a => a.NormalizedEmail.Contains(term) || a.DisplayName.ToLower().Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.LastSeenAt)
            .Skip((pageNum - 1) * size)
            .Take(size)
            .Select(a => new AddressResponse(
                a.Id,
                a.ContactId,
                a.Contact != null ? a.Contact.DisplayName : null,
                a.NormalizedEmail,
                a.DisplayName,
                a.Status,
                a.FirstSeenAt,
                a.LastSeenAt,
                a.SeenCount))
            .ToListAsync();

        return Results.Ok(new PagedResult<AddressResponse>(items, total, pageNum, size));
    }

    private static async Task<IResult> GetAddressAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var address = await db.Addresses
            .AsNoTracking()
            .Include(a => a.Contact)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address is null)
            return Results.NotFound(new { message = localizer["AddressNotFound"].Value });

        return Results.Ok(ToAddressResponse(address));
    }

    private static async Task<IResult> UpdateAddressAsync(
        Guid id,
        UpdateAddressRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var address = await db.Addresses
            .Include(a => a.Contact)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address is null)
            return Results.NotFound(new { message = localizer["AddressNotFound"].Value });

        if (!Enum.IsDefined(request.Status))
            return Results.BadRequest(new { message = localizer["AddressStatusInvalid"].Value });

        address.DisplayName = request.DisplayName.Trim();
        address.Status = request.Status;

        await db.SaveChangesAsync();
        return Results.Ok(ToAddressResponse(address));
    }

    private static async Task<IResult> DeleteAddressAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var address = await db.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address is null)
            return Results.NotFound(new { message = localizer["AddressNotFound"].Value });

        if (address.ContactId is not null)
            return Results.Conflict(new { message = localizer["AddressLinkedCannotDelete"].Value });

        db.Addresses.Remove(address);
        await db.SaveChangesAsync();
        return Results.Ok(new { message = localizer["AddressDeleted"].Value });
    }

    private static async Task<IResult> PromoteAddressAsync(
        Guid id,
        PromoteAddressRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var address = await db.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address is null)
            return Results.NotFound(new { message = localizer["AddressNotFound"].Value });

        if (address.ContactId is not null)
            return Results.Conflict(new { message = localizer["AddressAlreadyLinked"].Value });

        if (request.Status is not (AddressStatus.Known or AddressStatus.Important))
            return Results.BadRequest(new { message = localizer["AddressStatusInvalid"].Value });

        var now = DateTime.UtcNow;
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = request.DisplayName.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Contacts.Add(contact);
        address.ContactId = contact.Id;
        address.Status = request.Status;

        await db.SaveChangesAsync();

        var created = await db.Contacts.AsNoTracking()
            .Include(c => c.Addresses)
            .FirstAsync(c => c.Id == contact.Id);

        return Results.Created($"/api/address-book/contacts/{contact.Id}", ToContactResponse(created));
    }

    private static async Task<IResult> LinkAddressAsync(
        Guid id,
        LinkAddressRequest request,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var address = await db.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address is null)
            return Results.NotFound(new { message = localizer["AddressNotFound"].Value });

        var contact = await db.Contacts
            .FirstOrDefaultAsync(c => c.Id == request.ContactId && c.UserId == userId);

        if (contact is null)
            return Results.NotFound(new { message = localizer["ContactNotFound"].Value });

        address.ContactId = contact.Id;
        await db.SaveChangesAsync();

        var reloaded = await db.Addresses.AsNoTracking()
            .Include(a => a.Contact)
            .FirstAsync(a => a.Id == id);

        return Results.Ok(ToAddressResponse(reloaded));
    }

    private static async Task<IResult> UnlinkAddressAsync(
        Guid id,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        var userId = GetCurrentUserId(httpContext);
        var address = await db.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address is null)
            return Results.NotFound(new { message = localizer["AddressNotFound"].Value });

        address.ContactId = null;
        await db.SaveChangesAsync();

        var reloaded = await db.Addresses.AsNoTracking()
            .Include(a => a.Contact)
            .FirstAsync(a => a.Id == id);

        return Results.Ok(ToAddressResponse(reloaded));
    }

    // --- Autocomplete (mounted on /api/mail group) ---

    public static RouteGroupBuilder MapRecipientSearch(this RouteGroupBuilder group)
    {
        group.MapGet("/recipients/search", SearchRecipientsAsync);
        return group;
    }

    private static async Task<IResult> SearchRecipientsAsync(
        HttpContext httpContext,
        FeirbDbContext db,
        string? q)
    {
        var userId = GetCurrentUserId(httpContext);
        var term = q?.Trim().ToLower() ?? string.Empty;

        var query = db.Addresses
            .AsNoTracking()
            .Include(a => a.Contact)
            .Where(a => a.UserId == userId);

        if (term.Length > 0)
        {
            query = query.Where(a =>
                a.NormalizedEmail.Contains(term) ||
                a.DisplayName.ToLower().Contains(term) ||
                (a.Contact != null && a.Contact.DisplayName.ToLower().Contains(term)));
        }

        // Rank: Important (5) > Known (3) > Unknown (2) > Blocked (1), then Recency
        var items = await query
            .Select(a => new
            {
                Address = a,
                Rank =
                    a.Status == AddressStatus.Important ? 5 :
                    a.Status == AddressStatus.Known ? 3 :
                    a.Status == AddressStatus.Unknown ? 2 :
                    1,
            })
            .OrderByDescending(x => x.Rank)
            .ThenByDescending(x => x.Address.LastSeenAt)
            .Take(_autocompleteLimit)
            .ToListAsync();

        var results = items
            .Select(x => new RecipientSuggestion(
                x.Address.Contact != null ? x.Address.Contact.DisplayName : x.Address.DisplayName,
                x.Address.NormalizedEmail,
                x.Address.Status))
            .ToList();

        return Results.Ok(results);
    }

    // --- Helpers ---

    private static (int page, int size) NormalizePaging(int? page, int? pageSize)
    {
        var p = page.GetValueOrDefault(1);
        if (p < 1) p = 1;
        var s = pageSize.GetValueOrDefault(_defaultPageSize);
        if (s < 1) s = _defaultPageSize;
        if (s > _maxPageSize) s = _maxPageSize;
        return (p, s);
    }

    private static ContactResponse ToContactResponse(Contact c) =>
        new(c.Id, c.DisplayName, c.Notes, c.CreatedAt, c.UpdatedAt,
            c.Addresses
                .OrderBy(a => a.NormalizedEmail)
                .Select(a => new AddressResponse(
                    a.Id, a.ContactId, c.DisplayName, a.NormalizedEmail, a.DisplayName,
                    a.Status,
                    a.FirstSeenAt, a.LastSeenAt, a.SeenCount))
                .ToList());

    private static AddressResponse ToAddressResponse(Address a) =>
        new(a.Id, a.ContactId, a.Contact?.DisplayName, a.NormalizedEmail, a.DisplayName,
            a.Status,
            a.FirstSeenAt, a.LastSeenAt, a.SeenCount);

    private static Guid GetCurrentUserId(HttpContext httpContext) =>
        Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
