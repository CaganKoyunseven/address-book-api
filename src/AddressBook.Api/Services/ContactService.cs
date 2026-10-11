using AddressBook.Api.Data;
using AddressBook.Api.Dtos;
using AddressBook.Api.Extensions;
using AddressBook.Api.Models;

namespace AddressBook.Api.Services;

public sealed class ContactService(IContactRepository repository) : IContactService
{
    public async Task<ContactResponseDto?> GetByIdAsync(Guid ownerId, Guid contactId, CancellationToken cancellationToken)
    {
        var contact = await repository.GetByIdAsync(contactId, cancellationToken);
        return IsVisibleToOwner(contact, ownerId) ? contact!.ToResponseDto() : null;
    }

    public async Task<ContactResponseDto> CreateAsync(Guid ownerId, ContactCreateDto dto, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var contact = new Contact(
            Id: Guid.NewGuid(),
            OwnerId: ownerId,
            FirstName: dto.FirstName.Trim(),
            LastName: dto.LastName.Trim(),
            Email: NormalizeOptional(dto.Email),
            Phone: NormalizeOptional(dto.Phone),
            Address: NormalizeAddress(dto.Address),
            Tags: NormalizeTags(dto.Tags),
            IsFavorite: dto.IsFavorite,
            IsDeleted: false,
            CreatedAt: now,
            UpdatedAt: now);

        var added = await repository.AddAsync(contact, cancellationToken);
        return added.ToResponseDto();
    }

    public async Task<ContactResponseDto?> UpdateAsync(Guid ownerId, Guid contactId, ContactUpdateDto dto, CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(contactId, cancellationToken);
        if (!IsVisibleToOwner(current, ownerId))
        {
            return null;
        }

        var updated = await repository.UpdateAsync(contactId, c => c with
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = NormalizeOptional(dto.Email),
            Phone = NormalizeOptional(dto.Phone),
            Address = NormalizeAddress(dto.Address),
            Tags = NormalizeTags(dto.Tags),
            IsFavorite = dto.IsFavorite,
            UpdatedAt = DateTimeOffset.UtcNow,
        }, cancellationToken);

        return updated?.ToResponseDto();
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid contactId, CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(contactId, cancellationToken);
        if (!IsVisibleToOwner(current, ownerId))
        {
            return false;
        }

        var updated = await repository.UpdateAsync(
            contactId,
            c => c with { IsDeleted = true, UpdatedAt = DateTimeOffset.UtcNow },
            cancellationToken);
        return updated is not null;
    }

    public async Task<bool> SetFavoriteAsync(Guid ownerId, Guid contactId, bool favorite, CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(contactId, cancellationToken);
        if (!IsVisibleToOwner(current, ownerId))
        {
            return false;
        }

        if (current!.IsFavorite == favorite)
        {
            return true; // already in that state: idempotent no-op, UpdatedAt stays as-is
        }

        var updated = await repository.UpdateAsync(
            contactId,
            c => c with { IsFavorite = favorite, UpdatedAt = DateTimeOffset.UtcNow },
            cancellationToken);
        return updated is not null;
    }

    private static bool IsVisibleToOwner(Contact? contact, Guid ownerId) =>
        contact is not null && contact.OwnerId == ownerId && !contact.IsDeleted;

    private static string? NormalizeOptional(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static Address? NormalizeAddress(AddressDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        var street = NormalizeOptional(dto.Street);
        var city = NormalizeOptional(dto.City);
        var country = NormalizeOptional(dto.Country);
        return street is null && city is null && country is null ? null : new Address(street, city, country);
    }

    /// <summary>Trims, drops empties, de-duplicates case-insensitively (first spelling wins), preserves order.</summary>
    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return [];
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            var trimmed = tag.Trim();
            if (trimmed.Length > 0 && seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }
}
