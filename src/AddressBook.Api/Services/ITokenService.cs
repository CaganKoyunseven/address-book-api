using AddressBook.Api.Models;

namespace AddressBook.Api.Services;

public interface ITokenService
{
    /// <summary>Creates a signed JWT for the given user.</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateToken(User user);
}
