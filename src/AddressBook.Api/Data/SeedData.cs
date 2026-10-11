using AddressBook.Api.Models;
using AddressBook.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AddressBook.Api.Data;

/// <summary>
/// Populates the in-memory store at startup with a seeded admin, a seeded demo user and 24
/// demo contacts, so pagination/search/sort/favorites can be demonstrated immediately. Runs
/// once per process start (the in-memory store is empty again after every restart); skips
/// itself if a user named "admin" already exists, so it is safe to call more than once.
/// </summary>
public static class SeedData
{
    public static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DemoUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static readonly int[] FavoriteIndexes = [1, 3, 6, 10, 15];

    public static async Task SeedAsync(
        IUserRepository userRepository,
        IContactRepository contactRepository,
        IPasswordHasher<User> passwordHasher,
        IOptions<SeedOptions> seedOptions,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var options = seedOptions.Value;
        if (!options.Enabled)
        {
            logger.LogInformation("Seeding disabled (Seed:Enabled=false)");
            return;
        }

        if (await userRepository.GetByUsernameAsync("admin", cancellationToken) is not null)
        {
            logger.LogInformation("Seed data already present, skipping");
            return;
        }

        var admin = CreateUser(AdminId, "admin", options.AdminPassword, Role.Admin, passwordHasher);
        var demo = CreateUser(DemoUserId, "demo", options.DemoPassword, Role.User, passwordHasher);
        await userRepository.TryAddAsync(admin, cancellationToken);
        await userRepository.TryAddAsync(demo, cancellationToken);

        var contacts = BuildDemoContacts(demo.Id);
        foreach (var contact in contacts)
        {
            await contactRepository.AddAsync(contact, cancellationToken);
        }

        logger.LogInformation("Seeded admin, demo user and {Count} demo contacts", contacts.Count);
    }

    private static User CreateUser(Guid id, string username, string password, Role role, IPasswordHasher<User> passwordHasher)
    {
        var user = new User(id, username, PasswordHash: string.Empty, role);
        return user with { PasswordHash = passwordHasher.HashPassword(user, password) };
    }

    private static IReadOnlyList<Contact> BuildDemoContacts(Guid ownerId)
    {
        // CreatedAt increases with each contact so sorting by createdAt is meaningful.
        var seedDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

        // Alan Turing/Katherine Johnson/Grace Hopper/Isaac Newton/Linus Torvalds are the
        // favorites (1-based positions below). "Hans Einstein" shares a last name with
        // "Albert Einstein" on purpose, to exercise the sort tie-break.
        (string First, string Last, string City, string Country, string[] Tags)[] people =
        [
            ("Alan", "Turing", "London", "United Kingdom", ["Work"]),
            ("Marie", "Curie", "Paris", "France", ["Family"]),
            ("Katherine", "Johnson", "Hampton", "United States", ["Phone"]),
            ("Charles", "Darwin", "Shrewsbury", "United Kingdom", []),
            ("Ada", "Lovelace", "London", "United Kingdom", ["Work"]),
            ("Grace", "Hopper", "New York", "United States", ["Family"]),
            ("Nikola", "Tesla", "Smiljan", "Croatia", ["Phone"]),
            ("Albert", "Einstein", "Ulm", "Germany", ["Work", "Friends"]),
            ("Rosalind", "Franklin", "London", "United Kingdom", ["Family"]),
            ("Isaac", "Newton", "Woolsthorpe", "United Kingdom", ["Work"]),
            ("Galileo", "Galilei", "Pisa", "Italy", ["Phone"]),
            ("Dorothy", "Vaughan", "Kansas City", "United States", ["Work"]),
            ("Mary", "Jackson", "Hampton", "United States", ["Family"]),
            ("Tim", "Berners-Lee", "London", "United Kingdom", ["Work", "Phone"]),
            ("Linus", "Torvalds", "Helsinki", "Finland", ["Work"]),
            ("Margaret", "Hamilton", "Paoli", "United States", ["Family"]),
            ("Richard", "Feynman", "New York", "United States", ["Phone"]),
            ("Carl", "Sagan", "New York", "United States", ["Friends"]),
            ("Hedy", "Lamarr", "Vienna", "Austria", ["Work"]),
            ("Johannes", "Kepler", "Weil der Stadt", "Germany", ["Family"]),
            ("Hans", "Einstein", "Bern", "Switzerland", ["Phone"]),
            ("Lise", "Meitner", "Vienna", "Austria", ["Work"]),
            ("Srinivasa", "Ramanujan", "Erode", "India", ["Family", "Friends"]),
            ("Emmy", "Noether", "Erlangen", "Germany", []),
        ];

        var contacts = new List<Contact>(people.Length);
        for (var i = 0; i < people.Length; i++)
        {
            var index = i + 1;
            var (first, last, city, country, tags) = people[i];
            var createdAt = seedDate.AddMinutes(index);

            contacts.Add(new Contact(
                Id: Guid.Parse($"00000000-0000-0000-0001-{index:D12}"),
                OwnerId: ownerId,
                FirstName: first,
                LastName: last,
                Email: $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}@example.com",
                Phone: $"+1 555 123 {4500 + index}",
                Address: new Address(Street: null, City: city, Country: country),
                Tags: tags,
                IsFavorite: FavoriteIndexes.Contains(index),
                IsDeleted: false,
                CreatedAt: createdAt,
                UpdatedAt: createdAt));
        }

        return contacts;
    }
}
