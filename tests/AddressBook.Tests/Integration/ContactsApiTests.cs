using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AddressBook.Tests.Integration;

public class ContactsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<HttpClient> NewAuthorizedClientAsync()
    {
        var client = factory.CreateClient();
        var token = await AuthTestHelper.RegisterAndLoginAsync(client);
        return client.WithToken(token);
    }

    [Fact]
    public async Task Full_flow_create_list_search_sort_favorite_delete()
    {
        using var client = await NewAuthorizedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/contacts", new
        {
            firstName = "Alan",
            lastName = "Turing",
            email = "alan.turing@example.com",
            tags = new[] { "Work" },
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        // the Location header points at the resource that was just created
        var getByLocation = await client.GetAsync(createResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getByLocation.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/contacts");
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(list.GetProperty("items").EnumerateArray(), c => c.GetProperty("id").GetString() == id);

        var searchResponse = await client.GetAsync("/api/v1/contacts?search=turing");
        var searchResult = await searchResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, searchResult.GetProperty("totalCount").GetInt32());

        var sortResponse = await client.GetAsync("/api/v1/contacts?sortBy=name&sortDir=asc");
        Assert.Equal(HttpStatusCode.OK, sortResponse.StatusCode);

        var favoriteResponse = await client.PutAsync($"/api/v1/contacts/{id}/favorite", content: null);
        Assert.Equal(HttpStatusCode.NoContent, favoriteResponse.StatusCode);
        var afterFavorite = await (await client.GetAsync($"/api/v1/contacts/{id}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(afterFavorite.GetProperty("isFavorite").GetBoolean());

        var deleteResponse = await client.DeleteAsync($"/api/v1/contacts/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listAfterDelete = await (await client.GetAsync("/api/v1/contacts")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(listAfterDelete.GetProperty("items").EnumerateArray(), c => c.GetProperty("id").GetString() == id);
    }

    [Fact]
    public async Task Another_users_contact_returns_404_not_403()
    {
        using var ownerClient = await NewAuthorizedClientAsync();
        var createResponse = await ownerClient.PostAsJsonAsync("/api/v1/contacts", new { firstName = "Secret", lastName = "Owner" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        using var otherClient = await NewAuthorizedClientAsync();
        var response = await otherClient.GetAsync($"/api/v1/contacts/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Favorite_put_and_delete_are_idempotent()
    {
        using var client = await NewAuthorizedClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/contacts", new { firstName = "A", lastName = "B" })).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var put1 = await client.PutAsync($"/api/v1/contacts/{id}/favorite", content: null);
        var put2 = await client.PutAsync($"/api/v1/contacts/{id}/favorite", content: null);
        var delete1 = await client.DeleteAsync($"/api/v1/contacts/{id}/favorite");
        var delete2 = await client.DeleteAsync($"/api/v1/contacts/{id}/favorite");

        Assert.Equal(HttpStatusCode.NoContent, put1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, put2.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete2.StatusCode);
    }

    [Fact]
    public async Task Deleting_the_same_contact_twice_is_204_then_404()
    {
        using var client = await NewAuthorizedClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/contacts", new { firstName = "A", lastName = "B" })).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var first = await client.DeleteAsync($"/api/v1/contacts/{id}");
        var second = await client.DeleteAsync($"/api/v1/contacts/{id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }
}
