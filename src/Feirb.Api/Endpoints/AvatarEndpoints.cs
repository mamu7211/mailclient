using System.Security.Claims;
using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Resources;
using Feirb.Shared.Avatars;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SkiaSharp;

namespace Feirb.Api.Endpoints;

public static class AvatarEndpoints
{
    private const int _avatarSize = 256;

    public static RouteGroupBuilder MapAvatarEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/avatars/{md5hash}", GetAvatarAsync).AllowAnonymous();
        group.MapPut("/avatars/{md5hash}", UploadAvatarAsync).DisableAntiforgery();
        group.MapDelete("/avatars/{md5hash}", DeleteAvatarAsync);
        return group;
    }

    private static async Task<IResult> GetAvatarAsync(
        string md5hash,
        FeirbDbContext db)
    {
        var avatar = await db.Avatars.FirstOrDefaultAsync(a => a.EmailHash == md5hash);
        if (avatar is null)
            return Results.NoContent();

        return Results.Bytes(
            avatar.ImageData,
            contentType: "image/png");
    }

    private static async Task<IResult> UploadAvatarAsync(
        string md5hash,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        if (!await IsAuthorizedForHashAsync(md5hash, httpContext, db))
            return Results.Forbid();

        var form = await httpContext.Request.ReadFormAsync();
        var file = form.Files.Count > 0 ? form.Files[0] : null;
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = localizer["AvatarInvalidImage"].Value });

        byte[] resizedPng;
        try
        {
            resizedPng = await ResizeImageAsync(file);
        }
        catch (InvalidOperationException)
        {
            return Results.BadRequest(new { message = localizer["AvatarInvalidImage"].Value });
        }

        var avatar = await db.Avatars.FirstOrDefaultAsync(a => a.EmailHash == md5hash);
        if (avatar is null)
        {
            avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                EmailHash = md5hash,
                Email = await ResolveEmailForHashAsync(md5hash, httpContext, db) ?? md5hash,
                ImageData = resizedPng,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Avatars.Add(avatar);
        }
        else
        {
            avatar.ImageData = resizedPng;
            avatar.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAvatarAsync(
        string md5hash,
        HttpContext httpContext,
        FeirbDbContext db,
        IStringLocalizer<ApiMessages> localizer)
    {
        if (!httpContext.User.IsInRole("Admin"))
            return Results.Forbid();

        var avatar = await db.Avatars.FirstOrDefaultAsync(a => a.EmailHash == md5hash);
        if (avatar is null)
            return Results.NotFound(new { message = localizer["AvatarNotFound"].Value });

        db.Avatars.Remove(avatar);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<bool> IsAuthorizedForHashAsync(
        string md5hash,
        HttpContext httpContext,
        FeirbDbContext db)
    {
        if (httpContext.User.IsInRole("Admin"))
            return true;

        var userId = GetCurrentUserId(httpContext);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return false;

        if (AvatarHashHelper.ComputeEmailHash(user.Email) == md5hash)
            return true;

        var mailboxEmails = await db.Mailboxes
            .Where(m => m.UserId == userId)
            .Select(m => m.EmailAddress)
            .ToListAsync();

        return mailboxEmails.Any(email => AvatarHashHelper.ComputeEmailHash(email) == md5hash);
    }

    private static async Task<string?> ResolveEmailForHashAsync(
        string md5hash,
        HttpContext httpContext,
        FeirbDbContext db)
    {
        var userId = GetCurrentUserId(httpContext);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is not null && AvatarHashHelper.ComputeEmailHash(user.Email) == md5hash)
            return user.Email;

        var mailboxEmail = await db.Mailboxes
            .Where(m => m.UserId == userId)
            .Select(m => m.EmailAddress)
            .FirstOrDefaultAsync(email => AvatarHashHelper.ComputeEmailHash(email) == md5hash);

        return mailboxEmail;
    }

    private static async Task<byte[]> ResizeImageAsync(IFormFile file)
    {
        using var inputStream = new MemoryStream();
        await file.CopyToAsync(inputStream);
        inputStream.Position = 0;

        using var original = SKBitmap.Decode(inputStream);
        if (original is null)
            throw new InvalidOperationException("Not a valid image.");

        using var resized = original.Resize(new SKImageInfo(_avatarSize, _avatarSize), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        if (resized is null)
            throw new InvalidOperationException("Failed to resize image.");

        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static Guid GetCurrentUserId(HttpContext httpContext) =>
        Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
