using System.ComponentModel.DataAnnotations;
using AddressBook.Api.Validation;

namespace AddressBook.Api.Dtos;

/// <summary>Request body for POST /contacts.</summary>
public sealed class ContactCreateDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string LastName { get; init; } = string.Empty;

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; init; }

    [RegularExpression(@"^[0-9+\-()\s]{3,30}$", ErrorMessage = "Phone may only contain digits, spaces, '+', '-', '(' and ')'.")]
    public string? Phone { get; init; }

    public AddressDto? Address { get; init; }

    [TagList]
    public List<string> Tags { get; init; } = [];

    public bool IsFavorite { get; init; }
}
