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
    });
builder.Services.AddAuthorization();

// Data Protection API for credential encryption
builder.Services.AddDataProtection();

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
}

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapDefaultEndpoints();
app.MapGet("/", () => "Feirb API");

// Require authorization on all /api/* endpoints by default
var apiGroup = app.MapGroup("/api").RequireAuthorization();

// Auth endpoints are anonymous
var authGroup = app.MapGroup(ApiRoutes.Auth).AllowAnonymous();
authGroup.MapAuthEndpoints();

app.Run();

// Make the implicit Program class accessible for integration tests
public partial class Program;
