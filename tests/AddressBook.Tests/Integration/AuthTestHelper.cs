using System.Net.Http.Json;
using System.Text.Json;

namespace AddressBook.Tests.Integration;

/// <summary>Small shared helpers so each test file doesn't repeat register/login boilerplate.</summary>
internal static class AuthTestHelper
{
    public const string DefaultPassword = "Test#12345";

    public static string UniqueUsername(string prefix = "user")
    {
        var name = $"{prefix}{Guid.NewGuid():N}";
        return name.Length > 30 ? name[..30] : name;
    }

    public static async Task<string> RegisterAndLoginAsync(HttpClient client, string? username = null, string password = DefaultPassword)
    {
        username ??= UniqueUsername();
        await client.PostAsJsonAsync("/api/v1/auth/register", new { username, password });
        return await LoginAsync(client, username, password);
    }

    public static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
