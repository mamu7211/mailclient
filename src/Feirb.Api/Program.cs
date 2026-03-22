using System.Text;
using Feirb.Api.Data;
using Feirb.Api.Endpoints;
using Feirb.Api.Services;
using Feirb.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// Database
builder.AddNpgsqlDbContext<FeirbDbContext>("mailclientdb");

// JWT configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<FeirbDbContext>();
                var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var stampClaim = context.Principal?.FindFirst("security_stamp")?.Value;

                if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Invalid token.");
                    return;
                }

                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (user is null || user.SecurityStamp != stampClaim)
                {
                    context.Fail("Token has been revoked.");
                }
            },
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
});

// Data Protection API for credential encryption
builder.Services.AddDataProtection();

// Localization
builder.Services.AddLocalization();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Run migrations on startup (skip for in-memory databases used in tests)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FeirbDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    // Seed development data when FEIRB_SEED_DATA=true
    if (string.Equals(app.Configuration["FEIRB_SEED_DATA"], "true", StringComparison.OrdinalIgnoreCase))
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        await DatabaseSeeder.SeedAsync(db, logger);
    }
}

var supportedCultures = new[] { "en-US", "de-DE", "fr-FR", "it-IT", "de", "fr", "it" };
app.UseRequestLocalization(options =>
    options.SetDefaultCulture("en-US")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures));

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapDefaultEndpoints();

// Require authorization on all /api/* endpoints by default
var apiGroup = app.MapGroup("/api").RequireAuthorization();

// Auth endpoints are anonymous
var authGroup = app.MapGroup(ApiRoutes.Auth).AllowAnonymous();
authGroup.MapAuthEndpoints();

// Setup endpoints are anonymous (guarded by admin-exists check)
var setupGroup = app.MapGroup(ApiRoutes.Setup).AllowAnonymous();
setupGroup.MapSetupEndpoints();

// Admin endpoints require admin role
var adminGroup = apiGroup.MapGroup("/admin").RequireAuthorization("RequireAdmin");
adminGroup.MapAdminEndpoints();
var systemSettingsGroup = adminGroup.MapGroup("/system-settings");
systemSettingsGroup.MapSystemSettingsEndpoints();

// Settings endpoints (per-user, JWT required)
var settingsGroup = apiGroup.MapGroup("/settings");
settingsGroup.MapMailboxEndpoints();
settingsGroup.MapProfileEndpoints();

// Mail test endpoints (per-user, JWT required)
var mailGroup = apiGroup.MapGroup("/mail");
mailGroup.MapMailTestEndpoints();

app.MapFallbackToFile("index.html");

app.Run();

// Make the implicit Program class accessible for integration tests
public partial class Program;
