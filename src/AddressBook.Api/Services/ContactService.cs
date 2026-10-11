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

    public async Task<PagedResult<ContactResponseDto>> ListAsync(Guid ownerId, ContactQuery query, CancellationToken cancellationToken)
    {
        var all = await repository.GetAllAsync(cancellationToken);
        var filtered = all.Where(c => c.OwnerId == ownerId && !c.IsDeleted);
        var matching = ApplyFilters(filtered, query).ToList();

        SortContacts(matching, query);

        var totalCount = matching.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        var items = matching
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => c.ToResponseDto())
            .ToList();

        return new PagedResult<ContactResponseDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
        };
    }

    public async Task<IReadOnlyList<string>> GetTagsAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var all = await repository.GetAllAsync(cancellationToken);
        return all
            .Where(c => c.OwnerId == ownerId && !c.IsDeleted)
            // Repository order comes from a ConcurrentDictionary and is not guaranteed, so
            // "first spelling wins" needs its own deterministic order to pick from.
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .SelectMany(c => c.Tags)
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Applies favorite/tag/search filters. Shared shape so the admin list (Phase 6) can reuse it.</summary>
    internal static IEnumerable<Contact> ApplyFilters(IEnumerable<Contact> contacts, ContactQuery query)
    {
        var filtered = contacts;

        if (query.Favorite is { } favorite)
        {
            filtered = filtered.Where(c => c.IsFavorite == favorite);
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            filtered = filtered.Where(c => c.Tags.Any(t => string.Equals(t, query.Tag, StringComparison.OrdinalIgnoreCase)));
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(c => MatchesSearch(c, search));
        }

        return filtered;
    }

    private static bool MatchesSearch(Contact contact, string search)
    {
        var fullName = $"{contact.FirstName} {contact.LastName}";
        return Contains(contact.FirstName, search)
            || Contains(contact.LastName, search)
            || Contains(fullName, search)
            || Contains(contact.Email, search)
            || Contains(contact.Phone, search)
            || Contains(contact.Address?.City, search)
            || contact.Tags.Any(tag => Contains(tag, search));
    }

    private static bool Contains(string? value, string search) =>
        value is not null && value.Contains(search, StringComparison.OrdinalIgnoreCase);

    /// <summary>Sorts in place: optional favorites-first, then the chosen field/direction, then Id as a deterministic tie-break.</summary>
    internal static void SortContacts(List<Contact> contacts, ContactQuery query)
    {
        Comparison<Contact> byField = query.SortBy switch
        {
            ContactSortBy.Email => (a, b) => string.Compare(a.Email ?? string.Empty, b.Email ?? string.Empty, StringComparison.OrdinalIgnoreCase),
            ContactSortBy.CreatedAt => (a, b) => a.CreatedAt.CompareTo(b.CreatedAt),
            _ => (a, b) =>
            {
                var byLastName = string.Compare(a.LastName, b.LastName, StringComparison.OrdinalIgnoreCase);
                return byLastName != 0 ? byLastName : string.Compare(a.FirstName, b.FirstName, StringComparison.OrdinalIgnoreCase);
            },
        };

        if (query.SortDir == SortDirection.Desc)
        {
            var ascending = byField;
            byField = (a, b) => ascending(b, a);
        }

        contacts.Sort((a, b) =>
        {
            if (query.FavoritesFirst)
            {
                var byFavorite = b.IsFavorite.CompareTo(a.IsFavorite); // favorites (true) always first
                if (byFavorite != 0)
                {
                    return byFavorite;
                }
            }

            var byFieldResult = byField(a, b);
            return byFieldResult != 0 ? byFieldResult : a.Id.CompareTo(b.Id);
        });
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
