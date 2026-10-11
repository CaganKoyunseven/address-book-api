using System.Text.Json.Serialization;
using AddressBook.Api.Extensions;
using AddressBook.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

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

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) =>
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync($"{{\"status\":\"{report.Status}\"}}");
    },
}).AllowAnonymous();

app.Run();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; can host the app in integration tests.</summary>
public partial class Program;
