using System.Reflection;
using System.Threading.RateLimiting;
using AddressBook.Api.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace AddressBook.Api.Extensions;

/// <summary>
/// Keeps Program.cs short by moving one-time service wiring (Swagger, rate limiting, etc.) here.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAddressBookSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Address Book API",
                Version = "v1",
                Description = "A REST API for managing a personal address book: contacts with search, filter, sort, pagination and favorites. " +
                              "Log in via /api/v1/auth/login with the seeded demo credentials, then use the Authorize button with the returned token.",
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Infers the "Bearer" scheme from the JWT bearer authentication handler registered
            // in Program.cs, so the Swagger UI "Authorize" button appears automatically.
            options.InferSecuritySchemes();
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            });
        });

        return services;
    }

    /// <summary>
    /// Global limit partitioned by user id (or IP for anonymous callers), plus a stricter named
    /// "auth" policy for brute-force protection on /auth/*. Limits come from "RateLimiting" in
    /// configuration and are read per-request via IOptionsMonitor, so test hosts that override
    /// them before the first request (see AddressBook.Tests) take effect.
    /// </summary>
    public static IServiceCollection AddAddressBookRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var limits = CurrentLimits(httpContext).Global;
                var userId = httpContext.User.FindFirst("sub")?.Value;
                var partitionKey = userId is not null ? $"user:{userId}" : $"ip:{RemoteIp(httpContext)}";
                return FixedWindow(partitionKey, limits);
            });

            options.AddPolicy("auth", httpContext =>
            {
                var limits = CurrentLimits(httpContext).Auth;
                var partitionKey = $"ip:{RemoteIp(httpContext)}";
                return FixedWindow(partitionKey, limits);
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Type = "https://tools.ietf.org/html/rfc6585#section-4",
                };
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken);
            };
        });

        return services;
    }

    private static RateLimitOptions CurrentLimits(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitOptions>>().CurrentValue;

    private static string RemoteIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static RateLimitPartition<string> FixedWindow(string partitionKey, RateLimitPolicyOptions limits) =>
        RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limits.PermitLimit,
            Window = TimeSpan.FromSeconds(limits.WindowSeconds),
            QueueLimit = 0,
        });
}
