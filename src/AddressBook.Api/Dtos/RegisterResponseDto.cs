namespace AddressBook.Api.Dtos;

/// <summary>Response body for POST /auth/register.</summary>
public sealed class RegisterResponseDto
{
    public Guid Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
