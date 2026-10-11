using AddressBook.Api.Data;
using AddressBook.Api.Dtos;
using AddressBook.Api.Services;

namespace AddressBook.Tests.Unit;

public class ContactServiceTests
{
    private static ContactCreateDto NewCreateDto(string first = "Ada", string last = "Lovelace") => new()
    {
        FirstName = $"  {first}  ",
        LastName = $"  {last}  ",
        Email = "  ada@example.com  ",
        Phone = "  +1 555 123 4567  ",
        Address = new AddressDto { Street = "  ", City = "  London  ", Country = "" },
        Tags = ["Work", " work ", "Family", ""],
    };

    [Fact]
    public async Task CreateAsync_sets_owner_and_timestamps_and_trims_fields()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var ownerId = Guid.NewGuid();

        var created = await service.CreateAsync(ownerId, NewCreateDto(), CancellationToken.None);

        Assert.Equal("Ada", created.FirstName);
        Assert.Equal("Lovelace", created.LastName);
        Assert.Equal("ada@example.com", created.Email);
        Assert.Equal("+1 555 123 4567", created.Phone);
        Assert.True(created.CreatedAt == created.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_normalizes_tags_trim_dedupe_case_insensitive_preserve_order()
    {
        var service = new ContactService(new InMemoryContactRepository());

        var created = await service.CreateAsync(Guid.NewGuid(), NewCreateDto(), CancellationToken.None);

        Assert.Equal(["Work", "Family"], created.Tags);
    }

    [Fact]
    public async Task CreateAsync_blank_optional_fields_become_null()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var dto = new ContactCreateDto { FirstName = "A", LastName = "B", Email = "   ", Phone = null, Address = new AddressDto { Street = "  ", City = "  ", Country = "  " } };

        var created = await service.CreateAsync(Guid.NewGuid(), dto, CancellationToken.None);

        Assert.Null(created.Email);
        Assert.Null(created.Phone);
        Assert.Null(created.Address);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_another_owners_contact()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        var created = await service.CreateAsync(ownerA, NewCreateDto(), CancellationToken.None);

        var found = await service.GetByIdAsync(ownerB, created.Id, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateAsync_replaces_fields_keeps_id_and_bumps_updated_at()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var ownerId = Guid.NewGuid();
        var created = await service.CreateAsync(ownerId, NewCreateDto(), CancellationToken.None);
        await Task.Delay(5);

        var update = new ContactUpdateDto { FirstName = "Grace", LastName = "Hopper", IsFavorite = true };
        var updated = await service.UpdateAsync(ownerId, created.Id, update, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Grace", updated.FirstName);
        Assert.Equal("Hopper", updated.LastName);
        Assert.True(updated.IsFavorite);
        Assert.True(updated.UpdatedAt > created.UpdatedAt);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
    }

    [Fact]
    public async Task UpdateAsync_returns_null_for_another_owners_contact()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        var created = await service.CreateAsync(ownerA, NewCreateDto(), CancellationToken.None);

        var update = new ContactUpdateDto { FirstName = "X", LastName = "Y" };
        var updated = await service.UpdateAsync(ownerB, created.Id, update, CancellationToken.None);

        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteAsync_soft_deletes_then_second_delete_fails_and_contact_is_hidden()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var ownerId = Guid.NewGuid();
        var created = await service.CreateAsync(ownerId, NewCreateDto(), CancellationToken.None);

        var firstDelete = await service.DeleteAsync(ownerId, created.Id, CancellationToken.None);
        var secondDelete = await service.DeleteAsync(ownerId, created.Id, CancellationToken.None);
        var found = await service.GetByIdAsync(ownerId, created.Id, CancellationToken.None);

        Assert.True(firstDelete);
        Assert.False(secondDelete);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_returns_false_for_another_owners_contact()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        var created = await service.CreateAsync(ownerA, NewCreateDto(), CancellationToken.None);

        var deleted = await service.DeleteAsync(ownerB, created.Id, CancellationToken.None);

        Assert.False(deleted);
    }

    [Fact]
    public async Task SetFavoriteAsync_marking_twice_stays_favorite_and_does_not_touch_updated_at_on_noop()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var ownerId = Guid.NewGuid();
        var created = await service.CreateAsync(ownerId, NewCreateDto(), CancellationToken.None);

        var first = await service.SetFavoriteAsync(ownerId, created.Id, true, CancellationToken.None);
        var afterFirst = await service.GetByIdAsync(ownerId, created.Id, CancellationToken.None);
        await Task.Delay(5);
        var second = await service.SetFavoriteAsync(ownerId, created.Id, true, CancellationToken.None);
        var afterSecond = await service.GetByIdAsync(ownerId, created.Id, CancellationToken.None);

        Assert.True(first);
        Assert.True(second);
        Assert.True(afterSecond!.IsFavorite);
        Assert.Equal(afterFirst!.UpdatedAt, afterSecond.UpdatedAt);
    }

    [Fact]
    public async Task SetFavoriteAsync_unmarking_twice_stays_not_favorite()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var ownerId = Guid.NewGuid();
        var created = await service.CreateAsync(ownerId, NewCreateDto(), CancellationToken.None);

        var first = await service.SetFavoriteAsync(ownerId, created.Id, false, CancellationToken.None);
        var second = await service.SetFavoriteAsync(ownerId, created.Id, false, CancellationToken.None);
        var found = await service.GetByIdAsync(ownerId, created.Id, CancellationToken.None);

        Assert.True(first);
        Assert.True(second);
        Assert.False(found!.IsFavorite);
    }

    [Fact]
    public async Task SetFavoriteAsync_returns_false_for_another_owners_contact()
    {
        var service = new ContactService(new InMemoryContactRepository());
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        var created = await service.CreateAsync(ownerA, NewCreateDto(), CancellationToken.None);

        var result = await service.SetFavoriteAsync(ownerB, created.Id, true, CancellationToken.None);

        Assert.False(result);
    }
}
