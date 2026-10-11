using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AddressBook.Tests.Integration;

/// <summary>
/// Hosts the real app in-process (Program.cs pipeline, middleware, seeding and all) for
/// HTTP-level tests. The global rate limit is relaxed so unrelated tests never trip it;
/// <see cref="RateLimitTests"/> uses a subclass with a tiny auth limit instead.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("RateLimiting:Global:PermitLimit", "100000");
        builder.UseSetting("RateLimiting:Global:WindowSeconds", "60");
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "100000");
        builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
    }
}
