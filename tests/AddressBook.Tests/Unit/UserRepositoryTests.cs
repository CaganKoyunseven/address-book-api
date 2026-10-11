using AddressBook.Api.Data;
using AddressBook.Api.Models;

namespace AddressBook.Tests.Unit;

public class UserRepositoryTests
{
    private static User NewUser(string username, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), username, PasswordHash: "hash", Role.User);

    [Fact]
    public async Task TryAddAsync_succeeds_for_a_new_username()
    {
        var repository = new InMemoryUserRepository();

        var added = await repository.TryAddAsync(NewUser("demo"), CancellationToken.None);

        Assert.True(added);
    }

    [Fact]
    public async Task TryAddAsync_fails_for_a_duplicate_username_regardless_of_case()
    {
        var repository = new InMemoryUserRepository();
        await repository.TryAddAsync(NewUser("demo"), CancellationToken.None);

        var added = await repository.TryAddAsync(NewUser("DEMO"), CancellationToken.None);

        Assert.False(added);
    }

    [Fact]
    public async Task GetByUsernameAsync_is_case_insensitive()
    {
        var repository = new InMemoryUserRepository();
        var user = NewUser("Demo");
        await repository.TryAddAsync(user, CancellationToken.None);

        var found = await repository.GetByUsernameAsync("demo", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task GetByUsernameAsync_returns_null_for_unknown_username()
    {
        var repository = new InMemoryUserRepository();

        var found = await repository.GetByUsernameAsync("nobody", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByIdAsync_returns_the_matching_user()
    {
        var repository = new InMemoryUserRepository();
        var user = NewUser("demo");
        await repository.TryAddAsync(user, CancellationToken.None);

        var found = await repository.GetByIdAsync(user.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("demo", found!.Username);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        var repository = new InMemoryUserRepository();

        var found = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(found);
    }
}
