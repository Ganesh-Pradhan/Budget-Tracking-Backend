using BudgetTracker.API.Data;
using BudgetTracker.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Configure CORS - restrict to allowed frontend origins
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularLocal", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Entity Framework Core
builder.Services.AddDbContext<BudgetTrackerDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "BudgetTrackerConnection")));

// Add Authentication (JWT)
// The signing key must be supplied through environment-specific
// configuration or a secret store. A placeholder or short key is never
// accepted, including in Development, so production cannot silently fall
// back to an insecure value.
const string placeholderKey = "REPLACE_WITH_STRONG_RANDOM_KEY_AT_LEAST_32_CHARS";
var configuredKey = builder.Configuration["Jwt:Key"];

var jwtKey = (string.IsNullOrWhiteSpace(configuredKey) ||
              string.Equals(configuredKey, placeholderKey, StringComparison.Ordinal) ||
              (configuredKey?.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase) ?? false))
    ? Environment.GetEnvironmentVariable("JWT_KEY")
    : configuredKey;
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key is not configured. Set Jwt:Key (or the JWT_KEY environment variable) " +
        "to a strong random value of at least 32 characters.");
}

if (string.Equals(jwtKey, placeholderKey, StringComparison.Ordinal) ||
    jwtKey.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase) ||
    jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Key is missing, is a placeholder, or is too short. " +
        "Use a cryptographically random key of at least 32 characters/bytes " +
        "(for example: openssl rand -base64 32).");
}

// Issuer and audience are not secrets, but they must be consistent between
// token generation and validation. Defaults are provided for local development;
// production deployments should set Jwt:Issuer and Jwt:Audience explicitly.
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BudgetTrackerAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BudgetTrackerClient";

if (!int.TryParse(builder.Configuration["Jwt:ExpiryHours"], out var jwtExpiryHours) ||
    jwtExpiryHours < 1 || jwtExpiryHours > 168)
{
    jwtExpiryHours = 24;
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5),
    };
});

builder.Services.AddAuthorization();

// Per-IP throttling for authentication endpoints. The partition key is the
// caller's remote IP address, so one client cannot consume the budget for all.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
});

// Add OpenAPI
builder.Services.AddOpenApi();

// Register AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure OpenAPI in Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AngularLocal");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the implicit Program class to integration tests
// (WebApplicationFactory<Program>).
public partial class Program { }


