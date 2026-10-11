using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AddressBook.Tests.Integration;

public class ErrorFormatTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<HttpClient> NewAuthorizedClientAsync()
    {
        var client = factory.CreateClient();
        var token = await AuthTestHelper.RegisterAndLoginAsync(client);
        return client.WithToken(token);
    }

    [Theory]
    [InlineData("{\"lastName\":\"Doe\"}")] // missing first name
    [InlineData("{\"firstName\":\"A\",\"lastName\":\"B\",\"email\":\"not-an-email\"}")] // bad email
    [InlineData("{\"firstName\":\"A\",\"lastName\":\"B\",\"phone\":\"call me maybe\"}")] // bad phone
    public async Task Invalid_contact_payloads_return_400_with_errors(string json)
    {
        using var client = await NewAuthorizedClientAsync();
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/contacts", content);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(body.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Too_many_tags_returns_400()
    {
        using var client = await NewAuthorizedClientAsync();
        var tags = Enumerable.Range(0, 11).Select(i => $"tag{i}").ToArray();

        var response = await client.PostAsJsonAsync("/api/v1/contacts", new { firstName = "A", lastName = "B", tags });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PageSize_over_the_max_returns_400()
    {
        using var client = await NewAuthorizedClientAsync();

        var response = await client.GetAsync("/api/v1/contacts?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_sortBy_value_returns_400()
    {
        using var client = await NewAuthorizedClientAsync();

        var response = await client.GetAsync("/api/v1/contacts?sortBy=banana");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unmatched_route_returns_404_problem_json()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/this-route-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Swagger_json_is_served_and_lists_the_expected_paths()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var paths = body.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/v1/auth/login", paths);
        Assert.Contains("/api/v1/contacts", paths);
        Assert.Contains("/api/v1/admin/contacts", paths);
        Assert.Contains("/api/v1/tags", paths);
    }

    [Fact]
    public async Task Swagger_ui_is_served_at_the_site_root()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Swagger UI", await response.Content.ReadAsStringAsync());
    }
}
