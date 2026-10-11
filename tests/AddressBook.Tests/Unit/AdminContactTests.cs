using AddressBook.Api.Data;
using AddressBook.Api.Dtos;
using AddressBook.Api.Models;
using AddressBook.Api.Services;

namespace AddressBook.Tests.Unit;

public class AdminContactTests
{
    private readonly IUserRepository _users = new InMemoryUserRepository();
    private readonly IContactRepository _contacts = new InMemoryContactRepository();
    private ContactService Service => new(_contacts, _users);

    private async Task<User> AddUserAsync(string username)
    {
        var user = new User(Guid.NewGuid(), username, "hash", Role.User);
        await _users.TryAddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task AdminListAsync_includes_contacts_from_every_owner_with_username()
    {
        var service = Service;
        var alice = await AddUserAsync("alice");
        var bob = await AddUserAsync("bob");
        await service.CreateAsync(alice.Id, new ContactCreateDto { FirstName = "Alan", LastName = "Turing" }, CancellationToken.None);
        await service.CreateAsync(bob.Id, new ContactCreateDto { FirstName = "Marie", LastName = "Curie" }, CancellationToken.None);

        var result = await service.AdminListAsync(new AdminContactQuery(), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, c => c.OwnerUsername == "alice" && c.LastName == "Turing");
        Assert.Contains(result.Items, c => c.OwnerUsername == "bob" && c.LastName == "Curie");
    }

    [Fact]
    public async Task AdminListAsync_filters_by_owner_id()
    {
        var service = Service;
        var alice = await AddUserAsync("alice");
        var bob = await AddUserAsync("bob");
        await service.CreateAsync(alice.Id, new ContactCreateDto { FirstName = "Alan", LastName = "Turing" }, CancellationToken.None);
        await service.CreateAsync(bob.Id, new ContactCreateDto { FirstName = "Marie", LastName = "Curie" }, CancellationToken.None);

        var result = await service.AdminListAsync(new AdminContactQuery { OwnerId = alice.Id }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Turing", result.Items[0].LastName);
    }

    [Fact]
    public async Task AdminListAsync_excludes_deleted_contacts_by_default_but_includes_them_when_asked()
    {
        var service = Service;
        var alice = await AddUserAsync("alice");
        var created = await service.CreateAsync(alice.Id, new ContactCreateDto { FirstName = "Alan", LastName = "Turing" }, CancellationToken.None);
        await service.DeleteAsync(alice.Id, created.Id, CancellationToken.None);

        var withoutDeleted = await service.AdminListAsync(new AdminContactQuery(), CancellationToken.None);
        var withDeleted = await service.AdminListAsync(new AdminContactQuery { IncludeDeleted = true }, CancellationToken.None);

        Assert.Empty(withoutDeleted.Items);
        Assert.Single(withDeleted.Items);
        Assert.True(withDeleted.Items[0].IsDeleted);
    }

    [Fact]
    public async Task RestoreAsync_brings_back_a_deleted_contact_so_the_owner_sees_it_again()
    {
        var service = Service;
        var alice = await AddUserAsync("alice");
        var created = await service.CreateAsync(alice.Id, new ContactCreateDto { FirstName = "Alan", LastName = "Turing" }, CancellationToken.None);
        await service.DeleteAsync(alice.Id, created.Id, CancellationToken.None);

        var restored = await service.RestoreAsync(created.Id, CancellationToken.None);
        var visibleAgain = await service.GetByIdAsync(alice.Id, created.Id, CancellationToken.None);

        Assert.True(restored);
        Assert.NotNull(visibleAgain);
    }

    [Fact]
    public async Task RestoreAsync_on_an_already_active_contact_also_succeeds()
    {
        var service = Service;
        var alice = await AddUserAsync("alice");
        var created = await service.CreateAsync(alice.Id, new ContactCreateDto { FirstName = "Alan", LastName = "Turing" }, CancellationToken.None);

        var restored = await service.RestoreAsync(created.Id, CancellationToken.None);

        Assert.True(restored);
    }

    [Fact]
    public async Task RestoreAsync_returns_false_for_an_unknown_id()
    {
        var service = Service;

        var restored = await service.RestoreAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(restored);
    }
}
