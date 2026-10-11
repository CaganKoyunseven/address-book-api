using AddressBook.Api.Data;
using AddressBook.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace AddressBook.Api.Services;

public sealed class AuthService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<User?> RegisterAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = new User(Guid.NewGuid(), username, PasswordHash: string.Empty, Role.User);
        user = user with { PasswordHash = passwordHasher.HashPassword(user, password) };

        var added = await userRepository.TryAddAsync(user, cancellationToken);
        return added ? user : null;
    }

    public async Task<User?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }
}
