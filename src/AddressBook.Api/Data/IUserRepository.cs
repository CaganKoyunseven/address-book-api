using AddressBook.Api.Models;

namespace AddressBook.Api.Data;

/// <summary>The only code allowed to touch the in-memory user store.</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    /// <summary>Adds the user atomically. Returns false if the (case-insensitive) username is already taken.</summary>
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken);
}
