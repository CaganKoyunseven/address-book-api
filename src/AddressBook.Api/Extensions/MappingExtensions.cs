using AddressBook.Api.Dtos;
using AddressBook.Api.Models;

namespace AddressBook.Api.Extensions;

/// <summary>Manual Model&lt;-&gt;DTO mapping. Deliberately not AutoMapper: the project is small and this is easier to teach.</summary>
public static class MappingExtensions
{
    public static ContactResponseDto ToResponseDto(this Contact contact) => new()
    {
        Id = contact.Id,
        FirstName = contact.FirstName,
        LastName = contact.LastName,
        Email = contact.Email,
        Phone = contact.Phone,
        Address = contact.Address?.ToDto(),
        Tags = contact.Tags,
        IsFavorite = contact.IsFavorite,
        CreatedAt = contact.CreatedAt,
        UpdatedAt = contact.UpdatedAt,
    };

    public static AddressDto ToDto(this Address address) => new()
    {
        Street = address.Street,
        City = address.City,
        Country = address.Country,
    };
}
