using AddressBook.Api.Dtos;

namespace AddressBook.Api.Services;

/// <summary>
/// Business rules for contacts: ownership, normalization and soft-delete. Takes the caller's
/// user id as a plain parameter (never reads HttpContext), so it stays easy to unit test.
/// </summary>
public interface IContactService
{
    Task<ContactResponseDto?> GetByIdAsync(Guid ownerId, Guid contactId, CancellationToken cancellationToken);

    Task<ContactResponseDto> CreateAsync(Guid ownerId, ContactCreateDto dto, CancellationToken cancellationToken);

    /// <summary>Full replacement of the editable fields. Returns null if the contact isn't visible to this owner.</summary>
    Task<ContactResponseDto?> UpdateAsync(Guid ownerId, Guid contactId, ContactUpdateDto dto, CancellationToken cancellationToken);

    /// <summary>Soft delete. Returns false if the contact isn't visible to this owner (including: already deleted).</summary>
    Task<bool> DeleteAsync(Guid ownerId, Guid contactId, CancellationToken cancellationToken);

    /// <summary>Idempotent: setting the same favorite state twice still succeeds and leaves UpdatedAt unchanged.</summary>
    Task<bool> SetFavoriteAsync(Guid ownerId, Guid contactId, bool favorite, CancellationToken cancellationToken);

    /// <summary>Searches, filters, sorts and pages the caller's own (non-deleted) contacts.</summary>
    Task<PagedResult<ContactResponseDto>> ListAsync(Guid ownerId, ContactQuery query, CancellationToken cancellationToken);

    /// <summary>Distinct tags used by the caller's own (non-deleted) contacts, sorted alphabetically.</summary>
    Task<IReadOnlyList<string>> GetTagsAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>Admin-only: lists contacts across every owner, with the same filters plus owner/soft-delete.</summary>
    Task<PagedResult<AdminContactResponseDto>> AdminListAsync(AdminContactQuery query, CancellationToken cancellationToken);

    /// <summary>Admin-only: un-deletes a contact regardless of its owner. Restoring an already-active contact also succeeds.</summary>
    Task<bool> RestoreAsync(Guid contactId, CancellationToken cancellationToken);
}
