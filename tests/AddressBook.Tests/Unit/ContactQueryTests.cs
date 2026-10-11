using AddressBook.Api.Data;
using AddressBook.Api.Dtos;
using AddressBook.Api.Services;

namespace AddressBook.Tests.Unit;

/// <summary>Covers search/filter/sort/paging and /tags behavior of ContactService.</summary>
public class ContactQueryTests
{
    private readonly ContactService _service = new(new InMemoryContactRepository(), new InMemoryUserRepository());
    private readonly Guid _ownerId = Guid.NewGuid();

    private async Task<ContactResponseDto> SeedAsync(
        string first, string last, string? email = null, string? city = null,
        string[]? tags = null, bool favorite = false, Guid? ownerId = null)
    {
        var dto = new ContactCreateDto
        {
            FirstName = first,
            LastName = last,
            Email = email,
            Address = city is null ? null : new AddressDto { City = city },
            Tags = tags?.ToList() ?? [],
            IsFavorite = favorite,
        };
        return await _service.CreateAsync(ownerId ?? _ownerId, dto, CancellationToken.None);
    }

    [Fact]
    public async Task ListAsync_only_returns_the_callers_own_contacts()
    {
        var otherOwner = Guid.NewGuid();
        await SeedAsync("Alan", "Turing");
        await SeedAsync("Marie", "Curie", ownerId: otherOwner);

        var result = await _service.ListAsync(_ownerId, new ContactQuery(), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Turing", result.Items[0].LastName);
    }

    [Fact]
    public async Task ListAsync_hides_soft_deleted_contacts()
    {
        var created = await SeedAsync("Alan", "Turing");
        await _service.DeleteAsync(_ownerId, created.Id, CancellationToken.None);

        var result = await _service.ListAsync(_ownerId, new ContactQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
    }

    [Theory]
    [InlineData("turing")]
    [InlineData("alan tur")]
    [InlineData("ALAN@EXAMPLE.COM")]
    [InlineData("london")]
    [InlineData("work")]
    public async Task ListAsync_search_matches_across_name_email_city_and_tags(string search)
    {
        await SeedAsync("Alan", "Turing", email: "alan@example.com", city: "London", tags: ["Work"]);

        var result = await _service.ListAsync(_ownerId, new ContactQuery { Search = search }, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task ListAsync_search_with_no_matches_returns_empty()
    {
        await SeedAsync("Alan", "Turing");

        var result = await _service.ListAsync(_ownerId, new ContactQuery { Search = "nobody" }, CancellationToken.None);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ListAsync_filters_by_tag_case_insensitively()
    {
        await SeedAsync("Alan", "Turing", tags: ["Work"]);
        await SeedAsync("Marie", "Curie", tags: ["Family"]);

        var result = await _service.ListAsync(_ownerId, new ContactQuery { Tag = "WORK" }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Turing", result.Items[0].LastName);
    }

    [Fact]
    public async Task ListAsync_filters_by_favorite()
    {
        await SeedAsync("Alan", "Turing", favorite: true);
        await SeedAsync("Marie", "Curie", favorite: false);

        var favorites = await _service.ListAsync(_ownerId, new ContactQuery { Favorite = true }, CancellationToken.None);
        var nonFavorites = await _service.ListAsync(_ownerId, new ContactQuery { Favorite = false }, CancellationToken.None);

        Assert.Single(favorites.Items);
        Assert.Equal("Turing", favorites.Items[0].LastName);
        Assert.Single(nonFavorites.Items);
        Assert.Equal("Curie", nonFavorites.Items[0].LastName);
    }

    [Fact]
    public async Task ListAsync_sorts_by_name_last_then_first_ascending_by_default()
    {
        await SeedAsync("Bob", "Zephyr");
        await SeedAsync("Ada", "Albert");
        await SeedAsync("Zoe", "Albert");

        var result = await _service.ListAsync(_ownerId, new ContactQuery(), CancellationToken.None);

        Assert.Equal(["Ada", "Zoe", "Bob"], result.Items.Select(c => c.FirstName));
    }

    [Fact]
    public async Task ListAsync_sorts_by_name_descending()
    {
        await SeedAsync("Alan", "Turing");
        await SeedAsync("Marie", "Curie");

        var result = await _service.ListAsync(
            _ownerId, new ContactQuery { SortBy = ContactSortBy.Name, SortDir = SortDirection.Desc }, CancellationToken.None);

        Assert.Equal(["Turing", "Curie"], result.Items.Select(c => c.LastName));
    }

    [Fact]
    public async Task ListAsync_sorts_by_email()
    {
        await SeedAsync("Alan", "Turing", email: "z@example.com");
        await SeedAsync("Marie", "Curie", email: "a@example.com");

        var result = await _service.ListAsync(_ownerId, new ContactQuery { SortBy = ContactSortBy.Email }, CancellationToken.None);

        Assert.Equal(["a@example.com", "z@example.com"], result.Items.Select(c => c.Email));
    }

    [Fact]
    public async Task ListAsync_sorts_by_created_at()
    {
        var first = await SeedAsync("Alan", "Turing");
        await Task.Delay(5);
        var second = await SeedAsync("Marie", "Curie");

        var result = await _service.ListAsync(_ownerId, new ContactQuery { SortBy = ContactSortBy.CreatedAt }, CancellationToken.None);

        Assert.Equal([first.Id, second.Id], result.Items.Select(c => c.Id));
    }

    [Fact]
    public async Task ListAsync_favorites_first_overrides_the_normal_sort_order()
    {
        await SeedAsync("Alan", "Zephyr", favorite: true);
        await SeedAsync("Marie", "Albert", favorite: false);

        var result = await _service.ListAsync(
            _ownerId, new ContactQuery { FavoritesFirst = true }, CancellationToken.None);

        // "Albert" would normally sort before "Zephyr", but the favorite comes first.
        Assert.Equal(["Zephyr", "Albert"], result.Items.Select(c => c.LastName));
    }

    [Fact]
    public async Task ListAsync_tie_breaks_identical_names_deterministically_by_id()
    {
        var a = await SeedAsync("Hans", "Einstein");
        var b = await SeedAsync("Hans", "Einstein");

        var result1 = await _service.ListAsync(_ownerId, new ContactQuery(), CancellationToken.None);
        var result2 = await _service.ListAsync(_ownerId, new ContactQuery(), CancellationToken.None);

        var expectedOrder = a.Id.CompareTo(b.Id) <= 0 ? new[] { a.Id, b.Id } : [b.Id, a.Id];
        Assert.Equal(expectedOrder, result1.Items.Select(c => c.Id));
        Assert.Equal(expectedOrder, result2.Items.Select(c => c.Id));
    }

    [Fact]
    public async Task ListAsync_pages_correctly_including_a_partial_last_page()
    {
        for (var i = 0; i < 24; i++)
        {
            await SeedAsync($"First{i:D2}", $"Last{i:D2}");
        }

        var lastPage = await _service.ListAsync(_ownerId, new ContactQuery { Page = 5, PageSize = 5 }, CancellationToken.None);

        Assert.Equal(4, lastPage.Items.Count);
        Assert.Equal(24, lastPage.TotalCount);
        Assert.Equal(5, lastPage.TotalPages);
    }

    [Fact]
    public async Task ListAsync_page_beyond_the_last_returns_empty_items_but_correct_totals()
    {
        await SeedAsync("Alan", "Turing");

        var result = await _service.ListAsync(_ownerId, new ContactQuery { Page = 99, PageSize = 10 }, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task ListAsync_with_no_results_has_zero_total_pages()
    {
        var result = await _service.ListAsync(_ownerId, new ContactQuery(), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetTagsAsync_returns_distinct_tags_sorted_alphabetically_first_spelling_wins()
    {
        await SeedAsync("Alan", "Turing", tags: ["work", "Family"]);
        await SeedAsync("Marie", "Curie", tags: ["Work", "Friends"]);

        var tags = await _service.GetTagsAsync(_ownerId, CancellationToken.None);

        Assert.Equal(["Family", "Friends", "work"], tags);
    }

    [Fact]
    public async Task GetTagsAsync_excludes_tags_from_other_owners_and_deleted_contacts()
    {
        var otherOwner = Guid.NewGuid();
        await SeedAsync("Alan", "Turing", tags: ["Work"]);
        await SeedAsync("Marie", "Curie", tags: ["Secret"], ownerId: otherOwner);
        var toDelete = await SeedAsync("Charles", "Darwin", tags: ["Gone"]);
        await _service.DeleteAsync(_ownerId, toDelete.Id, CancellationToken.None);

        var tags = await _service.GetTagsAsync(_ownerId, CancellationToken.None);

        Assert.Equal(["Work"], tags);
    }
}
