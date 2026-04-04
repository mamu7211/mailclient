using System.Text;
using Feirb.Api.Data;
using Feirb.Api.Endpoints;
using Feirb.Api.Services;
using Feirb.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
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

// Data Protection API for credential encryption — keys persisted to PostgreSQL
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<FeirbDbContext>();

// Localization
builder.Services.AddLocalization();

// IMAP sync configuration
builder.Services.Configure<ImapSyncSettings>(builder.Configuration.GetSection(ImapSyncSettings.SectionName));

// Quartz.NET scheduler
builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// Managed job infrastructure
builder.Services.AddManagedJobInfrastructure();
builder.Services.AddManagedJob<ImapSyncJob>("imap-sync");
builder.Services.AddManagedJob<ClassificationJob>("classification");

// AI / LLM — OllamaSharp via Aspire service discovery
// Connection string name follows Aspire convention: "{ollama-resource}-{model-name}" (tag stripped)
// Note: OllamaSharp manages its own HttpClient internally, not via IHttpClientFactory
builder.AddOllamaApiClient("feirb-ollama-qwen3").AddChatClient();


// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IImapSyncService, ImapSyncService>();
builder.Services.AddScoped<IClassificationService, ClassificationService>();

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
        var dataProtection = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        await DatabaseSeeder.SeedAsync(db, logger, dataProtection, app.Configuration);
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

// Dev config endpoint (anonymous, returns feature flags for frontend)
app.MapGet("/api/dev/config", (IConfiguration config) => Results.Ok(new
{
    AutoLogin = string.Equals(config["AUTO_LOGIN"], "true", StringComparison.OrdinalIgnoreCase)
})).AllowAnonymous();

// Setup endpoints are anonymous (guarded by admin-exists check)
var setupGroup = app.MapGroup(ApiRoutes.Setup).AllowAnonymous();
setupGroup.MapSetupEndpoints();

// Job endpoints (role-based filtering: admin sees all, user sees own + system)
var jobsGroup = apiGroup.MapGroup("/jobs").RequireAuthorization();
jobsGroup.MapJobSettingsEndpoints();

// Admin endpoints require admin role
var adminGroup = apiGroup.MapGroup("/admin").RequireAuthorization("RequireAdmin");
adminGroup.MapAdminEndpoints();
var systemSettingsGroup = adminGroup.MapGroup("/system-settings");
systemSettingsGroup.MapSystemSettingsEndpoints();
var adminJobsGroup = adminGroup.MapGroup("/jobs");
adminJobsGroup.MapJobStatsEndpoints();

// Settings endpoints (per-user, JWT required)
var settingsGroup = apiGroup.MapGroup("/settings");
settingsGroup.MapMailboxEndpoints();
settingsGroup.MapLabelEndpoints();
settingsGroup.MapProfileEndpoints();
settingsGroup.MapPreferencesEndpoints();
settingsGroup.MapClassificationRuleEndpoints();

// Mail test endpoints (per-user, JWT required)
var mailGroup = apiGroup.MapGroup("/mail");
mailGroup.MapMailTestEndpoints();
mailGroup.MapMessageEndpoints();
mailGroup.MapMailStatsEndpoints();

// Avatar endpoints (GET is public, PUT/DELETE require JWT)
apiGroup.MapAvatarEndpoints();

// Dashboard endpoints (per-user, JWT required)
var dashboardGroup = apiGroup.MapGroup("/dashboard");
dashboardGroup.MapDashboardEndpoints();
dashboardGroup.MapWidgetConfigEndpoints();

app.MapFallbackToFile("index.html");

app.Run();

// Make the implicit Program class accessible for integration tests
public partial class Program;
