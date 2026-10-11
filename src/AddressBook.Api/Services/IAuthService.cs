using AddressBook.Api.Models;

namespace AddressBook.Api.Services;

public interface IAuthService
{
    /// <summary>Creates a new User account with role User. Returns null if the username is already taken.</summary>
    Task<User?> RegisterAsync(string username, string password, CancellationToken cancellationToken);

    /// <summary>Returns the user if the credentials are valid, otherwise null (unknown user and wrong password look identical).</summary>
    Task<User?> LoginAsync(string username, string password, CancellationToken cancellationToken);
}
