using Feirb.Api.Data;
using Feirb.Api.Data.Entities;
using Feirb.Api.Services;
using Feirb.Shared.Auth;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", RegisterAsync);
        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        FeirbDbContext db,
        IAuthService authService)
    {
        var usernameExists = await db.Users.AnyAsync(u => u.Username == request.Username);
        if (usernameExists)
            return Results.Conflict(new { message = "Username is already taken." });

        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Results.Conflict(new { message = "Email is already registered." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = authService.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = new RegisterResponse(user.Id, user.Username, user.Email);
        return Results.Created($"/api/auth/users/{user.Id}", response);
    }
}
