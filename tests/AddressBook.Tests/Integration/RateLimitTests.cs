using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace AddressBook.Tests.Integration;

/// <summary>A tiny auth permit limit so the 429 path is reachable in a handful of requests.</summary>
public sealed class StrictAuthRateLimitApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "3");
        builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
    }
}

public class RateLimitTests(StrictAuthRateLimitApiFactory factory) : IClassFixture<StrictAuthRateLimitApiFactory>
{
    [Fact]
    public async Task Exceeding_the_auth_limit_returns_429_with_retry_after()
    {
        using var client = factory.CreateClient();
        var login = new { username = "nobody-at-all", password = "WrongPassword1" };

        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++)
        {
            last = await client.PostAsJsonAsync("/api/v1/auth/login", login);
        }

        var fourth = await client.PostAsJsonAsync("/api/v1/auth/login", login);

        Assert.Equal(HttpStatusCode.Unauthorized, last!.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, fourth.StatusCode);
        Assert.True(fourth.Headers.RetryAfter is not null);
        Assert.Equal("application/problem+json", fourth.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Health_is_never_rate_limited_even_after_exhausting_the_auth_limit()
    {
        using var client = factory.CreateClient();
        var login = new { username = "nobody-at-all", password = "WrongPassword1" };
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login", login);
        }

        var health = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }
}
