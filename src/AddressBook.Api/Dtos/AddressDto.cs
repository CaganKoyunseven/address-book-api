using System.ComponentModel.DataAnnotations;

namespace AddressBook.Api.Dtos;

/// <summary>A contact's postal address. All parts are optional.</summary>
public sealed class AddressDto
{
    [StringLength(100)]
    public string? Street { get; init; }

    [StringLength(60)]
    public string? City { get; init; }

    [StringLength(60)]
    public string? Country { get; init; }
}
