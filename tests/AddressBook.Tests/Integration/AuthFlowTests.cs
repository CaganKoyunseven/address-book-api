using System.Net;
using System.Net.Http.Json;

namespace AddressBook.Tests.Integration;

public class AuthFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_then_login_succeeds()
    {
        var username = AuthTestHelper.UniqueUsername();

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new { username, password = AuthTestHelper.DefaultPassword });
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username, password = AuthTestHelper.DefaultPassword });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Register_with_a_taken_username_returns_409()
    {
        var username = AuthTestHelper.UniqueUsername();
        await _client.PostAsJsonAsync("/api/v1/auth/register", new { username, password = AuthTestHelper.DefaultPassword });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new { username, password = AuthTestHelper.DefaultPassword });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_and_unknown_username_return_the_same_401()
    {
        var username = AuthTestHelper.UniqueUsername();
        await _client.PostAsJsonAsync("/api/v1/auth/register", new { username, password = AuthTestHelper.DefaultPassword });

        var wrongPassword = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username, password = "WrongPassword1" });
        var unknownUser = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username = AuthTestHelper.UniqueUsername(), password = "WrongPassword1" });
        var wrongPasswordBody = await wrongPassword.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var unknownUserBody = await unknownUser.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        // Same title/status/type for both: callers can't tell "wrong password" from "unknown user" apart. traceId legitimately differs per request.
        Assert.Equal(wrongPasswordBody.GetProperty("title").GetString(), unknownUserBody.GetProperty("title").GetString());
        Assert.Equal(wrongPasswordBody.GetProperty("status").GetInt32(), unknownUserBody.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Calling_a_protected_endpoint_without_a_token_returns_401_problem_json()
    {
        var response = await _client.GetAsync("/api/v1/contacts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Calling_a_protected_endpoint_with_a_garbage_token_returns_401()
    {
        using var client = factory.CreateClient().WithToken("not-a-real-jwt");

        var response = await client.GetAsync("/api/v1/contacts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_login_works_and_returns_the_admin_role()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin", password = "Admin#12345" });
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Admin", body.GetProperty("role").GetString());
    }
}
