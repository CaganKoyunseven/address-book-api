namespace AddressBook.Api.Dtos;

/// <summary>Response body for POST /auth/login.</summary>
public sealed class AuthResponseDto
{
    public string Token { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
