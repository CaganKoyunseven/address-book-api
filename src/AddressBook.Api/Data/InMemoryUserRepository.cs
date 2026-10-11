using System.Collections.Concurrent;
using AddressBook.Api.Models;

namespace AddressBook.Api.Data;

/// <summary>
/// Keyed by username with a case-insensitive comparer, so <see cref="TryAddAsync"/> is both
/// the insert and the atomic "is this username already taken" uniqueness check at once.
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _usersByUsername = new(StringComparer.OrdinalIgnoreCase);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_usersByUsername.Values.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        => Task.FromResult(_usersByUsername.TryGetValue(username, out var user) ? user : null);

    public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(_usersByUsername.TryAdd(user.Username, user));
}
