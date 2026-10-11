using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AddressBook.Tests.Integration;

public class AdminApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Non_admin_hitting_admin_contacts_gets_403()
    {
        using var client = factory.CreateClient();
        var token = await AuthTestHelper.RegisterAndLoginAsync(client);
        client.WithToken(token);

        var response = await client.GetAsync("/api/v1/admin/contacts");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Admin_sees_every_users_contacts_with_owner_info()
    {
        using var ownerClient = factory.CreateClient();
        var username = AuthTestHelper.UniqueUsername("owner");
        var ownerToken = await AuthTestHelper.RegisterAndLoginAsync(ownerClient, username);
        ownerClient.WithToken(ownerToken);
        await ownerClient.PostAsJsonAsync("/api/v1/contacts", new { firstName = "Owned", lastName = "ByThisUser" });

        using var adminClient = factory.CreateClient().WithToken(await AuthTestHelper.LoginAsync(factory.CreateClient(), "admin", "Admin#12345"));
        var response = await adminClient.GetAsync("/api/v1/admin/contacts?pageSize=100");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            body.GetProperty("items").EnumerateArray(),
            c => c.GetProperty("ownerUsername").GetString() == username && c.GetProperty("lastName").GetString() == "ByThisUser");
    }

    [Fact]
    public async Task Delete_then_includeDeleted_then_restore_round_trip()
    {
        using var ownerClient = factory.CreateClient();
        var ownerToken = await AuthTestHelper.RegisterAndLoginAsync(ownerClient);
        ownerClient.WithToken(ownerToken);
        var created = await (await ownerClient.PostAsJsonAsync("/api/v1/contacts", new { firstName = "Temp", lastName = "Contact" })).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();
        await ownerClient.DeleteAsync($"/api/v1/contacts/{id}");

        using var adminClient = factory.CreateClient().WithToken(await AuthTestHelper.LoginAsync(factory.CreateClient(), "admin", "Admin#12345"));

        var withoutDeleted = await (await adminClient.GetAsync("/api/v1/admin/contacts?pageSize=100")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(withoutDeleted.GetProperty("items").EnumerateArray(), c => c.GetProperty("id").GetString() == id);

        var withDeleted = await (await adminClient.GetAsync("/api/v1/admin/contacts?pageSize=100&includeDeleted=true")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(withDeleted.GetProperty("items").EnumerateArray(), c => c.GetProperty("id").GetString() == id);

        var restoreResponse = await adminClient.PostAsync($"/api/v1/admin/contacts/{id}/restore", content: null);
        Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);

        var ownerSeesItAgain = await ownerClient.GetAsync($"/api/v1/contacts/{id}");
        Assert.Equal(HttpStatusCode.OK, ownerSeesItAgain.StatusCode);
    }

    [Fact]
    public async Task Restoring_an_unknown_id_returns_404()
    {
        using var adminClient = factory.CreateClient().WithToken(await AuthTestHelper.LoginAsync(factory.CreateClient(), "admin", "Admin#12345"));

        var response = await adminClient.PostAsync($"/api/v1/admin/contacts/{Guid.NewGuid()}/restore", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
