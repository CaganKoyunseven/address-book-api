using AddressBook.Api.Data;
using AddressBook.Api.Models;
using AddressBook.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AddressBook.Tests.Unit;

public class SeedDataTests
{
    private static async Task<(IUserRepository Users, IContactRepository Contacts)> SeedAsync(bool enabled = true)
    {
        var users = new InMemoryUserRepository();
        var contacts = new InMemoryContactRepository();
        var hasher = new PasswordHasher<User>();
        var options = Options.Create(new SeedOptions { Enabled = enabled, AdminPassword = "Admin#12345", DemoPassword = "Demo#12345" });

        await SeedData.SeedAsync(users, contacts, hasher, options, NullLogger.Instance, CancellationToken.None);
        return (users, contacts);
    }

    [Fact]
    public async Task Seeds_admin_and_demo_users_with_hashed_passwords()
    {
        var (users, _) = await SeedAsync();

        var admin = await users.GetByUsernameAsync("admin", CancellationToken.None);
        var demo = await users.GetByUsernameAsync("demo", CancellationToken.None);

        Assert.NotNull(admin);
        Assert.Equal(Role.Admin, admin!.Role);
        Assert.NotEqual("Admin#12345", admin.PasswordHash);

        Assert.NotNull(demo);
        Assert.Equal(Role.User, demo!.Role);
    }

    [Fact]
    public async Task Seeds_24_contacts_owned_by_the_demo_user()
    {
        var (users, contacts) = await SeedAsync();
        var demo = await users.GetByUsernameAsync("demo", CancellationToken.None);

        var all = await contacts.GetAllAsync(CancellationToken.None);

        Assert.Equal(24, all.Count);
        Assert.All(all, c => Assert.Equal(demo!.Id, c.OwnerId));
    }

    [Fact]
    public async Task Does_not_seed_twice()
    {
        var users = new InMemoryUserRepository();
        var contacts = new InMemoryContactRepository();
        var hasher = new PasswordHasher<User>();
        var options = Options.Create(new SeedOptions { Enabled = true, AdminPassword = "Admin#12345", DemoPassword = "Demo#12345" });

        await SeedData.SeedAsync(users, contacts, hasher, options, NullLogger.Instance, CancellationToken.None);
        await SeedData.SeedAsync(users, contacts, hasher, options, NullLogger.Instance, CancellationToken.None);

        var all = await contacts.GetAllAsync(CancellationToken.None);
        Assert.Equal(24, all.Count);
    }

    [Fact]
    public async Task Skips_seeding_when_disabled()
    {
        var (users, contacts) = await SeedAsync(enabled: false);

        Assert.Null(await users.GetByUsernameAsync("admin", CancellationToken.None));
        Assert.Empty(await contacts.GetAllAsync(CancellationToken.None));
    }
}
