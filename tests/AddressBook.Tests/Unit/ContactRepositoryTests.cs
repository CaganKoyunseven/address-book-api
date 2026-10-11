using AddressBook.Api.Data;
using AddressBook.Api.Models;

namespace AddressBook.Tests.Unit;

public class ContactRepositoryTests
{
    private static Contact NewContact(Guid? id = null, Guid? ownerId = null) => new(
        Id: id ?? Guid.NewGuid(),
        OwnerId: ownerId ?? Guid.NewGuid(),
        FirstName: "Ada",
        LastName: "Lovelace",
        Email: "ada@example.com",
        Phone: null,
        Address: null,
        Tags: [],
        IsFavorite: false,
        IsDeleted: false,
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow);

    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_the_same_contact()
    {
        var repository = new InMemoryContactRepository();
        var contact = NewContact();

        await repository.AddAsync(contact, CancellationToken.None);
        var found = await repository.GetByIdAsync(contact.Id, CancellationToken.None);

        Assert.Equal(contact, found);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        var repository = new InMemoryContactRepository();

        var found = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task AddAsync_with_duplicate_id_throws()
    {
        var repository = new InMemoryContactRepository();
        var contact = NewContact();
        await repository.AddAsync(contact, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddAsync(contact, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllAsync_returns_every_added_contact()
    {
        var repository = new InMemoryContactRepository();
        var a = NewContact();
        var b = NewContact();

        await repository.AddAsync(a, CancellationToken.None);
        await repository.AddAsync(b, CancellationToken.None);
        var all = await repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, all.Count);
        Assert.Contains(all, c => c.Id == a.Id);
        Assert.Contains(all, c => c.Id == b.Id);
    }

    [Fact]
    public async Task UpdateAsync_applies_the_updater_and_returns_the_new_value()
    {
        var repository = new InMemoryContactRepository();
        var contact = NewContact();
        await repository.AddAsync(contact, CancellationToken.None);

        var updated = await repository.UpdateAsync(
            contact.Id,
            c => c with { IsFavorite = true },
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.True(updated!.IsFavorite);
        var stored = await repository.GetByIdAsync(contact.Id, CancellationToken.None);
        Assert.True(stored!.IsFavorite);
    }

    [Fact]
    public async Task UpdateAsync_returns_null_for_unknown_id()
    {
        var repository = new InMemoryContactRepository();

        var updated = await repository.UpdateAsync(
            Guid.NewGuid(),
            c => c with { IsFavorite = true },
            CancellationToken.None);

        Assert.Null(updated);
    }

    [Fact]
    public async Task UpdateAsync_under_concurrent_writers_loses_no_updates()
    {
        // Many threads each append their own tag via the CAS retry loop; if updates were
        // ever lost to a race, the final tag count would be less than the writer count.
        var repository = new InMemoryContactRepository();
        var contact = NewContact();
        await repository.AddAsync(contact, CancellationToken.None);

        const int writerCount = 50;
        var tasks = Enumerable.Range(0, writerCount).Select(i => repository.UpdateAsync(
            contact.Id,
            c => c with { Tags = [.. c.Tags, $"tag{i}"] },
            CancellationToken.None));

        await Task.WhenAll(tasks);

        var final = await repository.GetByIdAsync(contact.Id, CancellationToken.None);
        Assert.Equal(writerCount, final!.Tags.Count);
        Assert.Equal(writerCount, final.Tags.Distinct().Count());
    }
}
