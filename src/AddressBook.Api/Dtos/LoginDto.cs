using System.ComponentModel.DataAnnotations;

namespace AddressBook.Api.Dtos;

/// <summary>Request body for POST /auth/login.</summary>
public sealed class LoginDto
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
