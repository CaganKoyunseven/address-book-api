using System.ComponentModel.DataAnnotations;

namespace AddressBook.Api.Dtos;

/// <summary>Request body for POST /auth/register. The caller can never pick their own role.</summary>
public sealed class RegisterDto
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Username may only contain letters, digits, '.', '_' and '-'.")]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
