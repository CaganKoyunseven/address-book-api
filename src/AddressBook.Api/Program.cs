using System.Text;
using System.Text.Json.Serialization;
using AddressBook.Api.Data;
using AddressBook.Api.Extensions;
using AddressBook.Api.Middleware;
using AddressBook.Api.Models;
using AddressBook.Api.Options;
using AddressBook.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Render injects the port to listen on via the PORT env var; never hard-code a port.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks();

builder.Services.AddAddressBookSwagger();

// Repositories hold the actual data, so they are singletons; everything else is scoped per request.
builder.Services.AddSingleton<IContactRepository, InMemoryContactRepository>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection("Seed"));

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IContactService, ContactService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Values are read lazily (inside this callback) rather than eagerly, so test hosts
        // that override configuration before the first request still take effect.
        options.MapInboundClaims = false; // keep short claim names: sub/name/role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(RequireJwtKey(jwtSection))),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization();

builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddAddressBookRateLimiting();

builder.Services.AddCors(options =>
{
    // Public demo API, no cookies/credentials involved, so any origin is fine.
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Render terminates TLS and talks to the container over plain HTTP, forwarding the
// original client IP/scheme in headers. Its proxy IPs are not static, so we trust all of them.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

using (var seedScope = app.Services.CreateScope())
{
    var services = seedScope.ServiceProvider;
    await SeedData.SeedAsync(
        services.GetRequiredService<IUserRepository>(),
        services.GetRequiredService<IContactRepository>(),
        services.GetRequiredService<IPasswordHasher<User>>(),
        services.GetRequiredService<IOptions<SeedOptions>>(),
        services.GetRequiredService<ILogger<Program>>(),
        CancellationToken.None);
}

app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Address Book API v1");
    options.RoutePrefix = string.Empty; // serve the UI at the site root
});

app.UseCors();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) =>
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync($"{{\"status\":\"{report.Status}\"}}");
    },
}).AllowAnonymous().DisableRateLimiting();

app.Run();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; can host the app in integration tests.</summary>
public partial class Program
{
    /// <summary>Fails fast at startup if the signing key is missing or too weak to be HS256-safe.</summary>
    private static string RequireJwtKey(IConfigurationSection jwtSection)
    {
        var key = jwtSection["Key"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException(
                "Configuration 'Jwt:Key' is missing or shorter than 32 characters. " +
                "Set it in appsettings (Development only) or via the Jwt__Key environment variable.");
        }

        return key;
    }
}
